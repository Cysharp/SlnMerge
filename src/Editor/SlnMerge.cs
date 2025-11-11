// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

// ReSharper disable All

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SlnMerge.Legacy;
using SlnMerge.Persistence;
using SlnMerge.Xml;

namespace SlnMerge
{
    public static class SlnMerge
    {
        public static bool TryMerge(string solutionFilePath, string solutionFileContent, ISlnMergeLogger logger, [NotNullWhen(true)] out string? resultSolutionContent)
        {
            logger.Debug(solutionFileContent);
            try
            {
                var solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);
                var isSlnx = Path.GetExtension(solutionFilePath) == ".slnx";

                // Load SlnMerge settings from .mergesttings
                var slnFileDirectory = Path.GetDirectoryName(solutionFilePath);
                var slnMergeSettings = new SlnMergeSettings();
                var slnMergeSettingsPath = Path.Combine(slnFileDirectory, $"{solutionName}.{(isSlnx ? "slnx" : "sln")}.mergesettings");
                var alternativeSlnMergeSettingsPath = Path.Combine(slnFileDirectory, $"{solutionName}.{(isSlnx ? "sln" : "slnx")}.mergesettings");

                if (File.Exists(slnMergeSettingsPath))
                {
                    logger.Debug($"Using SlnMerge Settings: {slnMergeSettingsPath}");
                    slnMergeSettings = SlnMergeSettings.FromFile(slnMergeSettingsPath);
                }
                else if (File.Exists(alternativeSlnMergeSettingsPath))
                {
                    logger.Debug($"Using SlnMerge Settings: {alternativeSlnMergeSettingsPath}");
                    slnMergeSettings = SlnMergeSettings.FromFile(alternativeSlnMergeSettingsPath);
                }
                else
                {
                    logger.Debug($"SlnMerge Settings (Not found): {slnMergeSettingsPath} or {alternativeSlnMergeSettingsPath}");
                }

                if (slnMergeSettings.Disabled)
                {
                    logger.Debug("SlnMerge is currently disabled.");
                    resultSolutionContent = solutionFileContent;
                    return true;
                }

                // Determine a overlay solution path.
                var overlaySolutionFilePath = Path.Combine(slnFileDirectory, $"{solutionName}.Merge.{(isSlnx ? "slnx" : "sln")}");
                var alternativeOverlaySolutionFilePath = Path.Combine(slnFileDirectory, $"{solutionName}.Merge.{(isSlnx ? "sln" : "slnx")}");
                if (!string.IsNullOrEmpty(slnMergeSettings.MergeTargetSolution))
                {
                    overlaySolutionFilePath = PathHelper.NormalizePath(Path.Combine(slnFileDirectory, slnMergeSettings.MergeTargetSolution));
                }
                if (!File.Exists(overlaySolutionFilePath))
                {
                    if (File.Exists(alternativeOverlaySolutionFilePath))
                    {
                        overlaySolutionFilePath = alternativeOverlaySolutionFilePath;
                    }
                    else
                    {
                        logger.Warn($"Cannot load the solution file to merge. skipped: {overlaySolutionFilePath}");
                        resultSolutionContent = null;
                        return false;
                    }
                }

                // Merge the solutions.
                logger.Debug($"Start merging: Base={solutionFilePath}; Overlay={overlaySolutionFilePath}");

                var succeeded = SlnMergePersistence.TryMerge(solutionFilePath, solutionFileContent, overlaySolutionFilePath, slnMergeSettings, logger, out resultSolutionContent);

                logger.Debug($"TryMerge: Succeeded:{succeeded}\n{resultSolutionContent}");

                return succeeded;
            }
            catch (Exception e)
            {
                logger.Error("Failed to merge the solutions", e);
                resultSolutionContent = null;
                return false;
            }

        }
    }
}
