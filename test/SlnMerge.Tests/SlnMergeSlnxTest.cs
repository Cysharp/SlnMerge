// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using SlnMerge.Xml;
using Xunit;

namespace SlnMerge.Tests;

public class SlnMergeSlnxTest
{
    [Fact]
    public void Merge()
    {
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
        var slnxOrigXml = """
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

        var slnxOverlay = SlnxFile.ParseFromXml("..\\Overlay.slnx", slnxOverlayXml);
        var slnxOrig = SlnxFile.ParseFromXml("Original.slnx", slnxOrigXml);

        var slnxMerged = SlnxFile.Merge(slnxOrig, slnxOverlay);

    }
}
