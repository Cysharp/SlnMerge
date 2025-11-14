// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;

namespace SlnMerge
{
    public class SlnMergeSettings
    {
        public bool Disabled { get; set; }

        public SolutionFolder[] SolutionFolders { get; set; } = Array.Empty<SolutionFolder>();
        public NestedProject[] NestedProjects { get; set; } = Array.Empty<NestedProject>();
        public ProjectConflictResolution ProjectConflictResolution { get; set; }

        public string? MergeTargetSolution { get; set; }


        public class SolutionFolder
        {
            [XmlAttribute] public string FolderPath { get; set; } = default!;
        }

        public class NestedProject
        {
            [XmlAttribute] public string ProjectName { get; set; } = default!;
            [XmlAttribute] public string FolderPath { get; set; } = default!;
        }

        public static bool TryLoad(string solutionFilePath, string? slnMergeSettingsPath, [NotNullWhen(true)] out string? finalSlnMergeSettingsPath, [NotNullWhen(true)] out SlnMergeSettings? settings)
        {
            if (string.IsNullOrWhiteSpace(slnMergeSettingsPath))
            {
                return TryLoadBySolutionPath(solutionFilePath, out finalSlnMergeSettingsPath, out settings);
            }
            else
            {
                if (File.Exists(slnMergeSettingsPath))
                {
                    settings = FromFile(slnMergeSettingsPath!);
                    finalSlnMergeSettingsPath = slnMergeSettingsPath!;
                    return true;
                }
                else
                {
                    settings = null;
                    finalSlnMergeSettingsPath = null;
                    return false;
                }
            }
        }

        public static bool TryLoadBySolutionPath(string solutionFilePath, [NotNullWhen(true)] out string? loadedSlnMergeSettingsPath, [NotNullWhen(true)] out SlnMergeSettings? settings)
        {
            var solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);
            var isSlnx = Path.GetExtension(solutionFilePath) == ".slnx";

            // Load SlnMerge settings from .mergesttings
            var slnFileDirectory = Path.GetDirectoryName(solutionFilePath)!;
            var slnMergeSettingsPath = Path.Combine(slnFileDirectory, $"{solutionName}.{(isSlnx ? "slnx" : "sln")}.mergesettings");
            var alternativeSlnMergeSettingsPath = Path.Combine(slnFileDirectory, $"{solutionName}.{(isSlnx ? "sln" : "slnx")}.mergesettings");

            if (File.Exists(slnMergeSettingsPath))
            {
                loadedSlnMergeSettingsPath = slnMergeSettingsPath;
                settings = SlnMergeSettings.FromFile(slnMergeSettingsPath);
            }
            else if (File.Exists(alternativeSlnMergeSettingsPath))
            {
                loadedSlnMergeSettingsPath = alternativeSlnMergeSettingsPath;
                settings = SlnMergeSettings.FromFile(alternativeSlnMergeSettingsPath);
            }
            else
            {
                loadedSlnMergeSettingsPath = null;
                settings = null;
            }

            return settings != null;
        }

        public static SlnMergeSettings FromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return (SlnMergeSettings)new XmlSerializer(typeof(SlnMergeSettings)).Deserialize(stream);
        }
    }

    public enum ProjectConflictResolution
    {
        /// <summary>
        /// Preserve Unity generated project.
        /// </summary>
        PreserveUnity,
        /// <summary>
        /// Preserve Overlay original project.
        /// </summary>
        PreserveOverlay,
        /// <summary>
        /// Preseve All projects.
        /// </summary>
        PreserveAll,
    }
}
