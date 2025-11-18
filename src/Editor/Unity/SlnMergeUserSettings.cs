using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SlnMerge.Unity
{
    public class SlnMergeUserSettings : ScriptableObject
    {
        private const string SettingsPath = "UserSettings/SlnMergeUserSettings.json";

        private static SlnMergeUserSettings? _instance;
        public static SlnMergeUserSettings Instance => (_instance != null) ? _instance : _instance = LoadOrNew();

        #region Settings
        [SerializeField]
        private string _mergeSettingsCustomLocation = string.Empty;
        public string MergeSettingsCustomLocation
        {
            get => _mergeSettingsCustomLocation;
            set => SetValue(ref _mergeSettingsCustomLocation, value);
        }

        [SerializeField] private bool _verboseLogging = false;
        public bool VerboseLogging
        {
            get => _verboseLogging;
            set => SetValue(ref _verboseLogging, value);
        }

        [SerializeField] private ProcessingPolicyOverride _processingPolicyOverride = ProcessingPolicyOverride.Unspecified;
        public ProcessingPolicyOverride ProcessingPolicyOverride
        {
            get => _processingPolicyOverride;
            set => SetValue(ref _processingPolicyOverride, value);
        }
        #endregion

        private static SlnMergeUserSettings LoadOrNew()
        {
            if (File.Exists(SettingsPath))
            {
                var instance = CreateInstance<SlnMergeUserSettings>();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), instance);
                return instance;
            }
            else
            {
                var instance = CreateInstance<SlnMergeUserSettings>();
                return instance;
            }
        }

        public void Save()
        {
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(_instance));
        }

        private void SetValue<T>(ref T field, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                Save();
            }
        }
    }
}
