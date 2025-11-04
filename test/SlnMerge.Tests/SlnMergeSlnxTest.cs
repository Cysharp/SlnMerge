// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using SlnMerge.Xml;
using Xunit;
using Xunit.Sdk;

namespace SlnMerge.Tests;

public class SlnMergeSlnxTest
{
    [Fact]
    public void Merge()
    {
        // Arrange
        var slnxOverlayXml = """
                          <Solution>
                            <Configurations>
                              <BuildType Name="Debug" />
                              <BuildType Name="Publish" />
                              <BuildType Name="Release" />
                            </Configurations>
                            <Folder Name="/Solution Items/">
                              <File Path=".gitlab-ci.yml" />
                              <File Path="Directory.Build.props" />
                              <File Path="LICENSE.md" />
                              <File Path="README.md" />
                              <File Path="signfile.bat" />
                              <File Path="version.json" />
                            </Folder>
                            <Folder Name="/Tools/">
                              <Project Path="tools/MyTool/MyTool.csproj" Id="13fddb1c-d4e7-453a-b582-7de8ba522a6e">
                                <BuildType Solution="Publish|*" Project="Release" />
                              </Project>
                            </Folder>
                            <Folder Name="/Server/">
                              <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                <BuildType Solution="Publish|*" Project="Release" />
                              </Project>
                            </Folder>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                            <Project Path="test/MyApp.Tests/MyApp.Tests.csproj" />
                          </Solution>
                          """;
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

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Configurations);
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);
        Assert.Equal(4, slnxMerged.Root.Configurations?.Children.Length);
        Assert.Equal(3, slnxMerged.Root.Folders.Count);
        Assert.Equal(6, slnxMerged.Root.Projects.Count);

        Assert.Equal([
            "MyApp.Client.csproj".ToCurrentPlatformPathForm(),
            "MyTool.Client.csproj".ToCurrentPlatformPathForm(),
            "../tools/MyTool/MyTool.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Server/MyApp.Server.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Shared/MyApp.Shared.csproj".ToCurrentPlatformPathForm(),
            "../test/MyApp.Tests/MyApp.Tests.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        Assert.Equal(1, slnxMerged.Root.Projects["../src/MyApp.Server/MyApp.Server.csproj".ToCurrentPlatformPathForm()].Children.Length);

        Assert.Equal([
            "/Tools/",
            "/Solution Items/",
            "/Server/",
        ], slnxMerged.Root.Folders.Keys.ToArray());

        Assert.Equal(6, slnxMerged.Root.Folders["/Solution Items/"].Children.Length);
    }

    [Fact]
    public void Merge_RewritePath()
    {
        // Arrange
        var slnxOverlayXml = """
                          <Solution>
                            <Folder Name="/Solution Items/">
                              <File Path="docs/Overlay.md" />
                            </Folder>
                            <File Path="README.md" />
                            <Project Path="src/Overlay/Overlay.csproj" />
                          </Solution>
                          """;
        var slnxBaseXml = """
                       <Solution>
                         <Folder Name="/Solution Items/">
                           <File Path="docs/Base.md" />
                         </Folder>
                         <File Path="README.md" />
                         <Project Path="src/Base/Base.csproj" />
                       </Solution>
                       """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        var filePaths = Traverse<IFilePathElement>(slnxMerged.Root.Children).Select(x => x.Path.ToCurrentPlatformPathForm()).ToArray();
        Assert.NotEmpty(filePaths);
        Assert.Equal([
            "docs/Base.md".ToCurrentPlatformPathForm(),
            "../Overlay/docs/Overlay.md".ToCurrentPlatformPathForm(),
            "README.md".ToCurrentPlatformPathForm(),
            "src/Base/Base.csproj".ToCurrentPlatformPathForm(),
            "../Overlay/README.md".ToCurrentPlatformPathForm(),
            "../Overlay/src/Overlay/Overlay.csproj".ToCurrentPlatformPathForm(),
        ], filePaths);

        static IEnumerable<T> Traverse<T>(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is T t)
                {
                    yield return t;
                }

                if (node is IElement e)
                {
                    foreach (var c in Traverse<T>(e.Children))
                    {
                        yield return c;
                    }
                }
            }
        }

    }

    [Fact]
    public void Merge_SolutionFolder_AlreadyExists()
    {
        // Arrange
        var slnxOverlayXml = """
                             <Solution>
                               <Folder Name="/New Folder1/">
                                 <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                                   <BuildType Solution="Publish|*" Project="Release" />
                                 </Project>
                                 <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                               </Folder>
                             </Solution>
                             """;
        var slnxBaseXml = """
                          <Solution>
                            <Folder Name="/New Folder1/">
                              <Project Path="MyApp.Client.csproj" />
                            </Folder>
                          </Solution>
                          """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [ new() { FolderPath = "New Folder1" } ],
            NestedProjects = [ ],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.Client.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Server/MyApp.Server.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Shared/MyApp.Shared.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        Assert.Equal([
            "/New Folder1/",
        ], slnxMerged.Root.Folders.Keys.ToArray());

        Assert.Equal(3, slnxMerged.Root.Folders["/New Folder1/"].Children.Length);
    }

    [Fact]
    public void Merge_SolutionFolder_NewFolder()
    {
        // Arrange
        var slnxOverlayXml = """
                          <Solution>
                            <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                              <BuildType Solution="Publish|*" Project="Release" />
                            </Project>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                          </Solution>
                          """;
        var slnxBaseXml = """
                       <Solution>
                         <Project Path="MyApp.Client.csproj" />
                       </Solution>
                       """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [ new() { FolderPath = "New Folder1" } ],
            NestedProjects = [ ],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.Client.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Server/MyApp.Server.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Shared/MyApp.Shared.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        Assert.Equal([
            "/New Folder1/",
        ], slnxMerged.Root.Folders.Keys.ToArray());

        Assert.Equal(0, slnxMerged.Root.Folders["/New Folder1/"].Children.Length);
    }

    [Fact]
    public void Merge_SolutionFolder_NewFolder_NestedFolder()
    {
        // Arrange
        var slnxOverlayXml = """
                          <Solution>
                            <Project Path="src/MyApp.Server/MyApp.Server.csproj">
                              <BuildType Solution="Publish|*" Project="Release" />
                            </Project>
                            <Project Path="src/MyApp.Shared/MyApp.Shared.csproj" />
                          </Solution>
                          """;
        var slnxBaseXml = """
                       <Solution>
                         <Project Path="MyApp.Client.csproj" />
                       </Solution>
                       """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Client\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [new() { FolderPath = "New Folder1" }, new() { FolderPath = "New Folder2" }],
            NestedProjects = [new() { ProjectName = "MyApp.Client", FolderPath = "New Folder1" }, new() { ProjectName = "MyApp.Server", FolderPath = "New Folder2" }],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.Client.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Server/MyApp.Server.csproj".ToCurrentPlatformPathForm(),
            "../src/MyApp.Shared/MyApp.Shared.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        Assert.Equal([
            "/New Folder1/",
            "/New Folder2/",
        ], slnxMerged.Root.Folders.Keys.ToArray());

        Assert.Single(slnxMerged.Root.Children.Where(x => x is ProjectElement));
        Assert.Equal(1, slnxMerged.Root.Folders["/New Folder1/"].Children.Length);
        Assert.Equal(1, slnxMerged.Root.Folders["/New Folder2/"].Children.Length);
    }

    [Fact]
    public void Merge_Conflict_PreserveOverlay()
    {
        // Arrange
        var slnxOverlayXml = """
                          <Solution>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Release" />
                            </Project>
                          </Solution>
                          """;
        var slnxBaseXml = """
                       <Solution>
                         <Project Path="MyApp.csproj" />
                       </Solution>
                       """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        var projs = slnxMerged.Root.Children.OfType<ProjectElement>().ToArray();
        Assert.Single(projs);
        Assert.Single(projs[0].Children); // BuildType from overlay
        Assert.Equal("Release", ((IElement)projs[0].Children[0]).Attributes.SingleOrDefault(x => x.Name == "Project").Value); // BuildType from overlay
    }


    [Fact]
    public void Merge_Conflict_PreserveOverlay_Children()
    {
        // Arrange
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Debug" />
                            </Project>
                          </Solution>
                          """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveOverlay,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        var projs = slnxMerged.Root.Children.OfType<ProjectElement>().ToArray();
        Assert.Single(projs);
        Assert.Single(projs[0].Children); // BuildType from overlay
        Assert.Equal("Release", ((IElement)projs[0].Children[0]).Attributes.SingleOrDefault(x => x.Name == "Project").Value); // BuildType from overlay
    }

    [Fact]
    public void Merge_Conflict_PreserveBase()
    {
        // Arrange
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        var projs = slnxMerged.Root.Children.OfType<ProjectElement>().ToArray();
        Assert.Single(projs);
        Assert.Empty(projs[0].Children); // BuildType from base
    }

    [Fact]
    public void Merge_Conflict_PreserveBase_Children()
    {
        // Arrange
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj">
                              <BuildType Solution="Publish|*" Project="Debug" />
                            </Project>
                          </Solution>
                          """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveUnity,
        };
        var slnxMerged = SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance);

        // Assert
        Assert.NotNull(slnxMerged.Root.Folders);
        Assert.NotNull(slnxMerged.Root.Projects);

        Assert.Equal([
            "MyApp.csproj".ToCurrentPlatformPathForm(),
        ], slnxMerged.Root.Projects.Keys.ToArray());

        var projs = slnxMerged.Root.Children.OfType<ProjectElement>().ToArray();
        Assert.Single(projs);
        Assert.Single(projs[0].Children); // BuildType from base
        Assert.Equal("Debug", ((IElement)projs[0].Children[0]).Attributes.SingleOrDefault(x => x.Name == "Project").Value); // BuildType from overlay
    }

    [Fact]
    public void Merge_Conflict()
    {
        // Arrange
        var slnxOverlayXml = """
                             <Solution>
                               <Project Path="MyApp.csproj">
                                 <BuildType Solution="Publish|*" Project="Release" />
                               </Project>
                             </Solution>
                             """;
        var slnxBaseXml = """
                          <Solution>
                            <Project Path="MyApp.csproj" />
                          </Solution>
                          """;

        // Act
        var slnxOverlay = SlnxFile.ParseFromXml(@"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm(), slnxOverlayXml);
        var slnxBase = SlnxFile.ParseFromXml(@"C:\repos\src\Base.slnx".ToCurrentPlatformPathForm(), slnxBaseXml);
        var slnMergeSettings = new SlnMergeSettings()
        {
            SolutionFolders = [],
            NestedProjects = [],
            ProjectConflictResolution = ProjectConflictResolution.PreserveAll,
        };

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => SlnMergeXml.Merge(slnxBase, slnxOverlay, slnMergeSettings, SlnMergeNullLogger.Instance));
    }
}
