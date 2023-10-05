﻿#region

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using nadena.dev.ndmf.runtime;
using UnityEngine;

#endregion

namespace nadena.dev.ndmf.util
{
    public static class MiscHelpers
    {
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

        [CanBeNull]
        public static string AvatarRootPath(this GameObject child)
        {
            return RuntimeUtil.AvatarRootPath(child);
        }

        [CanBeNull]
        public static string AvatarRootPath(this Component child)
        {
            return child.gameObject.AvatarRootPath();
        }
    }
}