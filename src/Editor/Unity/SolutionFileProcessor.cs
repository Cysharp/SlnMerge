// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SlnMerge.Unity
{
    [InitializeOnLoad]
    public class SolutionFileProcessor : AssetPostprocessor
    {
        private static readonly bool _hasVsForUnity;

        static SolutionFileProcessor()
        {
            // NOTE: If Visual Studio Tools for Unity is enabled, the .sln file will be rewritten after our process.
            // Use VSTU hook to prevent from discarding our changes.
            var typeProjectFilesGenerator = Type.GetType("SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator, SyntaxTree.VisualStudio.Unity.Bridge");
            if (typeProjectFilesGenerator != null)
            {
                _hasVsForUnity = true;

                var typeFileGenerationHandler = Type.GetType("SyntaxTree.VisualStudio.Unity.Bridge.FileGenerationHandler, SyntaxTree.VisualStudio.Unity.Bridge");
                var fieldSolutionFileGeneration = typeProjectFilesGenerator.GetField("SolutionFileGeneration");
                var fieldSolutionFileGenerationDelegate = (Delegate)fieldSolutionFileGeneration.GetValue(null);

                var d = Delegate.CreateDelegate(typeFileGenerationHandler, typeof(SolutionFileProcessor), "Merge");
                if (fieldSolutionFileGenerationDelegate == null)
                {
                    fieldSolutionFileGeneration.SetValue(null, d);
                }
                else
                {
                    fieldSolutionFileGeneration.SetValue(null, Delegate.Combine(fieldSolutionFileGenerationDelegate, d));
                }
            }
        }

        private static bool IsUnityVsIntegrationEnabled
        {
            get
            {
                if (!_hasVsForUnity) return false;

                var t = typeof(EditorApplication).Assembly.GetType("UnityEditor.VisualStudioIntegration.UnityVSSupport");
                if (t == null) return false;

                var methodShouldUnityVSBeActive = t.GetMethod("ShouldUnityVSBeActive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (methodShouldUnityVSBeActive == null) return false;

                return (bool)methodShouldUnityVSBeActive.Invoke(null, new object[0]);
            }
        }

        private static string OnGeneratedSlnSolution(string path, string content)
        {
            return IsUnityVsIntegrationEnabled
                ? content /* Visual Studio with VSTU */
                : Merge(path, content); /* other editors (Rider, VSCode ...) */
        }

        private static string Merge(string path, string content)
        {
            var logger = SlnMergeUnityLogger.Instance;
            var mergeSettingsCustomLocation = SlnMergeUserSettings.Instance.MergeSettingsCustomLocation;

            if (SlnMergeSettings.TryLoad(path, mergeSettingsCustomLocation, out var finalSlnMergeSettingsPath, out var slnMergeSettings))
            {
                logger.Debug($"Using SlnMerge Settings: {finalSlnMergeSettingsPath}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(mergeSettingsCustomLocation))
                {
                    logger.Debug($"No SlnMerge settings found.");
                }
                else
                {
                    logger.Warn($"Specified SlnMerge settings was not found at: {mergeSettingsCustomLocation}");
                }
            }

            if (SlnMerge.TryMerge(path, content, slnMergeSettings, logger, out var solutionContent))
            {
                return solutionContent;
            }

            return content;
        }

        private class SlnMergeUnityLogger : ISlnMergeLogger
        {
            public static ISlnMergeLogger Instance { get; } = new SlnMergeUnityLogger();

            private SlnMergeUnityLogger() { }

            public void Warn(string message)
            {
                UnityEngine.Debug.LogWarning(message);
            }

            public void Error(string message, Exception ex)
            {
                UnityEngine.Debug.LogError($"[SlnMerge] {message}");
                if (ex != null)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            public void Information(string message)
            {
                UnityEngine.Debug.Log($"[SlnMerge] {message}");
            }

            public void Debug(string message)
            {
                if (SlnMergeUserSettings.Instance.VerboseLogging)
                {
                    UnityEngine.Debug.Log($"[SlnMerge] {message}");
                }
            }
        }
    }
}
