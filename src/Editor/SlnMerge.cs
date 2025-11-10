// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

// ReSharper disable All

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SlnMerge.Legacy;
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
                // Load SlnMerge settings from .mergesttings
                var slnFileDirectory = Path.GetDirectoryName(solutionFilePath);
                var slnMergeSettings = new SlnMergeSettings();
                var slnMergeSettingsPath = Path.Combine(slnFileDirectory, Path.GetFileName(solutionFilePath) + ".mergesettings");
                if (File.Exists(slnMergeSettingsPath))
                {
                    logger.Debug($"Using SlnMerge Settings: {slnMergeSettingsPath}");
                    slnMergeSettings = SlnMergeSettings.FromFile(slnMergeSettingsPath);
                }
                else
                {
                    logger.Debug($"SlnMerge Settings (Not found): {slnMergeSettingsPath}");
                }

                if (slnMergeSettings.Disabled)
                {
                    logger.Debug("SlnMerge is currently disabled.");
                    resultSolutionContent = solutionFileContent;
                    return true;
                }

                // Determine a overlay solution path.
                var isSlnx = Path.GetExtension(solutionFilePath) == ".slnx";
                var overlaySolutionFilePath = Path.Combine(slnFileDirectory, Path.GetFileNameWithoutExtension(solutionFilePath) + $".Merge.{(isSlnx ? "slnx" : "sln")}");
                var alternativeOverlaySolutionFilePath = Path.Combine(slnFileDirectory, Path.GetFileNameWithoutExtension(solutionFilePath) + $".Merge.{(isSlnx ? "sln" : "slnx")}");
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
                return isSlnx ? SlnMergeXml.TryMerge(solutionFilePath, solutionFileContent, overlaySolutionFilePath, slnMergeSettings, logger, out resultSolutionContent)
                              : SlnMergeLegacy.TryMerge(solutionFilePath, solutionFileContent, overlaySolutionFilePath, slnMergeSettings, logger, out resultSolutionContent);
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
