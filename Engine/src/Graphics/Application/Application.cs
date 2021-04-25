using System;
using System.Reactive.Disposables;
using Autofac;
using Dck.Engine.Core;
using Dck.Engine.Graphics.Services;
using Dck.Engine.Logging;
using Dck.Engine.TEST;
using Dck.Subject;
using Veldrid;

namespace Dck.Engine.Graphics.Application
{
    public class Application : IStartable, IDisposable
    {
        private readonly CompositeDisposable _disposable = new();
        private readonly Renderer3D _renderer;
        private readonly Camera _camera;

        public Application(IApplicationWindow window, Camera.Factory cameraFactory)
        {
            Window = window;
            Window.Resized.Connect(_ => HandleWindowResize()).AddTo(_disposable);
            Window.GraphicsDeviceCreated.Connect(x => OnGraphicsDeviceCreated(x.Item1, x.Item2, x.Item3))
                .AddTo(_disposable);
            Window.GraphicsDeviceDestroyed.Connect(_ => OnDeviceDestroyed()).AddTo(_disposable);
            Window.Rendering.Connect(PreDraw).AddTo(_disposable);
            Window.Rendering.Connect(Draw).AddTo(_disposable);
            Window.KeyPressed.Connect(OnKeyDown).AddTo(_disposable);

            _camera = cameraFactory(Window.Width, Window.Height);
            _renderer = new Renderer3D(this);// new GraphicRenderer(_camera);
        }

        public IApplicationWindow Window { get; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain MainSwapchain { get; private set; }
        public CommandList CommandList { get; private set; }

        public void Dispose()
        {
            _disposable?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            Window.Start();
        }

        private void OnKeyDown(KeyEvent obj)
        {
            Log.Debug(obj);
        }

        private void PreDraw(float deltaTime)
        {
            _camera.Update(deltaTime);
        }

        private void Draw(float deltaTime)
        {
            if (GraphicsDevice == null) return;
            CommandList.Begin();
            ResetFramebuffer();

            _renderer.RenderFrame(deltaTime);
            Window.Draw(deltaTime);

            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.WaitForIdle();
            GraphicsDevice.SwapBuffers();
        }

        private void OnDeviceDestroyed()
        {
            GraphicsDevice = null;
            ResourceFactory = null;
            MainSwapchain = null;
            CommandList = null;
        }

        private void HandleWindowResize()
        {
            _camera.WindowResized(Window.Width, Window.Height);
        }

        private void OnGraphicsDeviceCreated(GraphicsDevice graphicsDevice, ResourceFactory factory,
            Swapchain swapchain)
        {
            Log.Debug("Graphics device created");
            Window.SetApplication(this);
            GraphicsDevice = graphicsDevice;
            ResourceFactory = factory;
            MainSwapchain = swapchain;
            CommandList = factory.CreateCommandList();
            _renderer.SetCommandList(CommandList);
            _renderer.SetCamera(_camera);
            _renderer.CreateResources(graphicsDevice);
        }

        private void ResetFramebuffer()
        {
            CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        }
    }
}