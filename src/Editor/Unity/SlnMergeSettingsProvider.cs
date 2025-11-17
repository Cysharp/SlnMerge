// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SlnMerge.Unity
{
    public class SlnMergeSettingsProvider : SettingsProvider
    {
        private string[] _mergeSettingsSelectionItems = Array.Empty<string>();

        public SlnMergeSettingsProvider(string path, SettingsScope scopes, IEnumerable<string>? keywords = null) : base(path, scopes, keywords)
        {
            UpdateMergeSettingsFilesSelectionItems();
        }

        [SettingsProvider]
        public static SettingsProvider Create()
            => new SlnMergeSettingsProvider("Preferences/SlnMerge", SettingsScope.User, new[] { "SlnMerge", "Solution", "Merge" });

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UpdateMergeSettingsFilesSelectionItems();
        }

        public override void OnGUI(string searchContext)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Space(8);
                    OnGUICore(searchContext);
                }
            }
        }

        private void OnGUICore(string searchContext)
        {
            EditorGUILayout.HelpBox("These settings are saved per user and per project.", MessageType.Info);
            var selectedIndex = _mergeSettingsSelectionItems.Select((x, i) => (Item: x, Index: i)).FirstOrDefault(x => x.Item == SlnMergeUserSettings.Instance.MergeSettingsCustomLocation).Index;
            selectedIndex = EditorGUILayout.Popup(new GUIContent("SlnMerge settings file"), selectedIndex, _mergeSettingsSelectionItems);
            if (selectedIndex == 0)
            {
                SlnMergeUserSettings.Instance.MergeSettingsCustomLocation = string.Empty;
            }
            else if (selectedIndex == _mergeSettingsSelectionItems.Length - 1)
            {
                var selectedFilePath = EditorUtility.OpenFilePanelWithFilters(
                    "Select a SlnMerge settings file",
                    Path.GetDirectoryName(Application.dataPath),
                    new[] { "SlnMerge Settings File (*.mergesettings)", "mergesettings", "All files", "*" }
                );
                if (!string.IsNullOrWhiteSpace(selectedFilePath))
                {
                    SlnMergeUserSettings.Instance.MergeSettingsCustomLocation = PathHelper.MakeRelative(Path.GetDirectoryName(Application.dataPath)!, selectedFilePath);
                    SlnMergeUserSettings.Instance.Save();
                    UpdateMergeSettingsFilesSelectionItems();
                    GUI.changed = true;
                }
            }
            else
            {
                SlnMergeUserSettings.Instance.MergeSettingsCustomLocation = _mergeSettingsSelectionItems[selectedIndex];
            }

            SlnMergeUserSettings.Instance.ProcessingPolicyOverride = (ProcessingPolicyOverride)EditorGUILayout.EnumPopup("Processing Policy Override", SlnMergeUserSettings.Instance.ProcessingPolicyOverride);
            EditorGUILayout.LabelField(" ", SlnMergeUserSettings.Instance.ProcessingPolicyOverride.GetDescription());
            GUILayout.Space(8);

            SlnMergeUserSettings.Instance.VerboseLogging = EditorGUILayout.Toggle("Verbose Logging", SlnMergeUserSettings.Instance.VerboseLogging);
            GUILayout.Space(32);

            if (GUILayout.Button("Edit current merge settings"))
            {
                SlnMergeSettingsWindow.Open(SlnMergeUserSettings.Instance.MergeSettingsCustomLocation);
            }

            GUILayout.Space(8);
            EditorGUILayout.HelpBox("To regenerate the solution, open 'External Tools' and click the 'Regenerate project files' button.", MessageType.Info);
        }

        private void UpdateMergeSettingsFilesSelectionItems()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath)!;
            var knownMergeSettings = Directory.EnumerateFiles(projectPath, "*.mergesettings", SearchOption.TopDirectoryOnly)
                .Select(x => PathHelper.MakeRelative(projectPath, x))
                .ToArray();

            _mergeSettingsSelectionItems = new[]
                {
                    "Same as the solution name (Auto detect)",
                    "  ",
                }
                .Concat(knownMergeSettings.Append(SlnMergeUserSettings.Instance.MergeSettingsCustomLocation).OrderBy(x => x))
                .Concat(new[]
                {
                    "Browse...",
                })
                .Where(x => x == "  " || !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();
        }
    }
}
