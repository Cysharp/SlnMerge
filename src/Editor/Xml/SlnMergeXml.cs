// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.Text;

namespace SlnMerge.Xml
{
    internal static class SlnMergeXml
    {
        public static SlnxFile Merge(SlnxFile slnxOrig, SlnxFile slnxOverlay, SlnMergeSettings settings, ISlnMergeLogger logger)
        {
            slnxOrig = slnxOrig.Clone();
            slnxOverlay = slnxOverlay.Clone();

            slnxOverlay.RewritePaths(slnxOrig);

            foreach (var proj in slnxOverlay.Root.Projects)
            {
                if (slnxOrig.Root.Projects.ContainsKey(proj.Key))
                {
                    switch (settings.ProjectConflictResolution)
                    {
                        case ProjectConflictResolution.PreserveOverlay:
                            slnxOrig.Root.RemoveProject(proj.Value);
                            break;
                        case ProjectConflictResolution.PreserveUnity:
                            slnxOverlay.Root.RemoveProject(proj.Value);
                            break;
                        case ProjectConflictResolution.PreserveAll:
                            break;
                    }
                }
            }

            foreach (var overlayChild in slnxOverlay.Root.Children)
            {
                if (overlayChild is FolderElement overlayFolder)
                {
                    slnxOrig.Root.AddOrMergeFolder(overlayFolder, settings.ProjectConflictResolution);
                }
                else if (overlayChild is ConfigurationsElement overlayConfig)
                {
                    slnxOrig.Root.AddOrMergeConfigurations(overlayConfig, settings.ProjectConflictResolution);
                }
                else
                {
                    slnxOrig.Root.AddChild(overlayChild);
                }
            }

            return slnxOrig;
        }
    }
}
