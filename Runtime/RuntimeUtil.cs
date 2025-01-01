﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace nadena.dev.ndmf.runtime
{
    /// <summary>
    /// A collection of general-purpose utilities that are available from Runtime-scope scripts.
    /// </summary>
    public static class RuntimeUtil
    {
        /// <summary>
        /// Invoke this function to register a callback with EditorApplication.delayCall from a context that cannot
        /// access EditorApplication.
        /// </summary>
        public static Action<Action> DelayCall { get; internal set; }

        static RuntimeUtil()
        {
            DelayCall = action => { throw new Exception("delayCall() cannot be called during static initialization"); };
        }

        // Shadow the VRC-provided methods to avoid deprecation warnings
        internal static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            if (!obj.TryGetComponent<T>(out var component)) component = obj.AddComponent<T>();
            return component;
        }

        internal static T GetOrAddComponent<T>(this Component obj) where T : Component
        {
            return obj.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// Returns whether the editor is in play mode.
        /// </summary>
#if UNITY_EDITOR
        public static bool IsPlaying => UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
        public static bool IsPlaying => true;
#endif

        /// <summary>
        /// Returns the relative path from root to child, or null is child is not a descendant of root.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        [CanBeNull]
        public static string RelativePath(GameObject root, GameObject child)
        {
            if (root == child) return "";

            List<string> pathSegments = new List<string>();
            while (child != root && child != null)
            {
                pathSegments.Add(child.name);
                child = child.transform.parent?.gameObject;
            }

            if (child == null && root != null) return null;

            pathSegments.Reverse();
            return String.Join("/", pathSegments);
        }

        /// <summary>
        /// Returns the path of a game object relative to the avatar root, or null if the avatar root could not be
        /// located.
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        [CanBeNull]
        public static string AvatarRootPath(GameObject child)
        {
            if (child == null) return null;
            var avatar = FindAvatarInParents(child.transform);
            if (avatar == null) return null;
            return RelativePath(avatar.gameObject, child);
        }

        /// <summary>
        /// Check whether the target component is the root of the avatar.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsAvatarRoot(Transform target)
        {
            return PlatformRegistry.Instance.IsAvatarRoot(target.gameObject);
        }

        /// <summary>
        /// Return a list of avatar roots in the current Scene(s). This function is a heuristic, and the details
        /// of its operation may change in patch releases.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<GameObject> FindAvatarRoots(GameObject root = null)
        {
            if (root == null)
            {
                var sceneCount = SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                    {
                        foreach (var avatar in FindAvatarRoots(sceneRoot))
                        {
                            yield return avatar;
                        }
                    }
                }
            }
            else
            {
                GameObject priorRoot = null;
                var candidates = PlatformRegistry.Instance.GetAvatarRootsInChildren(root);
                foreach (var candidate in candidates)
                {
                   
                    var gameObject = candidate.gameObject;
                    // Ignore nested candidates
                    if (priorRoot != null && RelativePath(priorRoot, gameObject) != null) continue;

                    priorRoot = gameObject;
                    yield return candidate.gameObject;
                }
            }
        }

        /// <summary>
        /// Returns the component marking the root of the avatar.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Transform FindAvatarInParents(Transform target)
        {
            while (target != null)
            {
                if (IsAvatarRoot(target)) return target;
                target = target.parent;
            }

            return null;
        }
        
        /// <summary>
        /// Returns the component marking the root of the avatar.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        internal static IEnumerable<Transform> FindAvatarsInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in PlatformRegistry.Instance.GetAvatarRootsInChildren(root))
                {
                    if (IsAvatarRoot(candidate)) yield return candidate;
                }
            }
        }
    }
}