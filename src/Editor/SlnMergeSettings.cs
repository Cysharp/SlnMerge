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
        public ProjectMergeBehavior ProjectMergeBehavior { get; set; }

        public string? MergeTargetSolution { get; set; }


        public class SolutionFolder
        {
            [XmlAttribute] public string FolderPath { get; set; } = default!;

            [XmlAttribute] public string? Guid { get; set; }
        }

        public class NestedProject
        {
            [XmlAttribute] public string ProjectName { get; set; } = default!;
            [XmlAttribute] public string? ProjectGuid { get; set; }
            [XmlAttribute] public string? FolderGuid { get; set; }
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
        /// Preseve All projects.
        /// </summary>
        PreserveAll,
        /// <summary>
        /// Preserve Unity generated project.
        /// </summary>
        PreserveUnity,
        /// <summary>
        /// Preserve Overlay original project.
        /// </summary>
        PreserveOverlay,
    }

    [Flags]
    public enum ProjectMergeBehavior
    {
        None,

        /// <summary>
        /// Throw an exception if the project or solution foler does not exist.
        /// </summary>
        ErrorIfProjectOrFolderDoesNotExist = 1 << 0,
    }

}
