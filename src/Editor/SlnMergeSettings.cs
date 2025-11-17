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
        public ProcessingPolicy DefaultProcessingPolicy { get; set; } = ProcessingPolicy.Merge;

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

        public void Save(string path)
        {
            using var stream = File.Create(path);
            new XmlSerializer(typeof(SlnMergeSettings)).Serialize(stream, this);
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
            try
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
                    settings = FromFile(slnMergeSettingsPath);
                }
                else if (File.Exists(alternativeSlnMergeSettingsPath))
                {
                    loadedSlnMergeSettingsPath = alternativeSlnMergeSettingsPath;
                    settings = FromFile(alternativeSlnMergeSettingsPath);
                }
                else
                {
                    loadedSlnMergeSettingsPath = null;
                    settings = null;
                }

                return settings != null;
            }
            catch
            {
                loadedSlnMergeSettingsPath = null;
                settings = null;
                return false;
            }
        }

        public static SlnMergeSettings FromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return (SlnMergeSettings)new XmlSerializer(typeof(SlnMergeSettings)).Deserialize(stream);
        }

        public static bool TryLoadFromFile(string path, [NotNullWhen(true)] out SlnMergeSettings? settings)
        {
            try
            {
                settings = FromFile(path);
                return true;
            }
            catch
            {
                settings = null;
                return false;
            }
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
        /// Preserve All projects.
        /// </summary>
        PreserveAll,
    }

    public enum ProcessingPolicy
    {
        /// <summary>
        /// Merge solutions
        /// </summary>
        Merge,
        /// <summary>
        /// Only process nested projects.
        /// </summary>
        NestedProjectOnly,
        /// <summary>
        /// Disable SlnMerge processing.
        /// </summary>
        Disabled,
    }

    public enum ProcessingPolicyOverride
    {
        /// <summary>
        /// Merge solutions
        /// </summary>
        Merge,
        /// <summary>
        /// Only process nested projects.
        /// </summary>
        NestedProjectOnly,
        /// <summary>
        /// Disable SlnMerge processing.
        /// </summary>
        Disabled,

        /// <summary>
        /// Unspecified processing policy.
        /// </summary>
        Unspecified,
    }

    public static class ProcessingPolicyExtensions
    {
        public static string GetDescription(this ProcessingPolicy policy) => policy switch
        {
            ProcessingPolicy.Merge => "The solution will be merged by default.",
            ProcessingPolicy.NestedProjectOnly => "Only the settings for nested projects will be applied, and the solution will not be merged.",
            ProcessingPolicy.Disabled => "Merging and other settings application are disabled by default.",
            _ => string.Empty,
        };

        public static string GetDescription(this ProcessingPolicyOverride policy) => policy switch
        {
            ProcessingPolicyOverride.Merge => "The solution will be merged.",
            ProcessingPolicyOverride.NestedProjectOnly => "Only the settings for nested projects will be applied, and the solution will not be merged.",
            ProcessingPolicyOverride.Disabled => "Merging and other settings application are disabled.",
            ProcessingPolicyOverride.Unspecified => "Follow the project's default merge settings",
            _ => string.Empty,
        };
    }
}
