// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnMerge.Editor
{
    internal static class PathHelper
    {
        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar));
        }

        public static string MakeRelative(string basePath, string targetPath)
        {
            var basePathParts = basePath.Split('/', '\\');
            var targetPathParts = targetPath.Split('/', '\\');

            var targetPathFixed = targetPath;
            for (var i = 0; i < Math.Min(basePathParts.Length, targetPathParts.Length); i++)
            {
                var basePathPrefix = string.Join("/", basePathParts.Take(i + 1));
                var targetPathPrefix = string.Join("/", targetPathParts.Take(i + 1));

                if (basePathPrefix == targetPathPrefix)
                {
                    var pathPrefix = basePathPrefix;
                    var upperDirCount = (basePathParts.Length - i - 2); // excepts a filename

                    var sb = new StringBuilder();
                    for (var j = 0; j < upperDirCount; j++)
                    {
                        sb.Append("..");
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                    sb.Append(targetPath.Substring(pathPrefix.Length + 1));

                    targetPathFixed = sb.ToString();
                }
                else
                {
                    break;
                }
            }

            return targetPathFixed;
        }
    }
}
