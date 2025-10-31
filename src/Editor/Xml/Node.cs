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
    internal class SolutionElement : Node, IElement
    {
        private Node[] _children;
        private IReadOnlyDictionary<string, ProjectElement>? _projects;
        private IReadOnlyDictionary<string, FolderElement>? _folders;
        private ConfigurationsElement? _configurations;

        public XAttribute[] Attributes { get; set; }

        public Node[] Children
        {
            get => _children;
            set
            {
                _children = value;
                InvalidateMappings();
            }
        }

        public IReadOnlyDictionary<string, ProjectElement> Projects => _projects ??= EnumerateAllProjects(Children).ToDictionary(k => k.Path);
        public IReadOnlyDictionary<string, FolderElement> Folders => _folders ??= Children.OfType<FolderElement>().ToDictionary(k => k.Name);

        public ConfigurationsElement? Configurations => _configurations ??= _children.OfType<ConfigurationsElement>().SingleOrDefault();

        public SolutionElement(IEnumerable<XAttribute> attributes, IEnumerable<Node> children)
        {
            Attributes = attributes.ToArray();
            _children = children.ToArray();

            InvalidateMappings();
        }

        private void InvalidateMappings()
        {
            _projects = null;
            _folders = null;
            _configurations = null;
        }

        private static IEnumerable<ProjectElement> EnumerateAllProjects(IEnumerable<Node> nodes)
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

        public void AddOrMergeFolder(FolderElement overlayFolder, ProjectConflictResolution strategy)
        {
            if (Folders.TryGetValue(overlayFolder.Name, out var existedFolder))
            {
                // Merge
                existedFolder.Merge(overlayFolder, strategy);
            }
            else
            {
                // New
                AddFolder(new FolderElement(overlayFolder.Name)
                {
                    Attributes = overlayFolder.Attributes,
                    Children = overlayFolder.Children
                });
            }
        }

        public void AddOrMergeConfigurations(ConfigurationsElement overlayConfig, ProjectConflictResolution strategy)
        {
            if (Configurations != null)
            {
                Configurations.Merge(overlayConfig, strategy);
            }
            else
            {
                _children = _children.Append(overlayConfig).ToArray();
                InvalidateMappings();
            }
        }

        public void AddFolder(FolderElement folder)
        {
            if (Folders.ContainsKey(folder.Name)) throw new InvalidOperationException($"The folder '{folder.Name}' already exists.");

            _children = _children.Append(folder).ToArray();
            InvalidateMappings();
        }

        public void RemoveProject(ProjectElement project)
        {
            if (!Projects.ContainsKey(project.Path)) throw new InvalidOperationException($"The project '{project.Path}' does not exist.");
            Remove(project);
        }

        private void Remove<T>(T element) where T : IKeyedElement
        {
            _children = _children.Select(x => RemoveKeyedElement(element, x)).ToArray();
            InvalidateMappings();

            Node RemoveKeyedElement(IKeyedElement e, Node root)
            {
                if (root is IElement rootElement)
                {
                    var children = new List<Node>(rootElement.Children.Length);
                    foreach (var child in rootElement.Children)
                    {
                        if (child is IKeyedElement keyedChild &&
                            keyedChild.KeyName == e.KeyName &&
                            keyedChild.ElementName == e.ElementName &&
                            keyedChild.Key == e.Key
                           )
                        {
                            continue;
                        }
                        else
                        {
                            children.Add(RemoveKeyedElement(e, child));
                        }
                    }
                    rootElement.Children = children.ToArray();
                }
                return root;
            }
        }

        public void AddProject(ProjectElement project)
        {
            if (Projects.ContainsKey(project.Path)) throw new InvalidOperationException($"The project '{project.Path}' already exists.");

            _children = _children.Append(project).ToArray();
            InvalidateMappings();
        }

        public void AddProject(ProjectElement project, FolderElement folder)
        {
            if (Projects.ContainsKey(project.Path)) throw new InvalidOperationException($"The project '{project.Path}' already exists.");
            if (!Folders.ContainsKey(folder.Name)) throw new InvalidOperationException($"The folder '{folder.Name}' does not exist.");

            folder.Children = folder.Children.Append(project).ToArray();
            InvalidateMappings();
        }

        public void AddChild(Node node)
        {
            if (node is ProjectElement proj)
            {
                AddProject(proj);
            }
            else
            {
                _children = _children.Append(node).ToArray();
                InvalidateMappings();
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

    internal abstract class Element : Node, IElement
    {
        public XAttribute[] Attributes { get; set; } = Array.Empty<XAttribute>();
        public Node[] Children { get; set; } = Array.Empty<Node>();

        protected static Node[] MergeChildren(IEnumerable<Node> baseChildren, IEnumerable<Node> overlayChildren, ProjectConflictResolution strategy)
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
                        if (strategy == ProjectConflictResolution.PreserveOverlay)
                        {
                            mergedChildren.Remove(foundE);
                            mergedChildren.Add(overlayChild);
                        }
                        else if (strategy == ProjectConflictResolution.PreserveUnity)
                        {
                            continue;
                        }
                        else if (strategy == ProjectConflictResolution.PreserveAll)
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

        protected static XAttribute[] MergeAttributes(IEnumerable<XAttribute> baseAttributes, IEnumerable<XAttribute> overlayAttributes, ProjectConflictResolution strategy)
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
                        case ProjectConflictResolution.PreserveOverlay:
                            mergedAttrs[name] = attr;
                            break;
                        case ProjectConflictResolution.PreserveUnity:
                            // Do nothing, keep base
                            break;
                        case ProjectConflictResolution.PreserveAll:
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

    internal interface IElement
    {
        XAttribute[] Attributes { get; set; }
        Node[] Children { get; set; }
    }

    internal interface IKeyedElement : IElement
    {
        string ElementName { get; }
        string KeyName { get; }
        string Key { get; }
    }

    internal interface IFilePathElement
    {
        string Path { get; set; }
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

        public void Merge(ConfigurationsElement overlay, ProjectConflictResolution strategy)
        {
            Attributes = MergeAttributes(this.Attributes, overlay.Attributes, strategy);
            Children = MergeChildren(this.Children, overlay.Children, strategy);
        }
    }

    [DebuggerDisplay("File: {Path,nq}")]
    internal class FileElement : KeyedElement, IFilePathElement
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

        public void Merge(FolderElement overlay, ProjectConflictResolution strategy)
        {
            Attributes = MergeAttributes(Attributes, overlay.Attributes, strategy);
            Children = MergeChildren(Children, overlay.Children, strategy);
        }
    }

    [DebuggerDisplay("Project: {Path,nq}")]
    internal class ProjectElement : KeyedElement, IFilePathElement
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
