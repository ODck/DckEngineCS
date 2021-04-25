using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using AssetPrimitives;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Graphics.Services;
using Serilog;
using Veldrid;
using Veldrid.SPIRV;
using Log = Dck.Engine.Logging.Log;

namespace Dck.Engine.TEST
{
    public class Renderer3D
    {
        private readonly Application _app;
        private CommandList _commandList = null;
        private Camera _camera;

        //Shared
        private ResourceSet _sharedResourceSet;
        private DeviceBuffer _cameraProjectionBuffer;
        private DeviceBuffer _lightInfoBuffer;

        //StarField
        private Pipeline _starFieldPipeline;
        private DeviceBuffer _viewInfoBuffer;
        private ResourceSet _viewInfoSet;

        //Dynamic
        private Vector3 _lightDir;
        private bool _lightFromCamera = false; // Press F1 to switch where the directional light originates
        private DeviceBuffer _rotationInfoBuffer; // Contains the local and global rotation values.
        private float _localRotation = 0f; // Causes individual rocks to rotate around their centers
        private float _globalRotation = 0f; // Causes rocks to rotate around the global origin (where the planet is)


        public Renderer3D(Application app)
        {
            _app = app;
        }

        public void CreateResources(GraphicsDevice graphicsDevice)
        {
            var factory = graphicsDevice.ResourceFactory;
            _camera.Position = new Vector3(-36f, 20f, 100f);
            _camera.Pitch = -0.3f;
            _camera.Yaw = 0.1f;

            _cameraProjectionBuffer = factory.CreateBuffer(
                new BufferDescription((uint) (Unsafe.SizeOf<Matrix4x4>() * 2),
                    BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _lightInfoBuffer =
                factory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _rotationInfoBuffer =
                factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _lightDir = Vector3.Normalize(new Vector3(0.3f, -0.75f, -0.3f));

            var sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2));

            var etc2Supported = graphicsDevice.GetPixelFormatSupport(
                PixelFormat.ETC2_R8_G8_B8_UNorm,
                TextureType.Texture2D,
                TextureUsage.Sampled);
            var pixelFormat = etc2Supported ? PixelFormat.ETC2_R8_G8_B8_UNorm : PixelFormat.BC3_UNorm;


            //Starfield Resources
            ResourceLayout invCameraInfoLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InvCameraInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            _viewInfoBuffer = factory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<MatrixPair>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _viewInfoSet = factory.CreateResourceSet(new ResourceSetDescription(invCameraInfoLayout, _viewInfoBuffer));
            
            
            ShaderSetDescription starFieldShaders = new ShaderSetDescription(
                Array.Empty<VertexLayoutDescription>(),
                LoadShaders("Starfield"));
            
            _starFieldPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                starFieldShaders,
                new[] { invCameraInfoLayout },
                graphicsDevice.MainSwapchain.Framebuffer.OutputDescription));
        }

        public void RenderFrame(float deltaTime)
        {
            _commandList.UpdateBuffer(_cameraProjectionBuffer, 0, new MatrixPair(_camera.ViewMatrix, _camera.ProjectionMatrix));
            _localRotation += deltaTime * ((float)Math.PI * 2 / 9);
            _globalRotation += -deltaTime * ((float)Math.PI * 2 / 240);
            _commandList.UpdateBuffer(_rotationInfoBuffer, 0, new Vector4(_localRotation, _globalRotation, 0, 0));
            
            Matrix4x4.Invert(_camera.ProjectionMatrix, out Matrix4x4 inverseProjection);
            Matrix4x4.Invert(_camera.ViewMatrix, out Matrix4x4 inverseView);
            _commandList.UpdateBuffer(_viewInfoBuffer, 0, new MatrixPair(
                inverseProjection,
                inverseView));
            
            //// First, draw the background starfield.
            _commandList.SetPipeline(_starFieldPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewInfoSet);
            _commandList.Draw(4);
        }

        public void SetCommandList(CommandList commandList)
        {
            _commandList = commandList;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }


        // private readonly Dictionary<Type, BinaryAssetSerializer> _serializers = DefaultSerializers.Get();
        // public T LoadEmbeddedAsset<T>(string name)
        // {
        //     if (!_serializers.TryGetValue(typeof(T), out BinaryAssetSerializer serializer))
        //     {
        //         throw new InvalidOperationException("No serializer registered for type " + typeof(T).Name);
        //     }
        //
        //     using (Stream stream = GetType().Assembly.GetManifestResourceStream(name))
        //     {
        //         if (stream == null)
        //         {
        //             throw new InvalidOperationException("No embedded asset with the name " + name);
        //         }
        //
        //         BinaryReader reader = new BinaryReader(stream);
        //         return (T)serializer.Read(reader);
        //     }
        // }

        private Shader[] LoadShaders(string setName)
        {
            return _app.GraphicsDevice.ResourceFactory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex,
                    ReadEmbeddedAssetBytes("Dck.Engine.src.TEST." + setName + "-vertex.glsl"), "main"),
                new ShaderDescription(ShaderStages.Fragment,
                    ReadEmbeddedAssetBytes("Dck.Engine.src.TEST." + setName + "-fragment.glsl"), "main"));
        }

        public byte[] ReadEmbeddedAssetBytes(string name)
        {
            using Stream stream = OpenEmbeddedAssetStream(name);
            byte[] bytes = new byte[stream.Length];
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                stream.CopyTo(ms);
                return bytes;
            }
        }

        public Stream OpenEmbeddedAssetStream(string name) => GetType().Assembly.GetManifestResourceStream(name);
        public struct MatrixPair
        {
            public Matrix4x4 First;
            public Matrix4x4 Second;

            public MatrixPair(Matrix4x4 first, Matrix4x4 second)
            {
                First = first;
                Second = second;
            }
        }
    }
}