using System;

namespace Dck.Engine.Utils
{
    public abstract class BaseSingleton<T> where T : BaseSingleton<T>
    {
        private static readonly Lazy<T> Lazy =
            new(() => Activator.CreateInstance(typeof(T), true) as T ?? throw new InvalidOperationException());

        public static T Instance => Lazy.Value;
    }
}