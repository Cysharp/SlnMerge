// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System.Text;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Xunit;
using Xunit.Abstractions;

namespace SlnMerge.Tests;

public class SolutionMergerTest(ITestOutputHelper outputHelper)
{
    private readonly SlnMergeTestOutputLogger _logger = new(outputHelper);

    [Fact]
    public void Merge()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                            <Folder Name="/Tools/">
                              <Project Path="MyTool.Client.csproj" />
                            </Folder>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Staging" />
                            </Configurations>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Folder Name="/Server/">
                              <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                <BuildType Solution="Publish|*" Project="Release" />
                              </Project>
                            </Folder>
                            <Folder Name="/Solution Items/">
                              <File Path=".github/workflows/build.yaml" />
                              <File Path="Directory.Build.props" />
                              <File Path="LICENSE.md" />
                              <File Path="README.md" />
                            </Folder>
                            <Folder Name="/Tools/">
                              <Project Path="tools/MyTool/MyTool.csproj" Id="13fddb1c-d4e7-453a-b582-7de8ba522a6e">
                                <BuildType Solution="Publish|*" Project="Release" />
                              </Project>
                            </Folder>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                            <Project Path="test/MyApp.Tests/MyApp.Tests.csproj" />
                          </Solution>
                          """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);

        Assert.Equal("""
                     <Solution>
                       <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                       <Project Path="../test/MyApp.Tests/MyApp.Tests.csproj" />
                       <Project Path="MyApp.Client.csproj" />
                       <Folder Name="/Server/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                           <BuildType Solution="Publish|*" Project="Release" />
                         </Project>
                       </Folder>
                       <Folder Name="/Solution Items/">
                         <File Path="../.github/workflows/build.yaml" />
                         <File Path="../Directory.Build.props" />
                         <File Path="../LICENSE.md" />
                         <File Path="../README.md" />
                       </Folder>
                       <Folder Name="/Tools/">
                         <Project Path="../tools/MyTool/MyTool.csproj">
                           <BuildType Solution="Publish|*" Project="Release" />
                         </Project>
                         <Project Path="MyTool.Client.csproj" />
                       </Folder>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                         <BuildType Name="Staging" />
                       </Configurations>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_2()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                            <Folder Name="/Tools/">
                              <Project Path="MyTool.Client.csproj" />
                            </Folder>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Staging" />
                            </Configurations>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Folder Name="/Solution Items/">
                              <File Path=".github/workflows/build.yaml" />
                              <File Path="Directory.Build.props" />
                              <File Path="LICENSE.md" />
                              <File Path="README.md" />
                            </Folder>
                            <Folder Name="/Tools/">
                              <Project Path="tools/MyTool/MyTool.csproj" Id="13fddb1c-d4e7-453a-b582-7de8ba522a6e">
                                <BuildType Solution="Publish|*" Project="Release" />
                              </Project>
                            </Folder>
                            <Folder Name="/Server/">
                              <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                <BuildType Solution="Publish|*" Project="Release" />
                                <BuildDependency Project="src/MyApp.Shared/MyApp.Shared.csproj" />
                              </Project>
                            </Folder>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                            <Project Path="test/MyApp.Tests/MyApp.Tests.csproj" />
                          </Solution>
                          """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
    }

    [Fact]
    public void Merge_BaseSln_OverlaySlnx()
    {
        // Arrange
        var slnBaseSlnV12 = """
                          Microsoft Visual Studio Solution File, Format Version 12.00
                          # Visual Studio 16
                          Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp", "Assembly-CSharp.csproj", "{1E7138DC-D3E2-51A8-4059-67524470B2E7}"
                          EndProject
                          Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{A94A546A-4413-A73D-F517-3C1A7CCFE662}"
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
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Project Path="src/MyApp.Server/MyApp.Server.csproj" />
                          </Solution>
                          """;

        var slnBasePath = @"C:\repos\src\Client\Base.sln".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnBase = CreateSolutionModelFromSln(slnBaseSlnV12);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnBase, slnBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnBase);

        Assert.Equal("""
                     Microsoft Visual Studio Solution File, Format Version 12.00
                     # Visual Studio 16
                     Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp", "Assembly-CSharp.csproj", "{1E7138DC-D3E2-51A8-4059-67524470B2E7}"
                     EndProject
                     Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{A94A546A-4413-A73D-F517-3C1A7CCFE662}"
                     EndProject
                     Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp.Server", "..\src\MyApp.Server\MyApp.Server.csproj", "{3A3E0043-9A70-D936-82A5-445F0CA81399}"
                     EndProject
                     Global
                     	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                     		Debug|Any CPU = Debug|Any CPU
                     		Release|Any CPU = Release|Any CPU
                     		Publish|Any CPU = Publish|Any CPU
                     	EndGlobalSection
                     	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.ActiveCfg = Release|Any CPU
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Release|Any CPU.Build.0 = Release|Any CPU
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Publish|Any CPU.ActiveCfg = Publish|Any CPU
                     		{1E7138DC-D3E2-51A8-4059-67524470B2E7}.Publish|Any CPU.Build.0 = Publish|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.ActiveCfg = Release|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Release|Any CPU.Build.0 = Release|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Publish|Any CPU.ActiveCfg = Publish|Any CPU
                     		{A94A546A-4413-A73D-F517-3C1A7CCFE662}.Publish|Any CPU.Build.0 = Publish|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Release|Any CPU.ActiveCfg = Release|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Release|Any CPU.Build.0 = Release|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Publish|Any CPU.ActiveCfg = Publish|Any CPU
                     		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Publish|Any CPU.Build.0 = Publish|Any CPU
                     	EndGlobalSection
                     	GlobalSection(SolutionProperties) = preSolution
                     		HideSolutionNode = FALSE
                     	EndGlobalSection
                     	GlobalSection(MonoDevelopProperties) = preSolution
                     		StartupItem = Assembly-CSharp.csproj
                     	EndGlobalSection
                     EndGlobal
                     """, mergedSln);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Project Path="../src/MyApp.Server/MyApp.Server.csproj" />
                       <Project Path="Assembly-CSharp-Editor.csproj" />
                       <Project Path="Assembly-CSharp.csproj" />
                       <Properties Name="MonoDevelopProperties">
                         <Property Name="StartupItem" Value="Assembly-CSharp.csproj" />
                       </Properties>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_BaseSlnx_OverlaySln()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="Assembly-CSharp.csproj" />
                            <Project Path="SlnMerge.Editor.csproj" />
                          </Solution>
                          """;
        var slnOverlaySlnV12 = """
                          Microsoft Visual Studio Solution File, Format Version 12.00
                          # Visual Studio Version 16
                          VisualStudioVersion = 16.0.29509.3
                          MinimumVisualStudioVersion = 10.0.40219.1
                          Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Nantoka.Server", "Nantoka.Server\Nantoka.Server.csproj", "{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}"
                          EndProject
                          Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Project Settings", "Project Settings", "{34006C71-946B-49BF-BBCB-BB091E5A3AE7}"
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
                          """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnOverlayPath = @"C:\repos\src\Overlay.sln".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnOverlay = CreateSolutionModelFromSln(slnOverlaySlnV12);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnOverlay, slnOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);

        Assert.Equal("""
                     <Solution>
                       <Folder Name="/Project Settings/" Id="34006c71-946b-49bf-bbcb-bb091e5a3ae7">
                         <File Path="../../Nantoka.Server/.gitignore" />
                       </Folder>
                       <Project Path="../Nantoka.Server/Nantoka.Server.csproj" />
                       <Project Path="Assembly-CSharp.csproj" />
                       <Project Path="SlnMerge.Editor.csproj" />
                       <Properties Name="Visual Studio">
                         <Property Name="OpenWith" Value="Visual Studio Version 16" />
                         <Property Name="SolutionId" Value="30d3efc0-a5f4-4446-b14e-1c2c1740aa87" />
                       </Properties>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_BaseSln_OverlaySln()
    {
        // Arrange
        var slnBaseSlnV12 = """
                          Microsoft Visual Studio Solution File, Format Version 12.00
                          # Visual Studio 16
                          Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp", "Assembly-CSharp.csproj", "{1E7138DC-D3E2-51A8-4059-67524470B2E7}"
                          EndProject
                          Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{A94A546A-4413-A73D-F517-3C1A7CCFE662}"
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
                          """;
        var slnxOverlaySlnV12 = """
                          Microsoft Visual Studio Solution File, Format Version 12.00
                          # Visual Studio Version 16
                          VisualStudioVersion = 16.0.29509.3
                          MinimumVisualStudioVersion = 10.0.40219.1
                          Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Nantoka.Server", "Nantoka.Server\Nantoka.Server.csproj", "{053476FC-B8B2-4A14-AED2-3733DFD5DFC3}"
                          EndProject
                          Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Project Settings", "Project Settings", "{34006C71-946B-49BF-BBCB-BB091E5A3AE7}"
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
                          """;

        var slnBasePath = @"C:\repos\src\Client\Base.sln".ToCurrentPlatformPathForm();
        var slnOverlayPath = @"C:\repos\src\Overlay.sln".ToCurrentPlatformPathForm();
        var slnBase = CreateSolutionModelFromSln(slnBaseSlnV12);
        var slnOverlay = CreateSolutionModelFromSln(slnxOverlaySlnV12);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnBase, slnBasePath, slnOverlay, slnOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnBase);

        Assert.Equal("""
                     Microsoft Visual Studio Solution File, Format Version 12.00
                     # Visual Studio 16
                     VisualStudioVersion = 16.0.29509.3
                     MinimumVisualStudioVersion = 10.0.40219.1
                     Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp", "Assembly-CSharp.csproj", "{1E7138DC-D3E2-51A8-4059-67524470B2E7}"
                     EndProject
                     Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{A94A546A-4413-A73D-F517-3C1A7CCFE662}"
                     EndProject
                     Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Project Settings", "Project Settings", "{34006C71-946B-49BF-BBCB-BB091E5A3AE7}"
                     	ProjectSection(SolutionItems) = preProject
                     		..\..\Nantoka.Server\.gitignore = ..\..\Nantoka.Server\.gitignore
                     	EndProjectSection
                     EndProject
                     Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Nantoka.Server", "..\Nantoka.Server\Nantoka.Server.csproj", "{79AD04D8-C13F-941A-C8A2-9C2763BC5F9B}"
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
                     		{79AD04D8-C13F-941A-C8A2-9C2763BC5F9B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     		{79AD04D8-C13F-941A-C8A2-9C2763BC5F9B}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     		{79AD04D8-C13F-941A-C8A2-9C2763BC5F9B}.Release|Any CPU.ActiveCfg = Release|Any CPU
                     		{79AD04D8-C13F-941A-C8A2-9C2763BC5F9B}.Release|Any CPU.Build.0 = Release|Any CPU
                     	EndGlobalSection
                     	GlobalSection(SolutionProperties) = preSolution
                     		HideSolutionNode = FALSE
                     	EndGlobalSection
                     	GlobalSection(ExtensibilityGlobals) = postSolution
                     		SolutionGuid = {30D3EFC0-A5F4-4446-B14E-1C2C1740AA87}
                     	EndGlobalSection
                     	GlobalSection(MonoDevelopProperties) = preSolution
                     		StartupItem = Assembly-CSharp.csproj
                     	EndGlobalSection
                     EndGlobal
                     """, mergedSln);
    }
    [Fact]
    public void Merge_Project_Dependencies()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                              <BuildType Solution="Publish|*" Project="Release" />
                              <BuildDependency Project="src/MyApp.Shared/MyApp.Shared.csproj" />
                            </Project>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                            <Project Path="test/MyApp.Tests/MyApp.Tests.csproj" />
                          </Solution>
                          """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);


        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                         <BuildDependency Project="../src/MyApp.Shared/MyApp.Shared.csproj" />
                         <BuildType Solution="Publish|*" Project="Release" />
                       </Project>
                       <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                       <Project Path="../test/MyApp.Tests/MyApp.Tests.csproj" />
                       <Project Path="MyApp.Client.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionItems()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Folder Name="/Solution Items/">
                              <File Path="InFolder-Base.md" />
                            </Folder>
                            <Folder Name="/Solution Items/Child1/">
                              <File Path="InFolder-Base-Child1.md" />
                            </Folder>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Folder Name="/Solution Items/">
                              <File Path="InFolder-Overlay.md" />
                            </Folder>
                            <Folder Name="/Solution Items/Child2/">
                              <File Path="InFolder-Overlay-Child2.md" />
                            </Folder>
                          </Solution>
                          """;

        var slnxBasePath = @"C:\repos\src\Base\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Folder Name="/Solution Items/">
                         <File Path="../Overlay/InFolder-Overlay.md" />
                         <File Path="InFolder-Base.md" />
                       </Folder>
                       <Folder Name="/Solution Items/Child1/">
                         <File Path="InFolder-Base-Child1.md" />
                       </Folder>
                       <Folder Name="/Solution Items/Child2/">
                         <File Path="../Overlay/InFolder-Overlay-Child2.md" />
                       </Folder>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_RewritePath()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Folder Name="/Solution Items/">
                              <File Path="docs/Base.md" />
                              <File Path="README.md" />
                            </Folder>
                            <Project Path="src/Base/Base.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Folder Name="/Solution Items/">
                                 <File Path="docs/Overlay.md" />
                                 <File Path="README.md" />
                               </Folder>
                               <Project Path="src/Overlay/Overlay.csproj">
                                 <BuildDependency Project="src/Overlay/Overlay.Dep.csproj" />
                               </Project>
                               <Project Path="src/Overlay/Overlay.Dep.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Base\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Folder Name="/Solution Items/">
                         <File Path="../Overlay/docs/Overlay.md" />
                         <File Path="../Overlay/README.md" />
                         <File Path="docs/Base.md" />
                         <File Path="README.md" />
                       </Folder>
                       <Project Path="../Overlay/src/Overlay/Overlay.csproj">
                         <BuildDependency Project="../Overlay/src/Overlay/Overlay.Dep.csproj" />
                       </Project>
                       <Project Path="../Overlay/src/Overlay/Overlay.Dep.csproj" />
                       <Project Path="src/Base/Base.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }


    [Fact]
    public void Merge_Configurations()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Configurations>
                                 <BuildType Name="Staging" />
                                 <BuildType Name="Publish" />
                                 <BuildType Name="Release" />
                               </Configurations>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                         <BuildType Name="Staging" />
                       </Configurations>
                     </Solution>
                     """, mergedSlnx);
    }


    [Fact]
    public void Merge_Conflict()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                          <Solution>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Release" />
                            </Project>
                          </Solution>
                          """;

        var slnxBasePath = @"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(); // To match the path, set it to the same directory as base
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveAll,
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger));
    }

    [Fact]
    public void Merge_Conflict_PreserveBase()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(); // To match the path, set it to the same directory as base
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Project Path="MyApp.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_Conflict_PreserveBase_Children()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Debug" />
                            </Project>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(); // To match the path, set it to the same directory as base
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Project Path="MyApp.csproj">
                         <BuildType Solution="Publish|*" Project="Debug" />
                       </Project>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_Conflict_PreserveOverlay()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Configurations>
                                 <BuildType Name="Debug" />
                                 <BuildType Name="Publish" />
                                 <BuildType Name="Release" />
                               </Configurations>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(); // To match the path, set it to the same directory as base
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Project Path="MyApp.csproj">
                         <BuildType Solution="Publish|*" Project="Release" />
                       </Project>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_Conflict_PreserveOverlay_Children()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Debug" />
                            </Project>
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(); // To match the path, set it to the same directory as base
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Project Path="MyApp.csproj">
                         <BuildType Solution="Publish|*" Project="Release" />
                       </Project>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_Conflict_SameName_DifferentPath()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveAll,
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger));
    }

    [Fact]
    public void Merge_SolutionFolder_NestedFolder_CreateFolderImplicitly()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="Assembly-CSharp.csproj" />
                            <Project Path="Assembly-CSharp-Editor.csproj" />
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj" />
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [new() { ProjectName = "MyApp.*", FolderPath = "src" }, new() { ProjectName = "Assembly-CSharp*", FolderPath = "src" }],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Folder Name="/src/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj" />
                         <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                         <Project Path="Assembly-CSharp-Editor.csproj" />
                         <Project Path="Assembly-CSharp.csproj" />
                         <Project Path="MyApp.Client.csproj" />
                       </Folder>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_NestedFolder_Wildcard()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="Assembly-CSharp.csproj" />
                            <Project Path="Assembly-CSharp-Editor.csproj" />
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Folder Name="/Solution Items/">
                               </Folder>
                               <Folder Name="/src/">
                               </Folder>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj" />
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "src" }],
            NestedProjects = [new() { ProjectName = "MyApp.*", FolderPath = "src" }, new() { ProjectName = "Assembly-CSharp*", FolderPath = "src" }],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Folder Name="/Solution Items/" />
                       <Folder Name="/src/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj" />
                         <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                         <Project Path="Assembly-CSharp-Editor.csproj" />
                         <Project Path="Assembly-CSharp.csproj" />
                         <Project Path="MyApp.Client.csproj" />
                       </Folder>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_NestedFolder_Wildcard_Once()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="Assembly-CSharp.csproj" />
                            <Project Path="Assembly-CSharp-Editor.csproj" />
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Folder Name="/Solution Items/">
                               </Folder>
                               <Folder Name="/src/">
                               </Folder>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj" />
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "src" }],
            NestedProjects = [
                new() { ProjectName = "MyApp.*", FolderPath = "src" },
                new() { ProjectName = "Assembly-CSharp*", FolderPath = "src" },
                new() { ProjectName = "MyApp.*", FolderPath = "NoMatch" },
            ],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Folder Name="/Solution Items/" />
                       <Folder Name="/src/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj" />
                         <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                         <Project Path="Assembly-CSharp-Editor.csproj" />
                         <Project Path="Assembly-CSharp.csproj" />
                         <Project Path="MyApp.Client.csproj" />
                       </Folder>
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_NewFolder()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Configurations>
                                 <BuildType Name="Debug" />
                                 <BuildType Name="Publish" />
                                 <BuildType Name="Release" />
                               </Configurations>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "New Folder1" }],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Folder Name="/New Folder1/" />
                       <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                         <BuildType Solution="Publish|*" Project="Release" />
                       </Project>
                       <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                       <Project Path="MyApp.Client.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_NewFolder_NestedFolder()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Configurations>
                                 <BuildType Name="Debug" />
                                 <BuildType Name="Publish" />
                                 <BuildType Name="Release" />
                               </Configurations>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "New Folder1" }, new() { FolderPath = "New Folder2" }],
            NestedProjects = [new() { ProjectName = "MyApp.Client", FolderPath = "New Folder1" }, new() { ProjectName = "MyApp.Server", FolderPath = "New Folder2" }],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Folder Name="/New Folder1/">
                         <Project Path="MyApp.Client.csproj" />
                       </Folder>
                       <Folder Name="/New Folder2/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                           <BuildType Solution="Publish|*" Project="Release" />
                         </Project>
                       </Folder>
                       <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_AlreadyExists()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Folder Name="/New Folder1/" />
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        var slnxOverlayXml = """
                             <Solution>
                               <Configurations>
                                 <BuildType Name="Debug" />
                                 <BuildType Name="Publish" />
                                 <BuildType Name="Release" />
                               </Configurations>
                               <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                               <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                             </Solution>
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnxOverlay = CreateSolutionModelFromSlnx(slnxOverlayXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "New Folder1" }],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Folder Name="/New Folder1/" />
                       <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                         <BuildType Solution="Publish|*" Project="Release" />
                       </Project>
                       <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                       <Project Path="MyApp.Client.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    [Fact]
    public void Merge_SolutionFolder_Sln_ItemOrder()
    {
        // Arrange
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.Client.csproj" />
                          </Solution>
                          """;
        // NOTE: In `.sln`, projects may be defined before solution folders.
        var slnOverlaySln = """
                             Microsoft Visual Studio Solution File, Format Version 12.00
                             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp.Server", "src\MyApp.Server\MyApp.Server.csproj", "{3A3E0043-9A70-D936-82A5-445F0CA81399}"
                             EndProject
                             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp.Shared", "src\MyApp.Shared\MyApp.Shared.csproj", "{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}"
                             EndProject
                             Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "New Folder1", "New Folder1", "{EC16570D-B7EA-1C82-7ECB-77418AAD0BB4}"
                             EndProject
                             Global
                             	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                             		Debug|Any CPU = Debug|Any CPU
                             		Release|Any CPU = Release|Any CPU
                             		Publish|Any CPU = Publish|Any CPU
                             	EndGlobalSection
                             	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Debug|Any CPU.Build.0 = Debug|Any CPU
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Release|Any CPU.ActiveCfg = Release|Any CPU
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Release|Any CPU.Build.0 = Release|Any CPU
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Publish|Any CPU.ActiveCfg = Release|Any CPU
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399}.Publish|Any CPU.Build.0 = Release|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Debug|Any CPU.Build.0 = Debug|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Release|Any CPU.ActiveCfg = Release|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Release|Any CPU.Build.0 = Release|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Publish|Any CPU.ActiveCfg = Publish|Any CPU
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B}.Publish|Any CPU.Build.0 = Publish|Any CPU
                             	EndGlobalSection
                             	GlobalSection(SolutionProperties) = preSolution
                             		HideSolutionNode = FALSE
                             	EndGlobalSection
                             	GlobalSection(NestedProjects) = preSolution
                             		{3A3E0043-9A70-D936-82A5-445F0CA81399} = {EC16570D-B7EA-1C82-7ECB-77418AAD0BB4}
                             		{83CC0C9F-E739-A13E-5604-F84B5D1BA90B} = {EC16570D-B7EA-1C82-7ECB-77418AAD0BB4}
                             	EndGlobalSection
                             EndGlobal
                             """;

        var slnxBasePath = @"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm();
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
        var slnxBase = CreateSolutionModelFromSlnx(slnxBaseXml);
        var slnOverlay = CreateSolutionModelFromSln(slnOverlaySln);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };

        // Act
        SolutionMerger.MergeTo(slnxBase, slnxBasePath, slnOverlay, slnxOverlayPath, slnMergeSettings, _logger);

        // Assert
        var mergedSln = SerializeSolutionToSln(slnxBase);
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        Assert.Equal("""
                     <Solution>
                       <Configurations>
                         <BuildType Name="Debug" />
                         <BuildType Name="Publish" />
                         <BuildType Name="Release" />
                       </Configurations>
                       <Folder Name="/New Folder1/">
                         <Project Path="../src/MyApp.Server/MyApp.Server.csproj">
                           <BuildType Solution="Publish|*" Project="Release" />
                         </Project>
                         <Project Path="../src/MyApp.Shared/MyApp.Shared.csproj" />
                       </Folder>
                       <Project Path="MyApp.Client.csproj" />
                     </Solution>
                     """, mergedSlnx);
    }

    private SolutionModel CreateSolutionModelFromSln(string content)
        => SolutionSerializers.SlnFileV12.OpenAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), CancellationToken.None).GetAwaiter().GetResult();

    private SolutionModel CreateSolutionModelFromSlnx(string content)
        => SolutionSerializers.SlnXml.OpenAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), CancellationToken.None).GetAwaiter().GetResult();

    private string SerializeSolutionToSlnx(SolutionModel sln)
    {
        var stream = new MemoryStream();
        SolutionSerializers.SlnXml.SaveAsync(stream, sln, CancellationToken.None).GetAwaiter().GetResult();
        return Encoding.UTF8.GetString(stream.ToArray()).Trim();
    }
    private string SerializeSolutionToSln(SolutionModel sln)
    {
        var stream = new MemoryStream();
        SolutionSerializers.SlnFileV12.SaveAsync(stream, sln, CancellationToken.None).GetAwaiter().GetResult();
        return Encoding.UTF8.GetString(stream.ToArray()).Trim();
    }
}
