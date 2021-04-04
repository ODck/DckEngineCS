using System;
using System.Reactive;
using Dck.Engine.Core;
using Dck.Engine.Logging;
using Dck.Engine.Settings;
using Dck.Engine.UI;
using Dck.Subject;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace Dck.Engine.Graphics.Application
{
    public class GraphicalWindow : IApplicationWindow
    {
        //Injected
        private readonly Input _input;

        private readonly DisposeCollectorResourceFactory _factory;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Sdl2Window _window;
        
        private ImGuiManager _imGui;
        private Application _application;
        private Clock _stopwatch;
        private bool _windowResized = true;

        public GraphicalWindow(Input input)
        {
            // ReSharper disable once InconsistentNaming
            var windowCI = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = Constants.Title
            };
            _window = VeldridStartup.CreateWindow(ref windowCI);
            _window.Resized += () => _windowResized = true;
            _window.KeyDown += OnKeyDown;

            var options = new GraphicsDeviceOptions(
                false,
                PixelFormat.R16_UNorm,
                true,
                ResourceBindingModel.Improved,
                true,
                true);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options);
            _factory = new DisposeCollectorResourceFactory(_graphicsDevice.ResourceFactory);
            
            _input = input;
        }

        public DckSubject<float> Rendering { get; } = new();
        public DckSubject<(GraphicsDevice, ResourceFactory, Swapchain)> GraphicsDeviceCreated { get; } = new();
        public DckSubject<Unit> GraphicsDeviceDestroyed { get; } = new();
        public DckSubject<Unit> Resized { get; } = new();
        public DckSubject<KeyEvent> KeyPressed { get; } = new();
        public uint Width => (uint) _window.Width;
        public uint Height => (uint) _window.Height;
        public void SetApplication(Application app)
        {
            _application = app;
        }

        public void Draw(float deltaTime)
        {
            _imGui.Draw(_stopwatch, _input);
        }

        public GraphicsDevice GraphicsDevice => _graphicsDevice;
        public ResourceFactory ResourceFactory => _factory;

        public void Start()
        {
            Log.Info($"{Constants.Title} Started in Graphical Mode");
            GraphicsDeviceCreated.Trigger((_graphicsDevice, _factory, _graphicsDevice.MainSwapchain));
            _imGui = new ImGuiManager(_application);
            _stopwatch = Clock.StartNew();
            while (_window.Exists) Tick(_stopwatch);

            Log.Info($"{Constants.Title} Exiting...");
            Dispose();
        }

        public void Dispose()
        {
            Log.Debug<GraphicalWindow>("Disposed Graphics");
            _graphicsDevice.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            _graphicsDevice.Dispose();
            GraphicsDeviceDestroyed.Trigger(Unit.Default);
            GC.SuppressFinalize(this);
        }

        private void Tick(Clock stopwatch)
        {
            var inputSnapshot = _window.PumpEvents();
            _input.UpdateFrameInput(inputSnapshot);
            if (!_window.Exists) return;
            stopwatch.Update();
            if (_windowResized)
            {
                _windowResized = false;
                _graphicsDevice.ResizeMainWindow((uint) _window.Width, (uint) _window.Height);
                Resized.Trigger(Unit.Default);
            }
            
            //Last in the tick always
            Rendering.Trigger(stopwatch.DeltaTime);
        }

        private void OnKeyDown(KeyEvent keyEvent)
        {
            KeyPressed.Trigger(keyEvent);
        }
    }
}