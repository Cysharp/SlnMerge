// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace SlnMerge.Unity
{
    [EditorWindowTitle(title = "SlnMerge Settings")]
    public class SlnMergeSettingsWindow : EditorWindow
    {
        private EditingContext _context = new();
        private ReorderableList _nestedProjects = default!;
        private Vector2 _scrollPosition;

        public static void Open(string path)
        {
            var window = EditorWindow.GetWindow<SlnMergeSettingsWindow>();
            window.Initialize(path);
            window.Show();
        }

        private void Initialize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                // Custom settings file path is not specified. Use the solution name based settings file.
                if (SlnMergeSettings.TryLoadBySolutionPath($"{Application.productName}.slnx", out var loadedSlnMergeSettingsPath, out _))
                {
                    // Found existing settings file.
                    path = loadedSlnMergeSettingsPath;
                }
                else
                {
                    path = $"{Application.productName}.sln.mergesettings";
                }
            }

            _context.Path = path;
            _context.Load();
        }

        private void OnEnable()
        {
            _nestedProjects = new ReorderableList(_context.NestedProjects, typeof(NestedProject), draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);
            _nestedProjects.drawHeaderCallback = (rect) =>
            {
                var halfWidth = rect.width / 2;
                GUI.Label(new Rect(rect.x, rect.y, halfWidth - 4, EditorGUIUtility.singleLineHeight), "Project Name (Required)");
                GUI.Label(new Rect(rect.x + halfWidth + 4, rect.y, halfWidth - 4, EditorGUIUtility.singleLineHeight), "Folder Path (Required)");
            };

            _nestedProjects.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var item = _context.NestedProjects[index];
                var halfWidth = rect.width / 2;
                var projectNameRect = new Rect(rect.x, rect.y + 2, halfWidth - 4, EditorGUIUtility.singleLineHeight);
                item.ProjectName = EditorGUI.TextField(projectNameRect, item.ProjectName);
                var folderPathRect = new Rect(rect.x + halfWidth + 4, rect.y + 2, halfWidth - 4, EditorGUIUtility.singleLineHeight);
                item.FolderPath = EditorGUI.TextField(folderPathRect, item.FolderPath);
            };
        }

        private void OnGUI()
        {
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition))
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Space(8);
                    OnGUICore();
                }
                GUILayout.Space(8);

                _scrollPosition = scrollView.scrollPosition;
            }
        }

        private void OnGUICore()
        {
            EditorGUILayout.LabelField(_context.Path, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This merge settings file is shared across all users editing the project.", MessageType.Info);
            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                _context.MergeTargetSolution = EditorGUILayout.TextField("Merge target solution", _context.MergeTargetSolution, GUILayout.ExpandWidth(true));
                var size = GUI.skin.button.CalcSize(new GUIContent("..."));
                if (GUILayout.Button("...", GUILayout.Width(size.x)))
                {
                    var selectedFilePath = EditorUtility.OpenFilePanelWithFilters(
                        "Select a solution",
                        Path.GetDirectoryName(Application.dataPath),
                        new[] { ".NET Solution File (*.sln, *.slnx)", "sln,slnx", "All files", "*" }
                    );
                    if (!string.IsNullOrWhiteSpace(selectedFilePath))
                    {
                        _context.MergeTargetSolution = PathHelper.MakeRelative(Path.GetDirectoryName(Application.dataPath)!, selectedFilePath);
                        GUI.changed = true;
                    }
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Nested Projects");
            _nestedProjects.DoLayoutList();
            _context.ProjectConflictResolution = (ProjectConflictResolution)EditorGUILayout.EnumPopup("Conflict Resolution", _context.ProjectConflictResolution);

            if (GUI.changed && IsValid())
            {
                _context.Save();
            }
        }

        private bool IsValid()
        {
            return _context.NestedProjects.All(x => !string.IsNullOrWhiteSpace(x.FolderPath) && !string.IsNullOrWhiteSpace(x.ProjectName));
        }

        [Serializable]
        private class EditingContext
        {
            public string Path = string.Empty;
            public List<NestedProject> NestedProjects = new();
            public string MergeTargetSolution = string.Empty;
            public ProjectConflictResolution ProjectConflictResolution;

            public void Load()
            {
                if (SlnMergeSettings.TryLoadFromFile(Path, out var settings))
                {
                    MergeTargetSolution = settings.MergeTargetSolution ?? string.Empty;
                    NestedProjects.Clear();
                    NestedProjects.AddRange(settings.NestedProjects.Select(x => new NestedProject
                    {
                        ProjectName = x.ProjectName,
                        FolderPath = x.FolderPath
                    }));
                    ProjectConflictResolution = settings.ProjectConflictResolution;
                }
            }

            public void Save()
            {
                if (!SlnMergeSettings.TryLoadFromFile(Path, out var settings))
                {
                    settings = new SlnMergeSettings();
                }

                settings.MergeTargetSolution = MergeTargetSolution;
                settings.NestedProjects = NestedProjects.Select(x => new SlnMergeSettings.NestedProject() { ProjectName = x.ProjectName, FolderPath = x.FolderPath }).ToArray();
                settings.ProjectConflictResolution = ProjectConflictResolution;

                settings.Save(Path);
            }
        }

        [Serializable]
        private class NestedProject
        {
            public string ProjectName = default!;
            public string FolderPath = default!;
        }
    }
}
