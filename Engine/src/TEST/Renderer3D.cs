using System.Numerics;
using System.Text;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Logging;
using Veldrid;
using Veldrid.SPIRV;

namespace Dck.Engine.TEST
{
    public class Renderer3D
    {
        private const string VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 0) out vec2 fsin_texCoords;

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
}";

        private const string FragmentCode = @"
#version 450

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 1, binding = 2) uniform sampler SurfaceSampler;

void main()
{
    fsout_color =  texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
}";

        private readonly ushort[] _indices;

        private readonly VertexPositionTexture[] _vertices;
        private readonly Application _app;
        private CommandList _cl;
        private DeviceBuffer _indexBuffer;
        private Pipeline _pipeline;
        private DeviceBuffer _projectionBuffer;
        private ResourceSet _projViewSet;
        private Texture _surfaceTexture;
        private TextureView _surfaceTextureView;
        private float _ticks;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _viewBuffer;
        private DeviceBuffer _worldBuffer;
        private ResourceSet _worldTextureSet;


        public Renderer3D(Application app)
        {
            _app = app;
            _vertices = GetCubeVertices();
            _indices = GetCubeIndices();
        }

        public void CreateResources(ResourceFactory factory)
        {
            Log.Debug("Loading resources...");
            _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _worldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _vertexBuffer =
                factory.CreateBuffer(new BufferDescription(
                    (uint) (VertexPositionTexture.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            _app.GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);

            _indexBuffer =
                factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint) _indices.Length,
                    BufferUsage.IndexBuffer));
            _app.GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

            var x = new TextureDescription(50, 50, 1, 1, 1, PixelFormat.R8_UInt, TextureUsage.Sampled,
                TextureType.Texture2D);
            var xd = factory.CreateTexture(x);
            _surfaceTextureView = factory.CreateTextureView(xd);

            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                            VertexElementFormat.Float3),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate,
                            VertexElementFormat.Float2))
                },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main")));

            var projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer,
                        ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer,
                        ShaderStages.Vertex)));

            var worldTextureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer,
                        ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly,
                        ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler,
                        ShaderStages.Fragment)));

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] {projViewLayout, worldTextureLayout},
                _app.MainSwapchain.Framebuffer.OutputDescription));

            _projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                _projectionBuffer,
                _viewBuffer));

            _worldTextureSet = factory.CreateResourceSet(new ResourceSetDescription(
                worldTextureLayout,
                _worldBuffer,
                _surfaceTextureView,
                _app.GraphicsDevice.Aniso4xSampler));

            _cl = factory.CreateCommandList();
            Log.Debug("Resources loaded!");
        }

        public void Draw(float deltaSeconds)
        {
            Log.Debug(deltaSeconds);
            _ticks += deltaSeconds * 1000f;
            _cl.Begin();

            _cl.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float) _app.Window.Width / _app.Window.Height,
                0.5f,
                100f));

            _cl.UpdateBuffer(_viewBuffer, 0, Matrix4x4.CreateLookAt(Vector3.UnitZ * 2.5f, Vector3.Zero, Vector3.UnitY));

            var rotation =
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, _ticks / 1000f)
                * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, _ticks / 3000f);
            _cl.UpdateBuffer(_worldBuffer, 0, ref rotation);

            _cl.SetFramebuffer(_app.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.DarkRed);
            _cl.ClearDepthStencil(1f);
            _cl.SetPipeline(_pipeline);
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _cl.SetGraphicsResourceSet(0, _projViewSet);
            _cl.SetGraphicsResourceSet(1, _worldTextureSet);
            _cl.DrawIndexed(36, 1, 0, 0, 0);

            _cl.End();
            _app.GraphicsDevice.SubmitCommands(_cl);
            _app.GraphicsDevice.SwapBuffers(_app.MainSwapchain);
            _app.GraphicsDevice.WaitForIdle();
        }

        private static VertexPositionTexture[] GetCubeVertices()
        {
            var vertices = new[]
            {
                // Top
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1)),
                // Bottom                                                             
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Left                                                               
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Right                                                              
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
                // Back                                                               
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Front                                                              
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1))
            };

            return vertices;
        }

        private static ushort[] GetCubeIndices()
        {
            ushort[] indices =
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            return indices;
        }

        public struct VertexPositionTexture
        {
            public const uint SizeInBytes = 20;

            public float PosX;
            public float PosY;
            public float PosZ;

            public float TexU;
            public float TexV;

            public VertexPositionTexture(Vector3 pos, Vector2 uv)
            {
                PosX = pos.X;
                PosY = pos.Y;
                PosZ = pos.Z;
                TexU = uv.X;
                TexV = uv.Y;
            }
        }
    }
}