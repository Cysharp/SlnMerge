// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SlnMerge
{
    internal class SolutionMerger
    {
        public static bool TryMerge(string solutionFilePath, string solutionFileContent, string overlaySolutionFilePath, string overlaySolutionFileContent, SlnMergeSettings slnMergeSettings, ISlnMergeLogger logger, out string? resultSolutionContent)
        {
            try
            {
                var baseSlnSerializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath)!;
                var overlaySlnSerializer = SolutionSerializers.GetSerializerByMoniker(overlaySolutionFilePath)!;
                var baseSln = ReadSolutionFromString(solutionFileContent, solutionFilePath);
                var overlaySln = ReadSolutionFromString(overlaySolutionFileContent, overlaySolutionFilePath);
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

        private static SolutionModel ReadSolutionFromString(string solutionContent, string moniker)
        {
            var serializer = SolutionSerializers.GetSerializerByMoniker(moniker)!;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(solutionContent));
            return (serializer.Name == "Slnx")
                ? SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None).GetAwaiter().GetResult()
                : SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static SolutionModel ReadSolutionFromPath(string moniker)
        {
            return SolutionSerializers.GetSerializerByMoniker(moniker)!.OpenAsync(moniker, CancellationToken.None).GetAwaiter().GetResult();
        }

        internal static void MergeTo(SolutionModel baseSln, string baseSlnPath, SolutionModel overlaySln, string overlaySlnPath, SlnMergeSettings settings, ISlnMergeLogger logger)
        {
            ValidateSettings(settings);

            RewritePaths(overlaySln, overlaySlnPath, baseSlnPath);

            // Handle project conflicts
            foreach (var proj in overlaySln.SolutionProjects.ToArray())
            {
                var projInBaseSln = baseSln.SolutionProjects.FirstOrDefault(x => x.ActualDisplayName == proj.ActualDisplayName);
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
                            throw new InvalidOperationException($"The project '{proj.FilePath}' conflicts with an existing project in the base solution.");
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
            var depsByProject = new Dictionary<SolutionProjectModel, List<string>>();
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
                        if (!depsByProject.TryGetValue(project, out var deps))
                        {
                            deps = new List<string>();
                            depsByProject[project] = deps;
                        }
                        //var baseDep = baseSln.SolutionProjects.FirstOrDefault(x => x.FilePath == overlayDep.FilePath);
                        //project.AddDependency(baseDep);
                        deps.Add(overlayDep.FilePath);
                    }
                    foreach (var overlayConfigRule in overlayProject.ProjectConfigurationRules ?? Array.Empty<ConfigurationRule>())
                    {
                        project.AddProjectConfigurationRule(overlayConfigRule);
                    }
                }
                else
                {
                    //throw new NotImplementedException();
                    logger.Warn($"Unknown solution item type: {item.GetType().Name}");
                }
            }
            // Update dependencies after all projects are added
            foreach (var (project, deps) in depsByProject)
            {
                foreach (var dep in deps)
                {
                    var projectInBase = baseSln.SolutionProjects.First(x => x.FilePath == dep);
                    project.AddDependency(projectInBase);
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

            foreach (var overlayProp in overlaySln.Properties)
            {
                var baseProp = baseSln.FindProperties(overlayProp.Id) ?? baseSln.AddProperties(overlayProp.Id, overlayProp.Scope);
                foreach (var (key, value) in overlayProp)
                {
                    if (!baseProp.ContainsKey(key))
                    {
                        baseProp.Add(key, value);
                    }
                }
            }

            // Add or move projects into solution folders
            foreach (var nested in settings.NestedProjects)
            {
                var nestedProjectNamePattern = new Regex($"^{Regex.Escape(nested.ProjectName).Replace(@"\*", ".*").Replace(@"\?", ".")}$");
                var matchedProjects = baseSln.SolutionProjects.Where(x => nestedProjectNamePattern.IsMatch(x.ActualDisplayName));
                var normalizedFolderPath = NormalizeSolutionFolderPath(nested.FolderPath);
                var destFolder = baseSln.FindFolder(normalizedFolderPath);
                if (destFolder is null)
                {
                    logger.Debug($"The destination folder '{nested.FolderPath}' was not found. (NestedProject: {nested.ProjectName})");
                    destFolder = baseSln.AddFolder(normalizedFolderPath);
                }

                foreach (var matchedProject in matchedProjects)
                {
                    matchedProject.MoveToFolder(destFolder);
                }
            }

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
