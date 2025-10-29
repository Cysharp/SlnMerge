// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SlnMerge.Xml
{
    internal abstract class Node
    {
        public abstract XNode ToXml();

        public static Node CreateFromXNode(XNode xNode)
        {
            if (xNode is XElement xEl)
            {
                return xEl.Name.ToString() switch
                {
                    "Solution" => SolutionElement.Create(xEl),
                    "Project" => new ProjectElement(xEl),
                    "Folder" => new FolderElement(xEl),
                    "File" => new FileElement(xEl),
                    "BuildType" => new BuildTypeElement(xEl),
                    "Configurations" => new ConfigurationsElement(xEl),
                    _ => new UnknownElement(xEl),
                };
            }
            return new UnknownNode(xNode);
        }
    }

    [DebuggerDisplay("Solution: Projects={Projects.Count,nq}; Folders={Folders.Count,nq}")]
    internal class SolutionElement : Node
    {
        public XAttribute[] Attributes { get; set; }
        public Node[] Children { get; set; }

        public Dictionary<string, ProjectElement> Projects { get; set; }
        public Dictionary<string, FolderElement> Folders { get; set; }
        public ConfigurationsElement? Configurations { get; set; }

        public SolutionElement(IEnumerable<XAttribute> attributes, IEnumerable<Node> children)
        {
            Attributes = attributes.ToArray();
            Children = children.ToArray();

            Projects = EnumerateAllProjects(Children).ToDictionary(k => k.Path);
            Folders = Children.OfType<FolderElement>().ToDictionary(k => k.Name);
            Configurations = Children.OfType<ConfigurationsElement>().SingleOrDefault();

            static IEnumerable<ProjectElement> EnumerateAllProjects(IEnumerable<Node> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node is ProjectElement proj) yield return proj;
                    if (node is Element element)
                    {
                        foreach (var proj2 in EnumerateAllProjects(element.Children))
                        {
                            yield return proj2;
                        }
                    }
                }
            }
        }

        public static SolutionElement Create(XElement xElement)
        {
            Debug.Assert(xElement.Name == "Solution");

            var attributes = xElement.Attributes().ToArray();
            var children = xElement.Nodes().Select(Node.CreateFromXNode).ToArray();

            return new SolutionElement(attributes, children);
        }

        public override XNode ToXml()
        {
            return new XElement("Solution",
                Attributes.OfType<XNode>().Concat(Children.Select(x => x.ToXml())));
        }
    }

    internal abstract class Element : Node
    {
        public XAttribute[] Attributes { get; set; } = Array.Empty<XAttribute>();
        public Node[] Children { get; set; } = Array.Empty<Node>();

        protected static Node[] MergeChildren(IEnumerable<Node> baseChildren, IEnumerable<Node> overlayChildren, MergeStrategy strategy)
        {
            var mergedChildren = new List<Node>(baseChildren);
            foreach (var overlayChild in overlayChildren)
            {
                if (overlayChild is IKeyedElement overlayKeyedE)
                {
                    var foundE = mergedChildren.FirstOrDefault(x => x is IKeyedElement baseKeyedE &&
                                                                    (baseKeyedE.ElementName, baseKeyedE.KeyName, baseKeyedE.Key) == (overlayKeyedE.ElementName, overlayKeyedE.KeyName, overlayKeyedE.Key));
                    if (foundE != null)
                    {
                        if (strategy == MergeStrategy.Overlay)
                        {
                            mergedChildren.Remove(foundE);
                            mergedChildren.Add(overlayChild);
                        }
                        else if (strategy == MergeStrategy.Preserve)
                        {
                            continue;
                        }
                        else if (strategy == MergeStrategy.Both)
                        {
                            mergedChildren.Add(overlayChild);
                        }

                        continue;
                    }
                }
                mergedChildren.Add(overlayChild);

            }

            return mergedChildren.ToArray();
        }

        protected static XAttribute[] MergeAttributes(IEnumerable<XAttribute> baseAttributes, IEnumerable<XAttribute> overlayAttributes, MergeStrategy strategy)
        {
            var mergedAttrs = new Dictionary<string, XAttribute>();
            foreach (var attr in baseAttributes)
            {
                mergedAttrs[attr.Name.ToString()] = attr;
            }

            foreach (var attr in overlayAttributes)
            {
                var name = attr.Name.ToString();
                if (mergedAttrs.ContainsKey(name))
                {
                    switch (strategy)
                    {
                        case MergeStrategy.Overlay:
                            mergedAttrs[name] = attr;
                            break;
                        case MergeStrategy.Preserve:
                            // Do nothing, keep base
                            break;
                        case MergeStrategy.Both:
                            // For attributes, "Both" doesn't make much sense; we can choose to keep base
                            break;
                    }
                }
                else
                {
                    mergedAttrs[name] = attr;
                }
            }
            return mergedAttrs.Values.ToArray();
        }
    }

    internal enum MergeStrategy
    {
        Overlay,
        Preserve,
        Both,
    }

    internal interface IKeyedElement
    {
        string ElementName { get; }
        string KeyName { get; }
        string Key { get; }
    }

    internal abstract class KeyedElement : Element, IKeyedElement
    {
        protected abstract string ElementName { get; }
        protected abstract string KeyName { get; }

        protected string Key { get; set; }

        string IKeyedElement.ElementName => ElementName;
        string IKeyedElement.KeyName => KeyName;
        string IKeyedElement.Key => Key;

        public KeyedElement(XElement xElement)
        {
            var key = xElement.Attribute(KeyName)?.Value ?? string.Empty;
            Key = key;

            Attributes = xElement.Attributes().Where(x => x.Name != KeyName).ToArray();
            Children = xElement.Nodes().Select(CreateFromXNode).ToArray();
        }

        public KeyedElement(string name)
        {
            Key = name;
            Attributes = Array.Empty<XAttribute>();
            Children = Array.Empty<Node>();
        }

        public override XNode ToXml()
        {
            var nodes = Attributes.OfType<XObject>()
                .Concat(Children.Select(x => x.ToXml()));

            if (!string.IsNullOrWhiteSpace(Key))
            {
                nodes = nodes.Prepend(new XAttribute(KeyName, Key));
            }

            return new XElement(ElementName, nodes);
        }
    }

    [DebuggerDisplay("Configurations: {Children.Length,nq}")]
    internal class ConfigurationsElement : Element
    {
        public ConfigurationsElement(XElement xElement)
        {
            Attributes = xElement.Attributes().ToArray();
            Children = xElement.Nodes().Select(Node.CreateFromXNode).ToArray();
        }
        public ConfigurationsElement(XAttribute[] attributes, Node[] children)
        {
            Attributes = attributes;
            Children = children;
        }

        public override XNode ToXml()
        {
            return new XElement("Configurations",
                Attributes.OfType<XNode>().Concat(Children.Select(x => x.ToXml())));
        }

        public void Merge(ConfigurationsElement overlay, MergeStrategy strategy = MergeStrategy.Overlay)
        {
            Attributes = MergeAttributes(this.Attributes, overlay.Attributes, strategy);
            Children = MergeChildren(this.Children, overlay.Children, strategy);
        }
    }

    [DebuggerDisplay("File: {Path,nq}")]
    internal class FileElement : KeyedElement
    {
        public string Path
        {
            get => Key;
            set => Key = value;
        }

        protected override string ElementName => "File";
        protected override string KeyName => "Path";

        public FileElement(XElement xElement) : base(xElement) { }
        public FileElement(string path) : base(path) { }
    }

    [DebuggerDisplay("BuildType: {Name,nq}")]
    internal class BuildTypeElement : KeyedElement
    {
        public string Name
        {
            get => Key;
            set => Key = value;
        }

        protected override string ElementName => "BuildType";
        protected override string KeyName => "Name";

        public BuildTypeElement(XElement xElement) : base(xElement) { }
        public BuildTypeElement(string name) : base(name) { }
    }

    [DebuggerDisplay("Folder: {Name,nq}")]
    internal class FolderElement : KeyedElement
    {
        public string Name
        {
            get => Key;
            set => Key = value;
        }

        protected override string ElementName => "Folder";
        protected override string KeyName => "Name";

        public FolderElement(XElement xElement) : base(xElement) { }
        public FolderElement(string name) : base(name) { }

        public void Merge(FolderElement overlay, MergeStrategy strategy = MergeStrategy.Overlay)
        {
            Attributes = MergeAttributes(Attributes, overlay.Attributes, strategy);
            Children = MergeChildren(Children, overlay.Children, strategy);
        }
    }

    [DebuggerDisplay("Project: {Path,nq}")]
    internal class ProjectElement : KeyedElement
    {
        public string Path
        {
            get => Key;
            set => Key = value;
        }

        protected override string ElementName => "Project";
        protected override string KeyName => "Path";

        public ProjectElement(XElement xElement) : base(xElement) { }
    }

    [DebuggerDisplay("Node: {Node.GetType(),nq}")]
    internal class UnknownNode : Node
    {
        public XNode Node { get; }
        public override XNode ToXml() => Node;

        public UnknownNode(XNode node)
        {
            Node = node;
        }
    }

    [DebuggerDisplay("Element: {Element.GetType(),nq}")]
    internal class UnknownElement : Node
    {
        public XElement Element { get; }

        public UnknownElement(XElement xEl)
        {
            Element = xEl;
        }

        public override XNode ToXml() => Element;
    }
}
