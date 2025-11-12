// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
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

        public static SlnMergeSettings FromFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return (SlnMergeSettings)new XmlSerializer(typeof(SlnMergeSettings)).Deserialize(stream);
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
        /// Preseve All projects.
        /// </summary>
        PreserveAll,
    }
}
