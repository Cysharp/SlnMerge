using SlnMerge.IO;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnMerge.Tests
{
    public class SlnMergeTestWriteBack
    {
        [Fact]
        public void GetDifferences_Additions_Deletions()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server2"", ""..\Nantoka.Server\Nantoka.Server2.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC5}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server2.csproj"), "");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Contains(diff.Additions, x => x == "{053476FC-B8B2-4A14-AED2-3733DFD5DFC5}");
            Assert.Contains(diff.Deletions, x => x == "{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}");
            Assert.DoesNotContain(diff.Deletions, x => x == "{1E7138DC-D3E2-51A8-4059-67524470B2E7}"); // Unity projects must be ignored.
            Assert.Empty(diff.Updates);
        }

        [Fact]
        public void GetDifferences_Additions_UnityProject_Ignore()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.Empty(diff.Updates);
        }


        [Fact]
        public void GetDifferences_Updates_NoChange()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server2.csproj"), "");
            //fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.Empty(diff.Updates);
        }


        [Fact]
        public void GetDifferences_Updates_Sections_NoChange()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
	ProjectSection(SlnMergeCustom) = preProject
		Key = Value
	EndProjectSection
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
	ProjectSection(SlnMergeCustom) = preProject
		Key = Value
	EndProjectSection
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server2.csproj"), "");
            //fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.Empty(diff.Updates);
        }

        [Fact]
        public void GetDifferences_Updates_Name()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server2"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server2.csproj"), "");
            //fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.Contains(diff.Updates, x => x == "{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}");
        }

        [Fact]
        public void GetDifferences_Updates_Path()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server2.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server2.csproj"), "");
            //fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.Contains(diff.Updates, x => x == "{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}");
        }

        [Fact]
        public void GetDifferences_Updates_Sections_1()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
	ProjectSection(SolutionItems) = preProject
		.gitignore__MODIFIED__ = .gitignore__MODIFIED__
	EndProjectSection
	ProjectSection(SlnMergeCustom) = preProject
		Key = Value
	EndProjectSection
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC4}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
	ProjectSection(SlnMergeCustom) = preProject
		Key = Value
	EndProjectSection
EndProject
Global
EndGlobal
".Trim());

            var fileProvider = new SlnMergeVirtualFileProvider();
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Assembly-CSharp.csproj"), "<ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
            fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server2.csproj"), "");
            //fileProvider.FileContentByPath.Add(PathUtility.NormalizePath(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj"), "");
            var engine = new SlnMergeEngine(new SlnMergeSettings(), SlnMergeNullLogger.Instance, fileProvider);
            var diff = engine.GetDifferences(baseSln, overlaySln);

            Assert.Empty(diff.Additions);
            Assert.Empty(diff.Deletions);
            Assert.NotEmpty(diff.Updates);
        }
    }

}
