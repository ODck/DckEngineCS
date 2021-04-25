using System;
using System.IO;
using System.Reflection;
using Autofac;
using Dck.Engine;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Graphics.Services;
using Dck.Engine.Logging;
using Dck.Engine.TEST;

namespace Sandbox
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        private static void Main(string[] args)
        {
            var program = new EntryPoint();
            program.Start(args);
        }
    }
}