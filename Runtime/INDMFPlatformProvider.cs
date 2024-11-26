#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.runtime;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

#if NDMF_VRCSDK3_AVATARS
[assembly: NDMFPlatformProvider(typeof(VRChatPlatformProvider))]
#endif

namespace nadena.dev.ndmf.runtime
{
    public interface INDMFPlatformProvider
    {
        string CanonicalName { get; }
        string DisplayName { get; }
        
        Type? AvatarRootComponentType { get; }
    }
    
#if NDMF_VRCSDK3_AVATARS
    // VRChat is hardcoded for now
    class VRChatPlatformProvider : INDMFPlatformProvider
    {
        string INDMFPlatformProvider.CanonicalName => "vrchat"; 
        string INDMFPlatformProvider.DisplayName => "VRChat";

        Type? INDMFPlatformProvider.AvatarRootComponentType => typeof(VRCAvatarDescriptor);
    }
#endif

    public sealed class PlatformRegistry
    {
        public static readonly PlatformRegistry Instance = new PlatformRegistry();

        INDMFPlatformProvider[] PlatformProviders { get; }
        Type[] AvatarRootComponentTypes { get; }

        internal bool IsAvatarRoot(GameObject gameObject)
        {
            foreach (var type in AvatarRootComponentTypes)
            {
                if (gameObject.TryGetComponent(type, out _)) return true;
            }
            return false;
        }

        internal IEnumerable<Component> GetAvatarDescriptorsInChildren(GameObject gameObject)
        {
            return AvatarRootComponentTypes.SelectMany(gameObject.GetComponentsInChildren);
        }

        internal IEnumerable<Transform> GetAvatarRootsInChildren(GameObject gameObject)
        {
            return GetAvatarDescriptorsInChildren(gameObject)
                .Select(component => component.transform)
                .Distinct();
        }

        PlatformRegistry(IEnumerable<Type> platformProviderTypes)
        {
            PlatformProviders = platformProviderTypes
                .Distinct()
                .Select(type => type.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()))
                .OfType<INDMFPlatformProvider>().ToArray();

            AvatarRootComponentTypes = 
                Enumerable.Empty<Type>()
                    .Concat(PlatformProviders
                        .Select(platform => platform.AvatarRootComponentType)
                        .OfType<Type>())
                    .Distinct().ToArray();
        }

        PlatformRegistry() : this(
            AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                    assembly => assembly.GetCustomAttributes(typeof(NDMFPlatformProvider), false))
                .OfType<NDMFPlatformProvider>()
                .Select(attr => attr.PlatformProviderType)
                .ToArray()
        )
        {
        }
    }

}
