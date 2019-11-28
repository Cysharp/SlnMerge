// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

// ReSharper disable All

//#define SLNMERGE_DEBUG

#if UNITY_EDITOR
namespace SlnMerge.Unity
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class SolutionFileProcessor : AssetPostprocessor
    {
        private static readonly bool _hasVsForUnity;
        private static FileSystemWatcher _watcher;
        private static string _solutionFilePath;

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

            var projectDir = Directory.GetParent(Application.dataPath).FullName;
            _solutionFilePath = Path.Combine(projectDir, Path.GetFileName(projectDir) + ".sln");
            WatchOverlaySolutionChanging();
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
        
        // UnityEditor Callback
        private static string OnGeneratedSlnSolution(string path, string content)
        {
            return IsUnityVsIntegrationEnabled
                ? content /* Visual Studio with VSTU */
                : Merge(path, content); /* other editors (Rider, VSCode ...) */
        }

        private static string Merge(string path, string content)
        {
            if (SlnMerge.TryMerge(path, content, SlnMergeUnityLogger.Instance, out var result))
            {
                WatchOverlaySolutionChanging();
                return result.Merged.ToFileContent();
            }

            return content;
        }

        private static void InvokeSyncOnce()
        {
            InvokeSyncForUnityEditor();
            EditorApplication.update -= InvokeSyncOnce;
        }

        private static void InvokeSync()
        {
            EditorApplication.update += InvokeSyncOnce;
        }

        private static void InvokeSyncForUnityEditor()
        {
            var typeSyncVS = Type.GetType("UnityEditor.SyncVS, UnityEditor");
            if (typeSyncVS == null)
            {
                SlnMergeUnityLogger.Instance.Debug("SlnMerge: UnityEditor.SyncVS class is not found.");
                return;
            }

            var fieldSynchronizer = typeSyncVS.GetField("Synchronizer", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldSynchronizer == null)
            {
                SlnMergeUnityLogger.Instance.Debug("SlnMerge: UnityEditor.SyncVS.Synchronizer field is not found.");
                return;
            }

            var synchronizer = fieldSynchronizer.GetValue(null);
            if (synchronizer == null)
            {
                SlnMergeUnityLogger.Instance.Debug("SlnMerge: UnityEditor has no Synchronizer");
                return;
            }

            var typeSynchronizer = synchronizer.GetType();
            var methodSync = typeSynchronizer.GetMethod("Sync");
            if (methodSync == null)
            {
                SlnMergeUnityLogger.Instance.Debug("SlnMerge: Synchronizer.Sync method is not found.");
                return;
            }

            SlnMergeUnityLogger.Instance.Debug("SlnMerge: Invoking Synchronizer");
            methodSync.Invoke(synchronizer, Array.Empty<object>());
        }

        private static void WatchOverlaySolutionChanging()
        {
            if (!TryGetOverlaySolutionFilePath(_solutionFilePath, out var overlaySolutionFilePath))
            {
                return;
            }

            SlnMergeUnityLogger.Instance.Debug($"SlnMerge: Watching {overlaySolutionFilePath}");
            _watcher?.Dispose();

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(overlaySolutionFilePath), Path.GetFileName(overlaySolutionFilePath));
            _watcher.Changed += (sender, args) =>
            {
                SlnMergeUnityLogger.Instance.Debug($"SlnMerge: {args.ChangeType}: {args.FullPath} ({args.Name})");
                InvokeSync();
            };
            _watcher.EnableRaisingEvents = true;
        }

        private static SlnMergeSettings LoadSettings(string solutionFilePath)
        {
            var slnFileDirectory = Path.GetDirectoryName(solutionFilePath);
            var slnMergeSettings = SlnMergeSettings.Default;
            var slnMergeSettingsPath = Path.Combine(slnFileDirectory, Path.GetFileName(solutionFilePath) + ".mergesettings");
            if (File.Exists(slnMergeSettingsPath))
            {
                slnMergeSettings = SlnMergeSettings.FromFile(slnMergeSettingsPath);
            }

            return slnMergeSettings;
        }

        private static bool TryGetOverlaySolutionFilePath(string solutionFilePath, out string overlaySolutionFilePath)
        {
            var slnFileDirectory = Path.GetDirectoryName(solutionFilePath);
            var slnMergeSettings = LoadSettings(solutionFilePath);

            if (slnMergeSettings.Disabled)
            {
                overlaySolutionFilePath = null;
                return false;
            }

            // Determine a overlay solution path.
            overlaySolutionFilePath = Path.Combine(slnFileDirectory, Path.GetFileNameWithoutExtension(solutionFilePath) + ".Merge.sln");
            if (!string.IsNullOrEmpty(slnMergeSettings.MergeTargetSolution))
            {
                overlaySolutionFilePath = PathUtility.NormalizePath(Path.Combine(slnFileDirectory, slnMergeSettings.MergeTargetSolution));
            }
            if (!File.Exists(overlaySolutionFilePath))
            {
                return false;
            }

            return true;
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
                UnityEngine.Debug.LogError(message);
                if (ex != null)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            public void Information(string message)
            {
                UnityEngine.Debug.Log(message);
            }

            public void Debug(string message)
            {
#if SLNMERGE_DEBUG
                UnityEngine.Debug.Log(message);
#endif
            }
        }
    }
}
#endif

namespace SlnMerge
{
    using global::SlnMerge.Diagnostics;
    using global::SlnMerge.IO;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;

    public class SlnMergeSettings
    {
        public static SlnMergeSettings Default { get; } = new SlnMergeSettings();

        public bool Disabled { get; set; }
        public NestedProject[] NestedProjects { get; set; }

        public string MergeTargetSolution { get; set; }

        public class NestedProject
        {
            [XmlAttribute]
            public string ProjectName { get; set; }
            [XmlAttribute]
            public string ProjectGuid { get; set; }
            [XmlAttribute]
            public string FolderGuid { get; set; }
            [XmlAttribute]
            public string FolderPath { get; set; }
        }

        public static SlnMergeSettings FromFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return (SlnMergeSettings)new XmlSerializer(typeof(SlnMergeSettings)).Deserialize(stream);
            }
        }
    }

    public static class SlnMerge
    {
        public class MergeResult
        {
            public SolutionFile Merged { get; }
            public SolutionFile Base { get; }
            public SolutionFile Overlay { get; }

            public MergeResult(SolutionFile mergedSolution, SolutionFile baseSolution, SolutionFile overlaySolution)
            {
                Merged = mergedSolution;
                Base = baseSolution;
                Overlay = overlaySolution;
            }
        }

        public static bool TryMerge(string solutionFilePath, ISlnMergeLogger logger, out MergeResult result)
        {
            return TryMerge(solutionFilePath, File.ReadAllText(solutionFilePath), logger, out result);
        }

        public static bool TryMerge(string solutionFilePath, string solutionFileContent, ISlnMergeLogger logger, out MergeResult result)
        {
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
                    result = null;
                    return false;
                }

                // Determine a overlay solution path.
                var overlaySolutionFilePath = Path.Combine(slnFileDirectory, Path.GetFileNameWithoutExtension(solutionFilePath) + ".Merge.sln");
                if (!string.IsNullOrEmpty(slnMergeSettings.MergeTargetSolution))
                {
                    overlaySolutionFilePath = PathUtility.NormalizePath(Path.Combine(slnFileDirectory, slnMergeSettings.MergeTargetSolution));
                }
                if (!File.Exists(overlaySolutionFilePath))
                {
                    logger.Warn($"Cannot load the solution file to merge. skipped: {overlaySolutionFilePath}");
                    result = null;
                    return false;
                }

                // Merge the solutions.
                var baseSolutionFile = SolutionFile.Parse(solutionFilePath, solutionFileContent);
                var overlaySolutionFile = SolutionFile.ParseFromFile(overlaySolutionFilePath);
                var engine = new SlnMergeEngine(slnMergeSettings, logger, SlnMergeFileProvider.Instance);
                var mergedSolutionFile = engine.Merge(baseSolutionFile, overlaySolutionFile);

                // Get file content of the merged solution.
                result = new MergeResult(mergedSolutionFile, baseSolutionFile, overlaySolutionFile);
            }
            catch (Exception e)
            {
                logger.Error("Failed to merge the solutions", e);
                result = null;
                return false;
            }

            return true;
        }
    }

    public class SlnMergeEngine
    {
        internal const string GuidProjectTypeFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        private readonly SlnMergeSettings _settings;
        private readonly ISlnMergeLogger _logger;
        private readonly ISlnMergeFileProvider _fileProvider;

        public SlnMergeEngine(SlnMergeSettings settings, ISlnMergeLogger logger, ISlnMergeFileProvider fileProvider)
        {
            _settings = settings;
            _logger = logger;
            _fileProvider = fileProvider;
        }

        public (string[] Additions, string[] Deletions, string[] Updates) GetDifferences(SolutionFile solutionFile, SolutionFile overlaySolutionFile)
        {
            var additions = solutionFile.Projects.Keys.Except(overlaySolutionFile.Projects.Keys);
            var solutionFileDir = Path.GetDirectoryName(solutionFile.Path);
            var overlaySolutionFileDir = Path.GetDirectoryName(overlaySolutionFile.Path);
            additions = additions.Where(x =>
            {
                var path = PathUtility.MakeAbsolute(solutionFileDir, solutionFile.Projects[x].Path);
                var isUnityProject = _fileProvider.ReadAsString(path).Contains("{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1}", StringComparison.OrdinalIgnoreCase);
                return !isUnityProject;
            });

            var deletions = overlaySolutionFile.Projects.Keys.Except(solutionFile.Projects.Keys);

            var updateCandidates = solutionFile.Projects.Keys.Intersect(overlaySolutionFile.Projects.Keys);
            var updates = updateCandidates.Where(x =>
            {
                var projUpdated = solutionFile.Projects[x];
                var projOriginal = overlaySolutionFile.Projects[x];

                // The paths may be adjusted when merging. We must make absolute path before comparing.
                var updatedPath = PathUtility.MakeAbsolute(solutionFileDir, projUpdated.Path);
                var originalPath = PathUtility.MakeAbsolute(overlaySolutionFileDir, projOriginal.Path);

                return updatedPath != originalPath ||
                    projUpdated.Name != projOriginal.Name ||
                    !CompareSections(projUpdated, projOriginal) ||
                    !projUpdated.Children.SequenceEqual(projOriginal.Children);
            });

            return (additions.ToArray(), deletions.ToArray(), updates.ToArray());
        }

        private bool CompareSections(SolutionProject a, SolutionProject b)
        {
            if (a.Sections.Count != b.Sections.Count) return false;
            return a.Sections.All(x => b.Sections.TryGetValue(x.Key, out var bValue) && CompareSection(x.Value, bValue));
        }

        private bool CompareSection(SolutionProjectSection a, SolutionProjectSection b)
        {
            if (a.Value != b.Value) return false;
            if (a.Category != b.Category) return false;
            if (!a.Children.SequenceEqual(b.Children)) return false;
            if (a.Values.Count != b.Values.Count) return false;
            if (!a.Values.All(x => b.Values.TryGetValue(x.Key, out var bValue) && x.Value == bValue)) return false;

            return true;
        }

        public SolutionFile Merge(SolutionFile solutionFile, SolutionFile overlaySolutionFile)
        {
            _logger.Debug($"Merge solution: Base={solutionFile.Path}; Overlay={overlaySolutionFile.Path}");

            var mergedSolutionFile = solutionFile.Clone();

            MergeProjects(mergedSolutionFile, overlaySolutionFile);

            MergeGlobalSections(mergedSolutionFile, overlaySolutionFile);

            ModifySolutionFolders(mergedSolutionFile);

            return mergedSolutionFile;
        }

        private void MergeProjects(SolutionFile solutionFile, SolutionFile overlaySolutionFile)
        {
            foreach (var project in overlaySolutionFile.Projects)
            {
                if (!solutionFile.Projects.ContainsKey(project.Key))
                {
                    if (!project.Value.IsFolder)
                    {
                        var overlayProjectPathAbsolute = PathUtility.NormalizePath(Path.Combine(Path.GetDirectoryName(overlaySolutionFile.Path), project.Value.Path));
                        project.Value.Path = PathUtility.MakeRelative(solutionFile.Path, overlayProjectPathAbsolute);
                    }
                    solutionFile.Projects.Add(project.Key, project.Value);
                }
                else
                {
                    // A project already exists.
                }
            }
        }

        private void MergeGlobalSections(SolutionFile solutionFile, SolutionFile overlaySolutionFile)
        {
            foreach (var sectionKeyValue in overlaySolutionFile.Global.Sections)
            {
                if (solutionFile.Global.Sections.TryGetValue(sectionKeyValue.Key, out var targetSection))
                {
                    foreach (var keyValue in sectionKeyValue.Value.Values)
                    {
                        targetSection.Values[keyValue.Key] = keyValue.Value;
                    }
                    targetSection.Children.AddRange(sectionKeyValue.Value.Children);
                }
                else
                {
                    solutionFile.Global.Sections.Add(sectionKeyValue.Key, sectionKeyValue.Value);
                }
            }
        }

        private void ModifySolutionFolders(SolutionFile solutionFile)
        {
            if (_settings.NestedProjects == null || _settings.NestedProjects.Length == 0) return;

            // Build a solution folder tree.
            var solutionTree = BuildSolutionFlatTree(solutionFile);

            // Create a NestedProject section in the solution if it does not exist.
            if (!solutionFile.Global.Sections.TryGetValue(("NestedProjects", "preSolution"), out var section))
            {
                section = new SolutionGlobalSection(solutionFile.Global, "NestedProjects", "preSolution");
                solutionFile.Global.Sections.Add((section.Category, section.Value), section);
            }

            // Prepare to add nested projects.
            var nestedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var nestedProject in _settings.NestedProjects)
            {
                var nestedProjectGuid = default(string);
                var nestedProjectFolderGuid = default(string);

                // Find a target project
                if (string.IsNullOrEmpty(nestedProject.ProjectName))
                {
                    // by GUID
                    nestedProjectGuid = nestedProject.ProjectGuid;
                }
                else
                {
                    // by Name
                    var proj = solutionFile.Projects.Values.FirstOrDefault(x => x.Name == nestedProject.ProjectName);
                    if (proj != null)
                    {
                        nestedProjectGuid = proj.Guid;
                    }
                }

                // Find a solution folder
                if (string.IsNullOrEmpty(nestedProject.FolderPath))
                {
                    // by GUID
                    nestedProjectFolderGuid = nestedProject.FolderGuid;
                }
                else
                {
                    // by Path
                    if (solutionTree.TryGetValue(nestedProject.FolderPath, out var folderNode))
                    {
                        if (!folderNode.IsFolder)
                        {
                            throw new Exception($"Path '{nestedProject.FolderPath}' is not a Solution Folder.");
                        }
                        nestedProjectFolderGuid = folderNode.Project.Guid;
                    }
                    else
                    {
                        // The target Solution Folder does not exist. make the Solution Folders.
                        var pathParts = nestedProject.FolderPath.Split('/', '\\');
                        for (var i = 0; i < pathParts.Length; i++)
                        {
                            var path = string.Join("/", pathParts.Take(i + 1));
                            var parentPath = string.Join("/", pathParts.Take(i));

                            if (solutionTree.TryGetValue(path, out var folderNode2))
                            {
                                // A solution tree node already exists.
                                if (!folderNode2.IsFolder)
                                {
                                    throw new Exception($"Path '{path}' is not a Solution Folder.");
                                }
                            }
                            else
                            {
                                // Create a new solution folder.
                                var newFolder = new SolutionProject(solutionFile,
                                    typeGuid: GuidProjectTypeFolder,
                                    guid: Guid.NewGuid().ToString("B").ToUpper(),
                                    name: pathParts[i],
                                    path: pathParts[i]
                                );
                                solutionFile.Projects.Add(newFolder.Guid, newFolder);

                                // If the solution folder has a parent folder, add the created folder as a child immediately.
                                if (!string.IsNullOrEmpty(parentPath))
                                {
                                    section.Values[newFolder.Guid] = solutionTree[parentPath].Project.Guid;
                                }

                                // Rebuild the solution tree.
                                solutionTree = BuildSolutionFlatTree(solutionFile);

                                nestedProjectFolderGuid = newFolder.Guid;
                            }
                        }
                    }
                }

                // Verify GUIDs / Paths
                if (nestedProjectGuid == null)
                {
                    throw new Exception($"Project '{nestedProject.ProjectName}' does not exists in the solution.");
                }
                if (nestedProjectFolderGuid == null)
                {
                    throw new Exception($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                }
                if (!solutionFile.Projects.ContainsKey(nestedProjectGuid))
                {
                    throw new Exception($"Project '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                }
                if (!solutionFile.Projects.ContainsKey(nestedProjectFolderGuid))
                {
                    throw new Exception($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                }

                nestedProjects.Add(nestedProjectGuid, nestedProjectFolderGuid);
            }

            // Add nested projects.
            foreach (var keyValue in nestedProjects)
            {
                section.Values[keyValue.Key] = keyValue.Value;
            }
        }

        private static Dictionary<string, SolutionTreeNode> BuildSolutionFlatTree(SolutionFile solutionFile)
        {
            var projectByPath = new Dictionary<string, SolutionTreeNode>(StringComparer.OrdinalIgnoreCase);
            var projectsByGuid = new Dictionary<string, SolutionTreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var project in solutionFile.Projects)
            {
                projectsByGuid[project.Key] = new SolutionTreeNode(project.Value);
            }

            if (solutionFile.Global.Sections.TryGetValue(("NestedProjects", "preSolution"), out var section))
            {
                foreach (var keyValue in section.Values)
                {
                    var projectGuid = keyValue.Key;
                    var parentProjectGuid = keyValue.Value;

                    if (!projectsByGuid.ContainsKey(projectGuid))
                    {
                        projectsByGuid[projectGuid] = new SolutionTreeNode(solutionFile.Projects[projectGuid]);
                    }

                    if (!projectsByGuid.ContainsKey(parentProjectGuid))
                    {
                        projectsByGuid[parentProjectGuid] = new SolutionTreeNode(solutionFile.Projects[parentProjectGuid]);
                    }

                    projectsByGuid[projectGuid].Parent = projectsByGuid[parentProjectGuid];
                    projectsByGuid[parentProjectGuid].Children.Add(projectsByGuid[projectGuid]);
                }
            }

            foreach (var slnNode in projectsByGuid.Values)
            {
                projectByPath[slnNode.Path] = slnNode;
            }

            return projectByPath;
        }


        [DebuggerDisplay("{nameof(SolutionTreeNode)}: {Path,nq}; IsFolder={IsFolder}; Children={Children.Count}")]
        private class SolutionTreeNode
        {
            public List<SolutionTreeNode> Children { get; } = new List<SolutionTreeNode>();
            public SolutionTreeNode Parent { get; set; }
            public SolutionProject Project { get; }
            public bool IsFolder => Project.IsFolder;

            public string Path => (Parent == null ? "" : Parent.Path + "/") + Project.Name;

            public SolutionTreeNode(SolutionProject project)
            {
                Project = project;
            }
        }
    }

    [DebuggerDisplay("{nameof(SolutionFile),nq}: {Path,nq}")]
    public class SolutionFile : SolutionDocumentNode
    {
        public Dictionary<string, SolutionProject> Projects { get; } = new Dictionary<string, SolutionProject>();
        public SolutionGlobal Global { get; set; }
        public string Path { get; }

        public SolutionFile(string path) : base(null)
        {
            Path = path;
        }

        public SolutionFile Clone()
        {
            return (SolutionFile)Clone(null);
        }

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            var newSolution = new SolutionFile(Path);

            newSolution.Children.AddRange(this.Children.Select(x => x.Clone(newSolution)));
            newSolution.Global = (SolutionGlobal)Global.Clone(newSolution);

            foreach (var keyValue in Projects)
            {
                newSolution.Projects.Add(keyValue.Key, keyValue.Value);
            }

            return newSolution;
        }

        public string ToFileContent()
        {
            var stringWriter = new StringWriter();
            Write(new LineWriter(stringWriter, 0));
            return stringWriter.ToString();
        }

        public void Write(TextWriter writer)
        {
            var lineWriter = new LineWriter(writer, 0);
            Write(lineWriter);
        }

        public static SolutionFile ParseFromFile(string path)
        {
            return SolutionFile.Parse(path, File.ReadAllLines(path));
        }

        public static SolutionFile Parse(string path, string content)
        {
            return SolutionFile.Parse(path, Regex.Split(content, "\r?\n"));
        }

        public static SolutionFile Parse(string path, string[] contentLines)
        {
            var solutionFile = new SolutionFile(path);
            SolutionDocumentNode current = solutionFile;
            foreach (var line in contentLines)
            {
                var parsedLine = SolutionDocLine.ParseLine(line);
                switch (parsedLine.Type)
                {
                    case SlnDocLineType.ProjectBegin:
                        {
                            if (!(current is SolutionFile)) throw new InvalidOperationException("Project must be located under Solution");
                            var sln = current as SolutionFile;
                            var proj = new SolutionProject(current, parsedLine);
                            current = proj;
                            if (sln.Projects.ContainsKey(proj.Guid))
                            {
                                // already exists
                                continue;
                            }
                            sln.Projects.Add(proj.Guid, proj);
                        }
                        break;
                    case SlnDocLineType.ProjectSectionBegin:
                        {
                            if (!(current is SolutionProject)) throw new InvalidOperationException("ProjectSection must be located under Project");
                            var proj = current as SolutionProject;
                            var projSection = new SolutionProjectSection(current, parsedLine);
                            current = projSection;
                            if (proj.Sections.ContainsKey((projSection.Category, projSection.Value)))
                            {
                                // already exists
                                continue;
                            }
                            proj.Sections.Add((projSection.Category, projSection.Value), projSection);
                        }
                        break;
                    case SlnDocLineType.GlobalBegin:
                        {
                            if (!(current is SolutionFile)) throw new InvalidOperationException("Global must be located under Solution");
                            var sln = current as SolutionFile;
                            sln.Global = new SolutionGlobal(current);
                            current = sln.Global;
                        }
                        break;
                    case SlnDocLineType.GlobalSectionBegin:
                        {
                            if (!(current is SolutionGlobal)) throw new InvalidOperationException("GlobalSection must be located under Global");
                            var global = current as SolutionGlobal;
                            var globalSection = new SolutionGlobalSection(current, parsedLine);
                            if (global.Sections.ContainsKey((globalSection.Category, globalSection.Value)))
                            {
                                // already exists
                                continue;
                            }
                            global.Sections.Add((globalSection.Category, globalSection.Value), globalSection);
                            current = globalSection;
                        }
                        break;
                    case SlnDocLineType.GlobalEnd:
                    case SlnDocLineType.ProjectEnd:
                    case SlnDocLineType.ProjectSectionEnd:
                    case SlnDocLineType.GlobalSectionEnd:
                        current = current.Parent;
                        break;
                    default:
                        current.AddChild(parsedLine);
                        break;
                }
            }
            return solutionFile;
        }

        public override void Write(LineWriter writer)
        {
            base.Write(writer);
            foreach (var proj in Projects)
            {
                proj.Value.Write(writer);
            }
            Global.Write(writer);
        }
    }

    public struct LineWriter
    {
        private readonly int _depth;
        private readonly TextWriter _writer;

        public LineWriter(TextWriter writer, int depth)
        {
            _writer = writer;
            _depth = depth;
        }

        public LineWriter Nest() => new LineWriter(_writer, _depth + 1);

        public void WriteLine(string line)
        {
            for (var i = 0; i < _depth; i++) _writer.Write('\t');
            _writer.WriteLine(line);
        }
    }

    [DebuggerDisplay("{nameof(SolutionDocLine),nq}: LineType={Type,nq}; Content={Content,nq}")]
    public class SolutionDocLine
    {
        public string Content { get; }
        public SlnDocLineType Type { get; }

        private SolutionDocLine(SlnDocLineType lineType, string content)
        {
            Type = lineType;
            Content = content;
        }

        public static SolutionDocLine ParseLine(string line)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("Microsoft Visual Studio Solution File, Format Version"))
            {
                return new SolutionDocLine(SlnDocLineType.FormatVersion, trimmedLine);
            }
            else if (trimmedLine.StartsWith("VisualStudioVersion"))
            {
                return new SolutionDocLine(SlnDocLineType.VsVersion, trimmedLine);
            }
            else if (trimmedLine.StartsWith("MinimumVisualStudioVersion"))
            {
                return new SolutionDocLine(SlnDocLineType.MinVsVersion, trimmedLine);
            }
            else if (trimmedLine.StartsWith("Project(\""))
            {
                return new SolutionDocLine(SlnDocLineType.ProjectBegin, trimmedLine);
            }
            else if (trimmedLine == "EndProject")
            {
                return new SolutionDocLine(SlnDocLineType.ProjectEnd, trimmedLine);
            }
            else if (trimmedLine.StartsWith("ProjectSection("))
            {
                return new SolutionDocLine(SlnDocLineType.ProjectSectionBegin, trimmedLine);
            }
            else if (trimmedLine == "EndProjectSection")
            {
                return new SolutionDocLine(SlnDocLineType.ProjectSectionEnd, trimmedLine);
            }
            else if (trimmedLine == "Global")
            {
                return new SolutionDocLine(SlnDocLineType.GlobalBegin, trimmedLine);
            }
            else if (trimmedLine == "EndGlobal")
            {
                return new SolutionDocLine(SlnDocLineType.GlobalEnd, trimmedLine);
            }
            else if (trimmedLine.StartsWith("GlobalSection("))
            {
                return new SolutionDocLine(SlnDocLineType.GlobalSectionBegin, trimmedLine);
            }
            else if (trimmedLine == "EndGlobalSection")
            {
                return new SolutionDocLine(SlnDocLineType.GlobalSectionEnd, trimmedLine);
            }
            else
            {
                return new SolutionDocLine(SlnDocLineType.Unknown, line);
            }
        }
    }

    public enum SlnDocLineType
    {
        FormatVersion,
        VsVersion,
        MinVsVersion,
        ProjectBegin,
        ProjectEnd,
        ProjectSectionBegin,
        ProjectSectionEnd,
        GlobalBegin,
        GlobalEnd,
        GlobalSectionBegin,
        GlobalSectionEnd,
        Unknown,
    }

    public abstract class SolutionDocumentNode : IEquatable<SolutionDocumentNode>
    {
        public SolutionDocumentNode Parent { get; }
        public List<SolutionDocumentNode> Children { get; } = new List<SolutionDocumentNode>();

        protected SolutionDocumentNode(SolutionDocumentNode parent)
        {
            Parent = parent;
        }

        public virtual void Write(LineWriter writer)
        {
            foreach (var child in Children)
            {
                child.Write(writer);
            }
        }

        public virtual void AddChild(SolutionDocLine line)
        {
            Children.Add(new SolutionDocumentTrivialNode(this, line));
        }

        public abstract SolutionDocumentNode Clone(SolutionDocumentNode newParent);

         bool IEquatable<SolutionDocumentNode>.Equals(SolutionDocumentNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Parent, other.Parent) && Children.SequenceEqual(other.Children);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SolutionDocumentNode) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Parent != null ? Parent.GetHashCode() : 0) * 397) ^ (Children != null ? Children.GetHashCode() : 0);
            }
        }
    }

    public abstract class SolutionSectionContainer<TSection> : SolutionDocumentNode
        where TSection : SolutionSection
    {
        protected SolutionSectionContainer(SolutionDocumentNode parent) : base(parent)
        { }

        public Dictionary<(string Category, string Value), TSection> Sections { get; } = new Dictionary<(string Category, string Value), TSection>();

        protected abstract string Tag { get; }
        protected abstract string Category { get; }
        protected abstract string Value { get; }

        public override void Write(LineWriter writer)
        {
            if (string.IsNullOrEmpty(Category) && string.IsNullOrEmpty(Value))
            {
                writer.WriteLine(Tag);
            }
            else if (string.IsNullOrEmpty(Value))
            {
                writer.WriteLine($"{Tag}({Category})");
            }
            else if (string.IsNullOrEmpty(Category))
            {
                writer.WriteLine($"{Tag} = {Value}");
            }
            else
            {
                writer.WriteLine($"{Tag}({Category}) = {Value}");
            }

            base.Write(writer);

            foreach (var section in Sections)
            {
                section.Value.Write(writer.Nest());
            }
            writer.WriteLine("End" + Tag);
        }
    }

    [DebuggerDisplay("{nameof(SolutionSection),nq}: {Category,nq} = {Value,nq}")]
    public abstract class SolutionSection : SolutionDocumentNode
    {
        protected abstract string Tag { get; }

        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
        public string Category { get; set; }
        public string Value { get; set; }

        protected SolutionSection(SolutionDocumentNode parent, SolutionDocLine line) : base(parent)
        {
            var match = Regex.Match(line.Content, Tag + @"\(([^)]+)\)\s+=\s+(.*)");
            Category = match.Groups[1].Value;
            Value = match.Groups[2].Value;
        }

        public SolutionSection(SolutionDocumentNode parent, string category, string value) : base(parent)
        {
            Category = category;
            Value = value;
        }

        public override void AddChild(SolutionDocLine line)
        {
            var parts = line.Content.Trim().Split(new[] { " = " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                Values[parts[0]] = parts[1];
            }
            else
            {
                base.AddChild(line);
            }
        }

        public override void Write(LineWriter writer)
        {
            writer.WriteLine($"{Tag}({Category}) = {Value}");
            {
                base.Write(writer);

                var nestedWriter = writer.Nest();
                foreach (var keyValue in Values)
                {
                    nestedWriter.WriteLine(keyValue.Key + " = " + keyValue.Value);
                }
            }
            writer.WriteLine("End" + Tag);
        }
    }

    [DebuggerDisplay("{nameof(SolutionDocumentTrivialNode),nq}: {Line.Content,nq}")]
    public class SolutionDocumentTrivialNode : SolutionDocumentNode
    {
        public SolutionDocLine Line { get; }

        public SolutionDocumentTrivialNode(SolutionDocumentNode parent, SolutionDocLine line) : base(parent)
        {
            Line = line;
        }

        public override void Write(LineWriter writer)
        {
            writer.WriteLine(Line.Content);
        }

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            return new SolutionDocumentTrivialNode(newParent, Line);
        }
    }

    [DebuggerDisplay("{nameof(SolutionProject),nq}: TypeGuid={TypeGuid,nq}; Name={Name,nq}; Path={Path,nq}; Guid={Guid,nq}")]
    public class SolutionProject : SolutionSectionContainer<SolutionProjectSection>
    {
        public string Guid { get; set; }
        public string TypeGuid { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public bool IsFolder => string.Compare(TypeGuid, SlnMergeEngine.GuidProjectTypeFolder, StringComparison.OrdinalIgnoreCase) == 0;

        protected override string Tag => "Project";
        protected override string Category => $"\"{TypeGuid}\"";
        protected override string Value => $"\"{Name}\", \"{Path}\", \"{Guid}\"";

        public SolutionProject(SolutionDocumentNode parent, SolutionDocLine line) : base(parent)
        {
            var match = Regex.Match(line.Content, Tag + @"\(""?([^"")]+)""?\)\s+=\s+""([^""]+)"",\s*""([^""]+)"",\s*""([^""]+)""");
            TypeGuid = match.Groups[1].Value;
            Name = match.Groups[2].Value;
            Path = match.Groups[3].Value;
            Guid = match.Groups[4].Value;
        }

        public SolutionProject(SolutionDocumentNode parent, string typeGuid, string name, string path, string guid)
            : base(parent)
        {
            TypeGuid = typeGuid;
            Name = name;
            Path = path;
            Guid = guid;
        }

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            var newProj = new SolutionProject(newParent, TypeGuid, Name, Path, Guid);
            newProj.Children.AddRange(Children.Select(x => x.Clone(newProj)));
            return newProj;
        }
    }

    [DebuggerDisplay("{nameof(SolutionProjectSection),nq}: Category={Category,nq}; Value={Value,nq}; Values={Values.Count,nq}")]
    public class SolutionProjectSection : SolutionSection
    {
        public SolutionProjectSection(SolutionDocumentNode parent, SolutionDocLine line) : base(parent, line) { }
        public SolutionProjectSection(SolutionDocumentNode parent, string category, string value) : base(parent, category, value) { }
        protected override string Tag => "ProjectSection";

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            var newNode = new SolutionProjectSection(newParent, Category, Value);
            newNode.Children.AddRange(Children.Select(x => x.Clone(newNode)));
            foreach (var keyValue in Values)
            {
                newNode.Values.Add(keyValue.Key, keyValue.Value);
            }
            return newNode;
        }
    }

    [DebuggerDisplay("{nameof(SolutionGlobal),nq}")]
    public class SolutionGlobal : SolutionSectionContainer<SolutionGlobalSection>
    {
        public SolutionGlobal(SolutionDocumentNode parent) : base(parent) { }

        protected override string Tag => "Global";
        protected override string Category => "";
        protected override string Value => "";

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            var newNode = new SolutionGlobal(newParent);
            newNode.Children.AddRange(Children.Select(x => x.Clone(newNode)));
            foreach (var keyValue in Sections)
            {
                newNode.Sections.Add(keyValue.Key, (SolutionGlobalSection)keyValue.Value.Clone(newNode));
            }
            return newNode;
        }
    }

    [DebuggerDisplay("{nameof(SolutionGlobalSection),nq}: Category={Category,nq}; Value={Value,nq}; Values={Values.Count,nq}")]
    public class SolutionGlobalSection : SolutionSection
    {
        public SolutionGlobalSection(SolutionDocumentNode parent, SolutionDocLine line) : base(parent, line) { }
        public SolutionGlobalSection(SolutionDocumentNode parent, string category, string value) : base(parent, category, value) { }

        protected override string Tag => "GlobalSection";

        public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
        {
            var newNode = new SolutionGlobalSection(newParent, Category, Value);
            newNode.Children.AddRange(Children.Select(x => x.Clone(newNode)));
            foreach (var keyValue in Values)
            {
                newNode.Values.Add(keyValue.Key, keyValue.Value);
            }
            return newNode;
        }
    }
}

namespace SlnMerge.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public interface ISlnMergeFileProvider
    {
        string ReadAsString(string path);
    }

    public class SlnMergeFileProvider : ISlnMergeFileProvider
    {
        public static ISlnMergeFileProvider Instance { get; } = new SlnMergeFileProvider();
        public string ReadAsString(string path) => File.ReadAllText(path);
    }

    public class SlnMergeVirtualFileProvider : ISlnMergeFileProvider
    {
        public Dictionary<string, string> FileContentByPath { get; } = new Dictionary<string, string>();
        public string ReadAsString(string path) => FileContentByPath[path];
    }

    internal class PathUtility
    {
        public static string MakeAbsolute(string baseDirPath, string path)
        {
            return NormalizePath(Path.Combine(baseDirPath, path));
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar));
        }

        public static string MakeRelative(string baseDirectoryPath, string targetPath)
        {
            var basePathParts = baseDirectoryPath.Split('/', '\\');
            var targetPathParts = targetPath.Split('/', '\\');

            var targetPathFixed = targetPath;
            for (var i = 0; i < Math.Min(basePathParts.Length, targetPathParts.Length); i++)
            {
                var basePathPrefix = string.Join("/", basePathParts.Take(i + 1));
                var targetPathPrefix = string.Join("/", targetPathParts.Take(i + 1));

                if (basePathPrefix == targetPathPrefix)
                {
                    var pathPrefix = basePathPrefix;
                    var upperDirCount = (basePathParts.Length - i - 2); // excepts a filename

                    var sb = new StringBuilder();
                    for (var j = 0; j < upperDirCount; j++)
                    {
                        sb.Append("..");
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                    sb.Append(targetPath.Substring(pathPrefix.Length + 1));

                    targetPathFixed = sb.ToString();
                }
                else
                {
                    break;
                }
            }

            return targetPathFixed;
        }
    }

}

namespace SlnMerge.Diagnostics
{
    using System;

    public interface ISlnMergeLogger
    {
        void Warn(string message);
        void Error(string message, Exception ex);
        void Information(string message);
        void Debug(string message);
    }

    public class SlnMergeConsoleLogger : ISlnMergeLogger
    {
        public static ISlnMergeLogger Instance { get; } = new SlnMergeConsoleLogger();

        private SlnMergeConsoleLogger() { }

        public void Warn(string message)
        {
            Console.WriteLine($"[Warn] {message}");
        }

        public void Error(string message, Exception ex)
        {
            Console.Error.WriteLine($"[Error] {message}");
            Console.Error.WriteLine(ex.ToString());
        }

        public void Information(string message)
        {
            Console.WriteLine($"[Info] {message}");
        }

        public void Debug(string message)
        {
            Console.WriteLine($"[Debug] {message}");
        }
    }
}