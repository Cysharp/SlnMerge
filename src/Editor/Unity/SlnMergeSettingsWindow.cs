// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
            var isValid = IsValid();
            if (!isValid)
            {
                EditorGUILayout.HelpBox("Some required fields are not filled.", MessageType.Error);
            }

            _context.ProjectConflictResolution = (ProjectConflictResolution)EditorGUILayout.EnumPopup("Conflict Resolution", _context.ProjectConflictResolution);
            _context.DefaultProcessingPolicy = (ProcessingPolicy)EditorGUILayout.EnumPopup("Default Processing Policy", _context.DefaultProcessingPolicy);
            EditorGUILayout.LabelField(" ", _context.DefaultProcessingPolicy.GetDescription());

            if (GUI.changed && isValid)
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
            public ProcessingPolicy DefaultProcessingPolicy;

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
                    DefaultProcessingPolicy = settings.DefaultProcessingPolicy;
                }
            }

            public void Save()
            {
                XDocument xDoc;
                try
                {
                    using var reader = File.OpenRead(Path);
                    xDoc = XDocument.Load(reader);
                }
                catch
                {
                    xDoc = new XDocument(new XElement("SlnMergeSettings"));
                }

                ApplyChangeTo(xDoc, nameof(MergeTargetSolution), MergeTargetSolution);
                ApplyChangeTo(xDoc, nameof(NestedProjects), nameof(NestedProject), NestedProjects,
                    x => new XElement(nameof(NestedProject),
                        new XAttribute(nameof(NestedProject.ProjectName), x.ProjectName),
                        new XAttribute(nameof(NestedProject.FolderPath), x.FolderPath)));
                ApplyChangeTo(xDoc, nameof(ProjectConflictResolution), ProjectConflictResolution);
                ApplyChangeTo(xDoc, nameof(DefaultProcessingPolicy), DefaultProcessingPolicy);

                using var stream = File.Create(Path);
                xDoc.Save(stream);
            }

            private void ApplyChangeTo<T>(XDocument xDoc, string elementName, T newValue)
            {
                var element = xDoc.Root?.Element(elementName);
                var serializedValue = newValue?.ToString() ?? string.Empty;
                if (element == null)
                {
                    xDoc.Root!.Add(new XElement(elementName, serializedValue));
                }
                else
                {
                    if (element.Value != serializedValue)
                    {
                        element.Value = serializedValue;
                    }
                }
            }

            private void ApplyChangeTo<T>(XDocument xDoc, string elementName, string childElementName, IEnumerable<T> newValues, Func<T, XElement> childElementFactory)
            {
                var element = xDoc.Root?.Element(elementName);
                if (element == null)
                {
                    element = new XElement(elementName);
                    xDoc.Root!.Add(element);
                }
                else
                {
                    element.Elements(childElementName).Remove();
                }
                element.Add(newValues.Select(childElementFactory));
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
