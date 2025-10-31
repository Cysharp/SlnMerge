// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SlnMerge.Xml
{
    internal static class SlnMergeXml
    {
        public static SlnxFile Merge(SlnxFile slnxBase, SlnxFile slnxOverlay, SlnMergeSettings settings, ISlnMergeLogger logger)
        {
            ValidateSettings(settings);

            slnxBase = slnxBase.Clone();
            slnxOverlay = slnxOverlay.Clone();

            slnxOverlay.RewritePaths(slnxBase);

            // Handle project conflicts
            foreach (var proj in slnxOverlay.Root.Projects)
            {
                if (slnxBase.Root.Projects.ContainsKey(proj.Key))
                {
                    switch (settings.ProjectConflictResolution)
                    {
                        case ProjectConflictResolution.PreserveOverlay:
                            slnxBase.Root.RemoveProject(proj.Value);
                            break;
                        case ProjectConflictResolution.PreserveUnity:
                            slnxOverlay.Root.RemoveProject(proj.Value);
                            break;
                        case ProjectConflictResolution.PreserveAll:
                            break;
                    }
                }
            }

            // Add solution folders from settings
            foreach (var folderBySetting in settings.SolutionFolders)
            {
                slnxBase.Root.AddOrMergeFolder(new FolderElement(NormalizeSolutionFolderPath(folderBySetting.FolderPath)), settings.ProjectConflictResolution);
            }

            // Merge overlay into base
            foreach (var overlayChild in slnxOverlay.Root.Children)
            {
                if (overlayChild is FolderElement overlayFolder)
                {
                    slnxBase.Root.AddOrMergeFolder(overlayFolder, settings.ProjectConflictResolution);
                }
                else if (overlayChild is ConfigurationsElement overlayConfig)
                {
                    slnxBase.Root.AddOrMergeConfigurations(overlayConfig, settings.ProjectConflictResolution);
                }
                else
                {
                    slnxBase.Root.AddChild(overlayChild);
                }
            }

            // Add or move projects into solution folders
            var detachedProjects = new List<(SlnMergeSettings.NestedProject NestedSetting, ProjectElement[] Projects)>();
            foreach (var nested in settings.NestedProjects)
            {
                detachedProjects.Add((nested, DetachProjectsFromFolders(nested.ProjectName, slnxBase.Root)));
            }

            foreach (var (nested, projects) in detachedProjects)
            {
                if (slnxBase.Root.Folders.TryGetValue(NormalizeSolutionFolderPath(nested.FolderPath), out var folderElement))
                {
                    foreach (var proj in projects)
                    {
                        slnxBase.Root.AddProject(proj, folderElement);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"The folder '{nested.FolderPath}' specified as the nesting destination for the project '{nested.ProjectName}' does not exist.");
                }
            }

            return slnxBase;
        }

        private static void ValidateSettings(SlnMergeSettings settings)
        {
            foreach (var nested in settings.NestedProjects)
            {
                if (string.IsNullOrWhiteSpace(nested.FolderPath))
                {
                    throw new InvalidOperationException($"The folder path for the nested project '{nested.ProjectName}' is not specified.");
                }

                if (string.IsNullOrWhiteSpace(nested.ProjectName))
                {
                    throw new InvalidOperationException($"The project name for the nested project '{nested.ProjectName}' is not specified.");
                }
            }

            foreach (var folder in settings.SolutionFolders)
            {
                if (string.IsNullOrWhiteSpace(folder.FolderPath))
                {
                    throw new InvalidOperationException($"The folder path for the solution folder is not specified.");
                }
            }
        }

        private static ProjectElement[] DetachProjectsFromFolders(string matchName, Node root)
        {
            if (root is IElement element && element.Children.Length != 0)
            {
                var nestedProjectNamePattern = new Regex($"^{Regex.Escape(matchName).Replace(@"\*", ".*").Replace(@"\?", ".")}$");
                var detachedProjects = new List<ProjectElement>();
                var newChildren = new List<Node>(element.Children.Length);

                foreach (var child in element.Children)
                {
                    if (child is ProjectElement proj && nestedProjectNamePattern.IsMatch(Path.GetFileNameWithoutExtension(proj.Path)))
                    {
                        detachedProjects.Add(proj);
                    }
                    else
                    {
                        detachedProjects.AddRange(DetachProjectsFromFolders(matchName, child));
                        newChildren.Add(child);
                    }
                }
                element.Children = newChildren.ToArray();
                return detachedProjects.ToArray();
            }

            return Array.Empty<ProjectElement>();
        }

        private static string NormalizeSolutionFolderPath(string path)
        {
            return $"/{path.Trim('/', '\\')}/";
        }

    }
}
