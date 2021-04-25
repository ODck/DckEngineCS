using Autofac;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Graphics.Services;
using Dck.Engine.Logging;

namespace Dck.Engine
{
    public class EntryPoint
    {
        private IContainer Container { get; set; } = null!;

        public void Start(string[] args)
        {
            Log.Debug<EntryPoint>("Hello world!");
            Container = InstallBindings();
        }
        
        private static IContainer InstallBindings()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Application>().As<IStartable>();
            builder.RegisterType<GraphicalWindow>().As<IApplicationWindow>();
            builder.RegisterType<Input>().SingleInstance();
            builder.RegisterType<Camera>().SingleInstance();
            return builder.Build();
        }
    }
}