// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SlnMerge.Xml
{
    internal class SlnxFile
    {
        public string Path { get; set; }
        public SolutionElement Root { get; set; }

        public SlnxFile(string filePath, SolutionElement root)
        {
            Path = filePath;
            Root = root;
        }

        public void AddFolder(FolderElement folder)
        {
            if (!Root.Folders.ContainsKey(folder.Name))
            {
                Root.Folders.Add(folder.Name, folder);
                Root.Children = Root.Children.Append(folder).ToArray();
            }
        }

        public SlnxFile Clone()
        {
            return new SlnxFile(Path, (SolutionElement)Node.CreateFromXNode(ToXml().Root!));
        }

        public XDocument ToXml()
        {
            return new XDocument(Root.ToXml());
        }

        public static SlnxFile ParseFromXml(string filePath, string xml)
        {
            var xDoc = XDocument.Parse(xml);
            return new SlnxFile(filePath, (SolutionElement)Node.CreateFromXNode(xDoc.Element("Solution")!));
        }

        public static SlnxFile Merge(SlnxFile slnxOrig, SlnxFile slnxOverlay)
        {
            slnxOrig = slnxOrig.Clone();

            var strategy = MergeStrategy.Overlay;
            var ignoreProjects = new HashSet<string>();
            foreach (var proj in slnxOverlay.Root.Projects)
            {
                if (slnxOrig.Root.Projects.ContainsKey(proj.Key))
                {
                    if (false)
                    {
                        // All
                        throw new NotImplementedException();
                    }
                    else if (false)
                    {
                        // PreserveUnity
                        ignoreProjects.Add(proj.Key);
                    }
                    else if (false)
                    {
                        // PreserveOverlay
                        throw new NotImplementedException();
                    }
                }
            }

            var newChildren = slnxOrig.Root.Children.ToList();
            foreach (var overlayChild in slnxOverlay.Root.Children)
            {
                if (overlayChild is FolderElement overlayFolder)
                {
                    if (slnxOrig.Root.Folders.TryGetValue(overlayFolder.Name, out var existedFolder))
                    {
                        // Merge
                        existedFolder.Merge(overlayFolder, strategy);
                    }
                    else
                    {
                        // New
                        var newFolder = new FolderElement(overlayFolder.Name);
                        newFolder.Attributes = overlayFolder.Attributes;
                        newFolder.Children = overlayFolder.Children;
                        newChildren.Add(newFolder);
                    }
                }
                else if (overlayChild is ConfigurationsElement overlayConfig)
                {
                    if (slnxOrig.Root.Configurations is { } configOrig)
                    {
                        configOrig.Merge(overlayConfig, strategy);
                    }
                    else
                    {
                        newChildren.Add(overlayConfig);
                    }
                }
                else
                {
                    newChildren.Add(overlayChild);
                }
            }
            slnxOrig.Root.Children = newChildren.ToArray();

            return slnxOrig;
        }
    }
}
