// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

// ReSharper disable All

//#define SLNMERGE_DEBUG

using System.Threading;

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

        private static string OnGeneratedSlnSolution(string path, string content)
        {
            return IsUnityVsIntegrationEnabled
                ? content /* Visual Studio with VSTU */
                : Merge(path, content); /* other editors (Rider, VSCode ...) */
        }

        private static string Merge(string path, string content)
        {
            if (SlnMerge.TryMerge(path, content, SlnMergeUnityLogger.Instance, out var solutionContent))
            {
                return solutionContent;
            }

            return content;
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
                UnityEngine.Debug.LogError($"[SlnMerge] {message}");
                if (ex != null)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            public void Information(string message)
            {
                UnityEngine.Debug.Log($"[SlnMerge] {message}");
            }

            public void Debug(string message)
            {
#if SLNMERGE_DEBUG
                UnityEngine.Debug.Log($"[SlnMerge] {message}");
#endif
            }
        }
    }
}
#endif

namespace SlnMerge
{
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
        public bool Disabled { get; set; }

        public SolutionFolder[] SolutionFolders { get; set; }
        public NestedProject[] NestedProjects { get; set; }
        public ProjectConflictResolution ProjectConflictResolution { get; set; }
        public ProjectMergeBehavior ProjectMergeBehavior { get; set; }

        public string MergeTargetSolution { get; set; }


        public class SolutionFolder
        {
            [XmlAttribute]
            public string FolderPath { get; set; }

            [XmlAttribute]
            public string Guid { get; set; }
        }

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

    public static class SlnMerge
    {
        internal const string GuidProjectTypeFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        public static bool TryMerge(string solutionFilePath, ISlnMergeLogger logger, out string resultSolutionContent)
        {
            return TryMerge(solutionFilePath, File.ReadAllText(solutionFilePath), logger, out resultSolutionContent);
        }

        public static bool TryMerge(string solutionFilePath, string solutionFileContent, ISlnMergeLogger logger, out string resultSolutionContent)
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
                    resultSolutionContent = solutionFileContent;
                    return true;
                }

                // Determine a overlay solution path.
                var overlaySolutionFilePath = Path.Combine(slnFileDirectory, Path.GetFileNameWithoutExtension(solutionFilePath) + ".Merge.sln");
                if (!string.IsNullOrEmpty(slnMergeSettings.MergeTargetSolution))
                {
                    overlaySolutionFilePath = NormalizePath(Path.Combine(slnFileDirectory, slnMergeSettings.MergeTargetSolution));
                }
                if (!File.Exists(overlaySolutionFilePath))
                {
                    logger.Warn($"Cannot load the solution file to merge. skipped: {overlaySolutionFilePath}");
                    resultSolutionContent = null;
                    return false;
                }

                // Merge the solutions.
                var solutionFile = SolutionFile.Parse(solutionFilePath, solutionFileContent);
                var overlaySolutionFile = SolutionFile.ParseFromFile(overlaySolutionFilePath);
                var mergedSolutionFile = Merge(solutionFile, overlaySolutionFile, slnMergeSettings, logger);

                // Get file content of the merged solution.
                resultSolutionContent = mergedSolutionFile.ToFileContent();
            }
            catch (Exception e)
            {
                logger.Error("Failed to merge the solutions", e);
                resultSolutionContent = null;
                return false;
            }

            return true;
        }

        private class SlnMergeMergeContext
        {
            public SolutionFile SolutionFile { get; }
            public SolutionFile OverlaySolutionFile { get; }
            public SolutionFile MergedSolutionFile { get; }
            public SlnMergeSettings Settings { get; }
            public ISlnMergeLogger Logger { get; }

            public SlnMergeMergeContext(SolutionFile solutionFile, SolutionFile overlaySolutionFile, SolutionFile mergedSolutionFile, SlnMergeSettings settings, ISlnMergeLogger logger)
            {
                SolutionFile = solutionFile;
                OverlaySolutionFile = overlaySolutionFile;
                MergedSolutionFile = mergedSolutionFile;
                Settings = settings;
                Logger = logger;
            }
        }

        public static SolutionFile Merge(SolutionFile solutionFile, SolutionFile overlaySolutionFile, SlnMergeSettings settings, ISlnMergeLogger logger)
        {
            logger.Debug($"Merge solution: Base={solutionFile.Path}; Overlay={overlaySolutionFile.Path}");

            var ctx = new SlnMergeMergeContext(solutionFile, overlaySolutionFile, solutionFile.Clone(), settings, logger);

            MergeProjects(ctx);

            MergeGlobalSections(ctx);

            ModifySolutionFolders(ctx);

            return ctx.MergedSolutionFile;
        }

        private static void MergeProjects(SlnMergeMergeContext ctx)
        {
            foreach (var project in ctx.OverlaySolutionFile.Projects)
            {
                if (!ctx.MergedSolutionFile.Projects.ContainsKey(project.Key))
                {
                    // If a solution contains a project that has same name, resolve conflict by resolution strategy.
                    if (ctx.MergedSolutionFile.Projects.Any(x => x.Value.Name == project.Value.Name))
                    {
                        ctx.Logger.Debug($"Project Conflict: Overlay={project.Value.Name} [{project.Key}; ProjectConflictResolution={ctx.Settings.ProjectConflictResolution}\nMergedSolutionFile.Projects:\n{string.Join("\n", ctx.MergedSolutionFile.Projects.Select(x => $"{x.Value.Name} [{x.Key}]"))}");

                        if (ctx.Settings.ProjectConflictResolution == ProjectConflictResolution.PreserveUnity)
                        {
                            // Ignore Overlay's project
                            continue;
                        }
                        else if (ctx.Settings.ProjectConflictResolution == ProjectConflictResolution.PreserveOverlay)
                        {
                            // Remove Unity generated project
                            var duplicatedProject = ctx.MergedSolutionFile.Projects.First(x => x.Value.Name == project.Value.Name);
                            ctx.MergedSolutionFile.Projects.Remove(duplicatedProject.Key);
                            ctx.Logger.Debug($"Remove duplicated project '{duplicatedProject.Value.Name} [{duplicatedProject.Key}; Path={duplicatedProject.Value.Path}]' (PreserveOverlay)");
                        }
                        else
                        {
                            // Preserve all projects
                        }
                    }

                    if (project.Value.IsFolder)
                    {
                        // If the project is a folder and it contains solution items, we adjust the path of solution items.
                        if (project.Value.Sections.TryGetValue(("SolutionItems", "preProject"), out var solutionItems))
                        {
                            foreach (var solutionItem in solutionItems.Values.ToArray())
                            {
                                solutionItems.Values.Remove(solutionItem.Key);
                                var solutionItemPathAbsolute = NormalizePath(Path.Combine(Path.GetDirectoryName(ctx.OverlaySolutionFile.Path), solutionItem.Key));
                                var solutionItemPathRelative = MakeRelative(ctx.MergedSolutionFile.Path, solutionItemPathAbsolute);
                                solutionItems.Values[solutionItemPathRelative] = solutionItemPathRelative;
                            }
                        }
                    }
                    else
                    {
                        var overlayProjectPathAbsolute = NormalizePath(Path.Combine(Path.GetDirectoryName(ctx.OverlaySolutionFile.Path), project.Value.Path));
                        project.Value.Path = MakeRelative(ctx.MergedSolutionFile.Path, overlayProjectPathAbsolute);
                    }
                    ctx.MergedSolutionFile.Projects.Add(project.Key, project.Value);
                }
                else
                {
                    // A overlay project already exists. Deduplication if needed.
                    var sameNameProjects = ctx.MergedSolutionFile.Projects.Where(x => x.Value.Name == project.Value.Name).ToArray();
                    if (sameNameProjects.Length > 1)
                    {
                        ctx.Logger.Debug($"Duplicated project was detected: Overlay={project.Value.Name} [{project.Key}; ProjectConflictResolution={ctx.Settings.ProjectConflictResolution}\nProjects:\n{string.Join("\n", sameNameProjects.Select(x => $"{x.Value.Name} [{x.Key}]"))}");

                        var removeKey = default(string);
                        if (ctx.Settings.ProjectConflictResolution == ProjectConflictResolution.PreserveUnity)
                        {
                            // Remove a project in the overlay from the solution.
                            removeKey = project.Key;
                        }
                        else if (ctx.Settings.ProjectConflictResolution == ProjectConflictResolution.PreserveOverlay)
                        {
                            // Remove a Unity generated project from the solution.
                            removeKey = sameNameProjects.Single(x => x.Key != project.Key).Key;
                        }

                        if (removeKey != null)
                        {
                            var duplicatedProject = ctx.MergedSolutionFile.Projects[removeKey];
                            ctx.MergedSolutionFile.Projects.Remove(removeKey);
                            ctx.Logger.Debug($"Remove duplicated project '{duplicatedProject.Name} [{duplicatedProject.Guid}; Path={duplicatedProject.Path}]'");
                        }
                    }
                }
            }
        }

        private static void MergeGlobalSections(SlnMergeMergeContext ctx)
        {
            foreach (var sectionKeyValue in ctx.OverlaySolutionFile.Global.Sections)
            {
                if (ctx.MergedSolutionFile.Global.Sections.TryGetValue(sectionKeyValue.Key, out var targetSection))
                {
                    foreach (var keyValue in sectionKeyValue.Value.Values)
                    {
                        targetSection.Values[keyValue.Key] = keyValue.Value;
                    }
                    targetSection.Children.AddRange(sectionKeyValue.Value.Children);
                }
                else
                {
                    ctx.MergedSolutionFile.Global.Sections.Add(sectionKeyValue.Key, sectionKeyValue.Value);
                }
            }
        }

        private static void ModifySolutionFolders(SlnMergeMergeContext ctx)
        {
            if (ctx.Settings.NestedProjects == null || ctx.Settings.NestedProjects.Length == 0) return;

            // Build a solution folder tree.
            var solutionTree = BuildSolutionFlatTree(ctx.MergedSolutionFile);

            // Create a NestedProject section in the solution if it does not exist.
            if (!ctx.MergedSolutionFile.Global.Sections.TryGetValue(("NestedProjects", "preSolution"), out var section))
            {
                section = new SolutionGlobalSection(ctx.MergedSolutionFile.Global, "NestedProjects", "preSolution");
                ctx.MergedSolutionFile.Global.Sections.Add((section.Category, section.Value), section);
            }

            // Prepare to add nested projects.
            var definedSolutionFolders = (ctx.Settings.SolutionFolders ?? Array.Empty<SlnMergeSettings.SolutionFolder>())
                .ToDictionary(k => k.FolderPath.Replace('\\', '/'), v => v.Guid);
            var nestedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var nestedProject in ctx.Settings.NestedProjects)
            {
                IEnumerable<string> nestedProjectGuids;
                var nestedProjectFolderGuid = default(string);

                // Find a target project
                if (string.IsNullOrEmpty(nestedProject.ProjectName))
                {
                    // by GUID
                    nestedProjectGuids = new[] { nestedProject.ProjectGuid };
                }
                else
                {
                    // by Name
                    var nestedProjectNamePattern = new Regex(
                        "^" + Regex.Escape(nestedProject.ProjectName)
                            .Replace(@"\*", ".*").Replace(@"\?", ".") + "$");
                    nestedProjectGuids = ctx.MergedSolutionFile.Projects.Values
                        .Where(x => nestedProjectNamePattern.IsMatch(x.Name))
                        .Select(x => x.Guid);
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
                                if (!definedSolutionFolders.TryGetValue(path, out var guid))
                                {
                                    throw new Exception($"Path '{path}' doesn't exist in the Solution or Solution Folder definitions. To create a new folder, you need to define SolutionFolder in .mergesettings.");
                                }

                                var newFolder = new SolutionProject(ctx.MergedSolutionFile,
                                    typeGuid: GuidProjectTypeFolder,
                                    guid: guid.ToUpper(),
                                    name: pathParts[i],
                                    path: pathParts[i]
                                );
                                ctx.MergedSolutionFile.Projects.Add(newFolder.Guid, newFolder);

                                // If the solution folder has a parent folder, add the created folder as a child immediately.
                                if (!string.IsNullOrEmpty(parentPath))
                                {
                                    section.Values[newFolder.Guid] = solutionTree[parentPath].Project.Guid;
                                }

                                // Rebuild the solution tree.
                                solutionTree = BuildSolutionFlatTree(ctx.MergedSolutionFile);

                                nestedProjectFolderGuid = newFolder.Guid;
                            }
                        }
                    }
                }

                // Verify GUIDs / Paths
                if (!nestedProjectGuids.Any())
                {
                    if (ctx.Settings.ProjectMergeBehavior.HasFlag(ProjectMergeBehavior.ErrorIfProjectOrFolderDoesNotExist))
                    {
                        throw new Exception($"Project '{nestedProject.ProjectName}' does not exists in the solution.");
                    }

                    // The Unity generated solution may not contain `.csproj` for packages.
                    // We should not log this as a warning.
                    ctx.Logger.Debug($"Project '{nestedProject.ProjectName}' does not exists in the solution.");
                    continue;
                }
                if (nestedProjectFolderGuid == null)
                {
                    if (ctx.Settings.ProjectMergeBehavior.HasFlag(ProjectMergeBehavior.ErrorIfProjectOrFolderDoesNotExist))
                    {
                        throw new Exception($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                    }
                    ctx.Logger.Warn($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                    continue;
                }
                if (!ctx.MergedSolutionFile.Projects.ContainsKey(nestedProjectFolderGuid))
                {
                    if (ctx.Settings.ProjectMergeBehavior.HasFlag(ProjectMergeBehavior.ErrorIfProjectOrFolderDoesNotExist))
                    {
                        throw new Exception($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                    }
                    ctx.Logger.Warn($"Solution Folder '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                    continue;
                }
                foreach (var nestedProjectGuid in nestedProjectGuids)
                {
                    if (!ctx.MergedSolutionFile.Projects.ContainsKey(nestedProjectGuid))
                    {
                        if (ctx.Settings.ProjectMergeBehavior.HasFlag(ProjectMergeBehavior.ErrorIfProjectOrFolderDoesNotExist))
                        {
                            throw new Exception($"Project '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                        }
                        ctx.Logger.Warn($"Project '{nestedProject.FolderGuid}' (GUID) does not exists in the solution.");
                        continue;
                    }
                    nestedProjects.Add(nestedProjectGuid, nestedProjectFolderGuid);
                }
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
                        // Project has been removed from the base solution.
                        if (!solutionFile.Projects.ContainsKey(projectGuid))
                        {
                            continue;
                        }

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

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar));
        }

        private static string MakeRelative(string basePath, string targetPath)
        {
            var basePathParts = basePath.Split('/', '\\');
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

            var isProjectMarkerAdded = false;
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

                            // The project is first item of projects.
                            if (sln.Projects.Count == 1)
                            {
                                sln.Children.Add(new ProjectsNodeMarker(sln));
                                isProjectMarkerAdded = true;
                            }
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
                            
                            if (!isProjectMarkerAdded)
                            {
                                // `Project` is always puts before `Global`.
                                sln.Children.Add(new ProjectsNodeMarker(sln));
                                isProjectMarkerAdded = true;
                            }
                            sln.Children.Add(new GlobalNodeMarker(sln));

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

        private class ProjectsNodeMarker : SolutionDocumentNode
        {
            private readonly SolutionFile _solution;

            public ProjectsNodeMarker(SolutionFile parentSolution) : base(parentSolution)
            {
                _solution = parentSolution;
            }

            public override void Write(LineWriter writer)
            {
                foreach (var proj in _solution.Projects)
                {
                    proj.Value.Write(writer);
                }
            }

            public override void AddChild(SolutionDocLine line)
                => throw new NotSupportedException();

            public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
            {
                return new ProjectsNodeMarker((SolutionFile)newParent);
            }
        }

        private class GlobalNodeMarker : SolutionDocumentNode
        {
            private readonly SolutionFile _solution;

            public GlobalNodeMarker(SolutionFile parentSolution) : base(parentSolution)
            {
                _solution = parentSolution;
            }

            public override void Write(LineWriter writer)
            {
                _solution.Global.Write(writer);
            }

            public override void AddChild(SolutionDocLine line)
                => throw new NotSupportedException();

            public override SolutionDocumentNode Clone(SolutionDocumentNode newParent)
            {
                return new GlobalNodeMarker((SolutionFile)newParent);
            }
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

    public abstract class SolutionDocumentNode
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

        public bool IsFolder => string.Compare(TypeGuid, SlnMerge.GuidProjectTypeFolder, StringComparison.OrdinalIgnoreCase) == 0;

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