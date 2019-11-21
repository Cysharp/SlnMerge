# SlnMerge

SlnMerge merges the solutions when creating solution file by Unity Editor.

[日本語](README.ja.md)

![](docs/images/SlnMerge-Image-01.png)

## Works with
- Unity 2018.4.5f1 + Windows 10 and macOS 10.15
- Microsoft Visual Studio 2019
- JetBrains Rider 2019.2

## How to use
### 1. Copy `src/SlnMerge.cs` to `Assets/Editor` in your Unity project.

### 2. Create `ProjectName.sln.mergesettings` and configure a target solution.
Create a setting XML file named `<ProjectName>.sln.mergesettings`.

For example, when the project name is `MyUnityApp`, Unity Editor generates `MyUnityApp.sln`. You need to create `MyUnityApp.sln.mergesettings`.

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\MyUnityApp.Server.sln</MergeTargetSolution>
</SlnMergeSettings>
```

You can specify the target solution to merge by `MergeTargetSolution` element.

**NOTE:** If you don't have the settings, SlnMerge uses `ProjectName.Merge.sln` as a target.

## Settings

### Add projects to solution folders
You can use `NestedProjects` settings to move projects to solution folders. When a solution folder doesn't exist, SlnMerge will create the solution folder automatically.

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\ChatApp.Server.sln</MergeTargetSolution>
    <NestedProjects>
        <NestedProject ProjectName="Assembly-CSharp" FolderPath="Unity" />
        <NestedProject ProjectName="Assembly-CSharp-Editor" FolderPath="Unity" />
    </NestedProjects>
</SlnMergeSettings>
```

## License
MIT License