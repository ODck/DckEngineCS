using System;
using System.Globalization;
using System.Numerics;
using System.Reactive.Disposables;
using Dck.Engine.Core;
using Dck.Engine.Graphics.Application;
using Dck.Subject;
using ImGuiNET;
using Veldrid;

namespace Dck.Engine.UI
{
    public class ImGuiManager : IDisposable
    {
        private readonly Application _app;
        private readonly ImGuiRenderer _renderer;
        private readonly CompositeDisposable _disposable = new();

        private bool _uiopen = true;

        public ImGuiManager(Application application)
        {
            _app = application;
            _renderer = new ImGuiRenderer(_app.GraphicsDevice,
                _app.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                (int) _app.Window.Width, (int) _app.Window.Height);
            _app.Window.Resized.Connect(_ => OnWindowResized()).AddTo(_disposable);
        }

        private void OnWindowResized()
        {
            _renderer.WindowResized((int) _app.Window.Width, (int) _app.Window.Height);
        }

        public void Draw(Clock clock, Input input)
        {
            _renderer.Update(clock.DeltaTime, input.FrameSnapshot);

            ImGui.BeginMainMenuBar();
            ImGui.Text($"Really cool engine uwu");
            ImGui.EndMainMenuBar();

            ImGuiWindowFlags flags = 0;
            flags |= ImGuiWindowFlags.NoMove;
            flags |= ImGuiWindowFlags.NoBackground;
            flags |= ImGuiWindowFlags.NoTitleBar;
            flags |= ImGuiWindowFlags.NoResize;
            
            ImGui.SetNextWindowPos(new Vector2(0,20));
            ImGui.SetNextWindowSize(new Vector2(200,100));
            ImGui.Begin("Clock", flags);
            ImGui.Text($"Elapsed: {clock.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)}");
            ImGui.Text($"FPS: {clock.FPS}");
            ImGui.End();

            //ImGui.ShowDemoWindow();
            
            _renderer.Render(_app.GraphicsDevice, _app.CommandList);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        
        //TODO: Enable ImGui widgets
        //TODO: Dispose
    }
}