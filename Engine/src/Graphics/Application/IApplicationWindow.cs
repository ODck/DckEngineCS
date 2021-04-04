using System;
using System.Reactive;
using Autofac;
using Dck.Subject;
using Veldrid;

namespace Dck.Engine.Graphics.Application
{
    public interface IApplicationWindow : IStartable, IDisposable
    {
        DckSubject<float> Rendering { get; }
        DckSubject<(GraphicsDevice, ResourceFactory, Swapchain)> GraphicsDeviceCreated { get; }
        DckSubject<Unit> GraphicsDeviceDestroyed { get; }
        DckSubject<Unit> Resized { get; }
        DckSubject<KeyEvent> KeyPressed { get; }

        uint Width { get; }
        uint Height { get; }
    }
}