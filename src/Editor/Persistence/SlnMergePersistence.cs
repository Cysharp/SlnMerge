// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using SlnMerge.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlnMerge.Persistence
{
    internal class SlnMergePersistence
    {
        public static bool TryMerge(string solutionFilePath, string solutionFileContent, string overlaySolutionFilePath, SlnMergeSettings slnMergeSettings, ISlnMergeLogger logger, out string? resultSolutionContent)
        {
            try
            {
                var baseSlnSerializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath)!;
                var overlaySlnSerializer = SolutionSerializers.GetSerializerByMoniker(overlaySolutionFilePath)!;
                var baseSln = baseSlnSerializer.OpenAsync(solutionFilePath, CancellationToken.None).GetAwaiter().GetResult();
                var overlaySln = overlaySlnSerializer.OpenAsync(overlaySolutionFilePath, CancellationToken.None).GetAwaiter().GetResult();
                MergeTo(baseSln, solutionFilePath, overlaySln, overlaySolutionFilePath, slnMergeSettings, logger);

                Func<Stream, SolutionModel, CancellationToken, Task> saveAsyncFunc =
                    (baseSlnSerializer.Name == "Slnx")
                        ? SolutionSerializers.SlnXml.SaveAsync
                        : SolutionSerializers.SlnFileV12.SaveAsync;

                var outputStream = new MemoryStream();
                saveAsyncFunc(outputStream, baseSln, CancellationToken.None).GetAwaiter().GetResult();
                resultSolutionContent = Encoding.UTF8.GetString(outputStream.ToArray());
                return true;
            }
            catch (Exception e)
            {
                logger.Error("Failed to merge the solutions", e);
                resultSolutionContent = null;
                return false;
            }
        }

        internal static void MergeTo(SolutionModel baseSln, string baseSlnPath, SolutionModel overlaySln, string overlaySlnPath, SlnMergeSettings settings, ISlnMergeLogger logger)
        {
            ValidateSettings(settings);

            RewritePaths(overlaySln, overlaySlnPath, baseSlnPath);

            // Handle project conflicts
            foreach (var proj in overlaySln.SolutionProjects)
            {
                var projInBaseSln = baseSln.SolutionProjects.FirstOrDefault(x => x.FilePath == proj.FilePath);
                if (projInBaseSln is not null)
                {
                    switch (settings.ProjectConflictResolution)
                    {
                        case ProjectConflictResolution.PreserveOverlay:
                            baseSln.RemoveProject(projInBaseSln);
                            break;
                        case ProjectConflictResolution.PreserveUnity:
                            overlaySln.RemoveProject(proj);
                            break;
                        case ProjectConflictResolution.PreserveAll:
                            break;
                    }
                }
            }

            // Add solution folders from settings
            foreach (var folderBySetting in settings.SolutionFolders)
            {
                var folderPath = NormalizeSolutionFolderPath(folderBySetting.FolderPath);
                if (baseSln.SolutionFolders.Any(x => string.Equals(x.Path, folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                baseSln.AddFolder(NormalizeSolutionFolderPath(folderBySetting.FolderPath));
            }

            // Merge overlay into base
            foreach (var item in overlaySln.SolutionItems)
            {
                if (item is SolutionFolderModel overlayFolder)
                {
                    var baseFolder = baseSln.SolutionFolders.FirstOrDefault(x => string.Equals(x.Path, overlayFolder.Path, StringComparison.OrdinalIgnoreCase));
                    if (baseFolder is null)
                    {
                        baseFolder = baseSln.AddFolder(overlayFolder.Path);
                        foreach (var prop in overlayFolder.Properties)
                        {
                            var baseProp = baseFolder.AddProperties(prop.Id, prop.Scope);
                            foreach (var (key, value) in prop)
                            {
                                baseProp.Add(key, value);
                            }
                        }
                        baseFolder.Id = overlayFolder.Id;
                    }

                    var files = overlayFolder.Files ?? Array.Empty<string>();
                    foreach (var file in files)
                    {
                        baseFolder.AddFile(file);
                    }
                }
                else if (item is SolutionProjectModel overlayProject)
                {
                    var baseFolder = item.Parent != null
                        ? baseSln.SolutionFolders.First(x => string.Equals(x.Path, item.Parent.Path, StringComparison.OrdinalIgnoreCase))
                        : null;

                    var project = baseSln.AddProject(overlayProject.FilePath, overlayProject.Type, baseFolder);
                    // Copy all project configuration
                    foreach (var overlayDep in overlayProject.Dependencies ?? Array.Empty<SolutionProjectModel>())
                    {
                        var baseDep = baseSln.SolutionProjects.FirstOrDefault(x => x.FilePath == overlayDep.FilePath);
                        project.AddDependency(baseDep);
                    }
                    foreach (var overlayConfigRule in overlayProject.ProjectConfigurationRules ?? Array.Empty<ConfigurationRule>())
                    {
                        project.AddProjectConfigurationRule(overlayConfigRule);
                    }
                }
            }

            foreach (var buildType in overlaySln.BuildTypes)
            {
                if (!baseSln.BuildTypes.Any(x => string.Equals(x, buildType, StringComparison.OrdinalIgnoreCase)))
                {
                    baseSln.AddBuildType(buildType);
                }
            }
            foreach (var platform in overlaySln.Platforms)
            {
                if (!baseSln.Platforms.Any(x => string.Equals(x, platform, StringComparison.OrdinalIgnoreCase)))
                {
                    baseSln.AddPlatform(platform);
                }
            }

            //// Add or move projects into solution folders
            //var detachedProjects = new List<(SlnMergeSettings.NestedProject NestedSetting, ProjectElement[] Projects)>();
            //foreach (var nested in settings.NestedProjects)
            //{
            //    detachedProjects.Add((nested, DetachProjectsFromFolders(nested.ProjectName, slnxBase.Root)));
            //}

            //foreach (var (nested, projects) in detachedProjects)
            //{
            //    if (slnxBase.Root.Folders.TryGetValue(NormalizeSolutionFolderPath(nested.FolderPath), out var folderElement))
            //    {
            //        foreach (var proj in projects)
            //        {
            //            slnxBase.Root.AddProject(proj, folderElement);
            //        }
            //    }
            //    else
            //    {
            //        throw new InvalidOperationException($"The folder '{nested.FolderPath}' specified as the nesting destination for the project '{nested.ProjectName}' does not exist.");
            //    }
            //}

            baseSln.DistillProjectConfigurations();
        }

        private static string NormalizeSolutionFolderPath(string path)
            => $"/{path.Trim('/', '\\')}/";

        private static void RewritePaths(SolutionModel overlaySln, string overlaySlnPath, string baseSlnPath)
        {
            var overlayDirectoryPath = Path.GetDirectoryName(overlaySlnPath)!;
            foreach (var item in overlaySln.SolutionItems)
            {
                if (item is SolutionProjectModel proj)
                {
                    var pathAbsolute = PathHelper.NormalizePath(Path.Combine(overlayDirectoryPath, proj.FilePath));
                    var pathRelative = PathHelper.MakeRelative(baseSlnPath, pathAbsolute);
                    proj.FilePath = pathRelative;
                }
                else if (item is SolutionFolderModel folder)
                {
                    if (folder.Files is null) continue;

                    foreach (var file in folder.Files.ToArray())
                    {
                        var pathAbsolute = PathHelper.NormalizePath(Path.Combine(overlayDirectoryPath, file));
                        var pathRelative = PathHelper.MakeRelative(baseSlnPath, pathAbsolute);
                        folder.RemoveFile(file);
                        folder.AddFile(pathRelative);
                    }
                }
            }
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
    }
}
