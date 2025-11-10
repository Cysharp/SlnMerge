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
    public void Test1()
    {
        // Arrange
        var slnxOverlayPath = @"C:\repos\src\Overlay.slnx".ToCurrentPlatformPathForm();
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
