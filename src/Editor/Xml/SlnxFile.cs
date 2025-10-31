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
            if (!System.IO.Path.IsPathFullyQualified(filePath)) throw new InvalidOperationException("The file path must be absolute.");

            Path = filePath;
            Root = root;
        }
        
        public SlnxFile Clone()
        {
            return new SlnxFile(Path, (SolutionElement)Node.CreateFromXNode(ToXml().Root!));
        }

        public XDocument ToXml()
        {
            return new XDocument(Root.ToXml());
        }

        public void RewritePaths(SlnxFile baseSlnx)
        {
            foreach (var e in EnumerateAllFilePathElements(Root))
            {
                var pathAbsolute = PathHelper.NormalizePath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path)!, e.Path));
                var pathRelative = PathHelper.MakeRelative(baseSlnx.Path, pathAbsolute);
                e.Path = pathRelative;
            }

            static IEnumerable<IFilePathElement> EnumerateAllFilePathElements(IElement element)
            {
                foreach (var c in element.Children)
                {
                    if (c is IFilePathElement fpe)
                    {
                        yield return fpe;
                    }
                    if (c is IElement e)
                    {
                        foreach (var c2 in EnumerateAllFilePathElements(e))
                        {
                            yield return c2;
                        }
                    }
                }
            }
        }

        public static SlnxFile ParseFromXml(string filePath, string xml)
        {
            var xDoc = XDocument.Parse(xml);
            return new SlnxFile(filePath, (SolutionElement)Node.CreateFromXNode(xDoc.Element("Solution")!));
        }
    }
}
