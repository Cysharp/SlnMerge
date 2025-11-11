// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System.Text;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using SlnMerge.Persistence;
using Xunit;

namespace SlnMerge.Tests;

public class SlnMergePersistenceTest
{
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);

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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);


        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        var ex = Assert.Throws<InvalidOperationException>(() => SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance));
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        var ex = Assert.Throws<InvalidOperationException>(() => SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance));
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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
        SlnMergePersistence.MergeTo(slnxBase, slnxBasePath, slnxOverlay, slnxOverlayPath, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var mergedSlnx = SerializeSolutionToSlnx(slnxBase);
        var mergedSln = SerializeSolutionToSln(slnxBase);
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

    private SolutionModel CreateSolutionModelFromSln(string content)
        => SolutionSerializers.SlnFileV12.OpenAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), CancellationToken.None).GetAwaiter().GetResult();

    private SolutionModel CreateSolutionModelFromSlnx(string content)
        => SolutionSerializers.SlnXml.OpenAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), CancellationToken.None).GetAwaiter().GetResult();

    private string SerializeSolutionToSlnx(SolutionModel sln)
    {
        var stream = new MemoryStream();
        SolutionSerializers.SlnXml.SaveAsync(stream, sln, CancellationToken.None).GetAwaiter().GetResult();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private string SerializeSolutionToSln(SolutionModel sln)
    {
        var stream = new MemoryStream();
        SolutionSerializers.SlnFileV12.SaveAsync(stream, sln, CancellationToken.None).GetAwaiter().GetResult();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
