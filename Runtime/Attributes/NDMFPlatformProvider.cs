using System;

namespace nadena.dev.ndmf.runtime
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class NDMFPlatformProvider : Attribute
    {
        public Type PlatformProviderType { get; }

        public NDMFPlatformProvider(Type platformProviderType)
        {
            PlatformProviderType = platformProviderType;
        }

    }
}
