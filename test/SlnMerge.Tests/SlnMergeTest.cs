using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SlnMerge.Tests
{
    public class SlnMergeTest
    {
        [Fact]
        public void Merge_AdjustProjectRelativePath()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();

            Assert.Equal(@"..\Nantoka.Server\Nantoka.Server.csproj".Replace('\\', Path.DirectorySeparatorChar), mergedSolutionFile.Projects.First().Value.Path);
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void Merge_AdjustProjectRelativePath_2()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();

            Assert.Equal(@"..\Nantoka.Server\Nantoka.Server.csproj".Replace('\\', Path.DirectorySeparatorChar), mergedSolutionFile.Projects.First().Value.Path);
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }


        [SkippableFact]
        public void Merge_AdjustProjectRelativePath_3()
        {
            Skip.If(Environment.OSVersion.Platform != PlatformID.Win32NT);

            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"D:\Path\To\Nantoka\Nantoka.Server.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            Assert.Equal(@"D:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.csproj", mergedSolutionFile.Projects.First().Value.Path);
        }

        [Fact]
        public void Merge_TrivialLines()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }


        [Fact]
        public void Merge_TrivialLines_Tail()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim() + "\r\n");
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Global
EndGlobal
".Trim().ReplacePathSeparators() + "\r\n", content.Trim() + "\r\n");
        }


        [Fact]
        public void Merge_SolutionFolder_NewFolder()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    SolutionFolders = new []
                    {
                        new SlnMergeSettings.SolutionFolder() { FolderPath = "New Folder1", Guid = "{fb9c1fbb-0842-45bc-897f-6909c9813f1f}" },
                    },
                    NestedProjects = new []
                    {
                        new SlnMergeSettings.NestedProject() { FolderPath = "New Folder1", ProjectName = "Assembly-CSharp" },
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            var folderGuid = mergedSolutionFile.Projects.Values.First(x => x.IsFolder).Guid;
            Assert.Equal((@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""New Folder1"", ""New Folder1"", """ + folderGuid + @"""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = " + folderGuid + @"
	EndGlobalSection
EndGlobal
").Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void Merge_SolutionFolder_NewFolder_ProjectDoesNotExist_Warning()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    SolutionFolders = new []
                    {
                        new SlnMergeSettings.SolutionFolder() { FolderPath = "New Folder1", Guid = "{fb9c1fbb-0842-45bc-897f-6909c9813f1f}" },
                    },
                    NestedProjects = new []
                    {
                        new SlnMergeSettings.NestedProject() { FolderPath = "New Folder1", ProjectName = "ProjectDoesNotExist" },
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            var folderGuid = mergedSolutionFile.Projects.Values.First(x => x.IsFolder).Guid;
            Assert.Equal((@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""New Folder1"", ""New Folder1"", """ + folderGuid + @"""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
	EndGlobalSection
EndGlobal
").Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void Merge_SolutionFolder_NewFolder_ProjectDoesNotExist_AsException()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());
            Assert.Throws<Exception>(() =>
            {
                var mergedSolutionFile = SlnMerge.Merge(
                    baseSln,
                    overlaySln,
                    new SlnMergeSettings()
                    {
                        SolutionFolders = new []
                        {
                            new SlnMergeSettings.SolutionFolder() { FolderPath = "New Folder1", Guid = "{fb9c1fbb-0842-45bc-897f-6909c9813f1f}" },
                        },
                        NestedProjects = new []
                        {
                            new SlnMergeSettings.NestedProject() { FolderPath = "New Folder1", ProjectName = "ProjectDoesNotExist" },
                        },
                        ProjectMergeBehavior = ProjectMergeBehavior.ErrorIfProjectOrFolderDoesNotExist,
                    },
                    SlnMergeNullLogger.Instance);
                var content = mergedSolutionFile.ToFileContent();
                var folderGuid = mergedSolutionFile.Projects.Values.First(x => x.IsFolder).Guid;
                Assert.Equal((@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""New Folder1"", ""New Folder1"", """ + folderGuid + @"""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
	EndGlobalSection
EndGlobal
").Trim().ReplacePathSeparators(), content.Trim());
            });
        }

        [Fact]
        public void Merge_SolutionFolder_NewFolder_NotExistsInDefinition_ThrowException()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
EndGlobal
".Trim());

            Assert.Throws<Exception>(() =>
            {
                var mergedSolutionFile = SlnMerge.Merge(
                    baseSln,
                    overlaySln,
                    new SlnMergeSettings()
                    {
                        NestedProjects = new[]
                        {
                            new SlnMergeSettings.NestedProject() {FolderPath = "New Folder1", ProjectName = "Assembly-CSharp"},
                        }
                    },
                    SlnMergeNullLogger.Instance);
            });
        }


        [Fact]
        public void Merge_SolutionFolder_ExistedFolder()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    NestedProjects = new[]
                    {
                        new SlnMergeSettings.NestedProject() { FolderPath = "Folder1", ProjectName = "Assembly-CSharp" },
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }


        [Fact]
        public void Merge_SolutionFolder_ExistedFolder_SectionExists()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    NestedProjects = new[]
                    {
                        new SlnMergeSettings.NestedProject() { FolderPath = "Folder1", ProjectName = "Assembly-CSharp" },
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void Merge_SolutionFolder_ProjectHasBeenRemovedFromBaseSolution()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
		{1E7138DC-D3E2-51A8-4059-67524470B2E8} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
EndGlobal
".Trim());

            // Merge without errors. NestedProject of removed projects remain in a merged solution.
            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    NestedProjects = new[]
                    {
                        new SlnMergeSettings.NestedProject() { FolderPath = "Folder1", ProjectName = "Assembly-CSharp" },
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Folder1"", ""Folder1"", ""{F95BC0CF-E609-419F-B0A0-019BD5783670}""
EndProject
Global
	GlobalSection(NestedProjects) = preSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
		{1E7138DC-D3E2-51A8-4059-67524470B2E8} = {F95BC0CF-E609-419F-B0A0-019BD5783670}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void Merge_SolutionItems()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Global
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings(),
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal((@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
EndGlobal
").Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void TestMerge()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(), SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void TestMerge_SameName_PreserveAll()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings() { ProjectConflictResolution = ProjectConflictResolution.PreserveAll }, SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""..\Nantoka.Server\Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void TestMerge_SameName_PreserveUnity()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings() { ProjectConflictResolution = ProjectConflictResolution.PreserveUnity }, SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }


        [Fact]
        public void TestMerge_SameName_PreserveOverlay()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(){ ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay }, SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""..\Nantoka.Server\Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim());
        }

        [Fact]
        public void TestMerge_SameName_Deduplication_PreserveOverlay()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""..\Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(){ ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay }, SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""..\Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim().ReplacePathSeparators());
        }


        [Fact]
        public void TestMerge_SameName_Deduplication_PreserveUnity()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""..\Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Shared"", ""Nantoka.Shared\Nantoka.Shared.csproj"", ""{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(baseSln, overlaySln, new SlnMergeSettings(){ ProjectConflictResolution = ProjectConflictResolution.PreserveUnity }, SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            Assert.Equal(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Nantoka.Shared"", ""Nantoka.Shared.csproj"", ""{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{CC600BF6-290F-4CF1-A92D-33B3A2B2BB6E}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EC057CB9-6687-4425-82D8-A31A4EE9E4A2}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim().ReplacePathSeparators(), content.Trim().ReplacePathSeparators());
        }

        [Fact]
        public void TestMerge_Wildcard()
        {
            var baseSln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Unity\Nantoka.Unity.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
EndGlobal
".Trim());
            var overlaySln = SolutionFile.Parse(@"C:\Path\To\Nantoka\Nantoka.Server\Nantoka.Server.sln".ToCurrentPlatformPathForm(), @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29509.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		.gitignore = .gitignore
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
EndGlobal
".Trim());

            var mergedSolutionFile = SlnMerge.Merge(
                baseSln,
                overlaySln,
                new SlnMergeSettings()
                {
                    SolutionFolders = new[]
                    {
                        new SlnMergeSettings.SolutionFolder() {FolderPath = "New Folder1", Guid = "{fb9c1fbb-0842-45bc-897f-6909c9813f1f}"},
                    },
                    NestedProjects = new[]
                    {
                        new SlnMergeSettings.NestedProject() {FolderPath = "New Folder1", ProjectName = "Assembly-CSharp*"},
                    }
                },
                SlnMergeNullLogger.Instance);
            var content = mergedSolutionFile.ToFileContent();
            var folderGuid = mergedSolutionFile.Projects.Values.First(x => x.Name == "New Folder1").Guid;
            Assert.Equal((@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp"", ""Assembly-CSharp.csproj"", ""{1E7138DC-D3E2-51A8-4059-67524470B2E7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Assembly-CSharp-Editor"", ""Assembly-CSharp-Editor.csproj"", ""{A94A546A-4413-A73D-F517-3C1A7CCFE662}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Nantoka.Server"", ""..\Nantoka.Server\Nantoka.Server\Nantoka.Server.csproj"", ""{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Project Settings"", ""Project Settings"", ""{34006C71-946B-49BF-BBCB-BB091E5A3AE7}""
	ProjectSection(SolutionItems) = preProject
		..\Nantoka.Server\.gitignore = ..\Nantoka.Server\.gitignore
	EndProjectSection
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""New Folder1"", ""New Folder1"", """ + folderGuid + @"""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(MonoDevelopProperties) = preSolution
		StartupItem = Assembly-CSharp.csproj
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{1E7138DC-D3E2-51A8-4059-67524470B2E7} = " + folderGuid + @"
		{A94A546A-4413-A73D-F517-3C1A7CCFE662} = " + folderGuid + @"
	EndGlobalSection
EndGlobal
").Trim().ReplacePathSeparators(), content.Trim());
        }

    }

    static class PlatformExtensions
    {
        public static string ReplacePathSeparators(this string value)
        {
            if (Path.DirectorySeparatorChar == '\\') return value; // on Windows: no-op

            return value.Replace('\\', '/');
        }

        public static string ToCurrentPlatformPathForm(this string path)
        {
            if (Path.DirectorySeparatorChar == '\\') return path; // on Windows: no-op

            return Regex.Replace(path, "^([a-zA-Z]):", "/mnt/$1").Replace('\\', '/');
        }
    }

    class SlnMergeNullLogger : ISlnMergeLogger
    {
        public static ISlnMergeLogger Instance { get; } = new SlnMergeNullLogger();

        public void Warn(string message)
        {
        }

        public void Error(string message, Exception ex)
        {
        }

        public void Information(string message)
        {
        }

        public void Debug(string message)
        {
        }
    }
}
