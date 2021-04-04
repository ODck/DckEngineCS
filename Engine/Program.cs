using Autofac;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Graphics.Services;
using Dck.Engine.Logging;

namespace Dck.Engine
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        private static IContainer Container { get; set; } = null!;

        private static void Main(string[] args)
        {
            Log.Debug<Program>("Hello world!");
            Container = InstallBindings();
        }

        private static IContainer InstallBindings()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Application>().As<IStartable>();
            builder.RegisterType<GraphicalWindow>().As<IApplicationWindow>();
            builder.RegisterType<Input>();
            builder.RegisterType<Camera>();
            return builder.Build();
        }
    }
}