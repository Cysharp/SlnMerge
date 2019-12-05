# SlnMerge

Unity でソリューションファイル生成時に指定したソリューションをマージするエディタ拡張です。

Unity とは別にサーバーサイドの C# プロジェクトおよびソリューションがあるようなケースで、Unity のソリューションで同時に開くことができるようになります。

![](docs/images/SlnMerge-Image-01.png)

## 動作確認環境
- Unity 2018.4.5f1 + Windows 10 & macOS 10.15
- Microsoft Visual Studio 2019
- JetBrains Rider 2019.2

## インストール
### 1. Unity プロジェクトの `Assets/Editor` 配下に `src/SlnMerge.cs` をコピーする

### 2. `プロジェクト名.sln.mergesettings` ファイルでマージしたいソリューションを指定する
Unity によって生成されるソリューションファイル名に .mergesettings をつけた名前の設定ファイルを用意します。

例えば MyUnityApp プロジェクトの場合は MyUnityApp.sln が生成されるので MyUnityApp.sln.mergesettings ファイルを作成します。

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\MyUnityApp.Server.sln</MergeTargetSolution>
</SlnMergeSettings>
```

**メモ:** ファイルが未指定の場合には `プロジェクト名.Merge.sln` が読み込まれます

## 設定
mergesettings ファイルには次の設定項目があります。

- `Disabled`: SlnMerge を無効にするかどうか (デフォルト: `false`)
- `MergeTargetSolution`: マージしたいソリューションのパス
- `NestedProjects`: ネストするプロジェクトを指定します。通常ソリューションフォルダーとして利用します
    - `NestedProject/FolderPath`: ソリューション上のフォルダーパス (存在しない場合は生成。GUIDと排他)
    - `NestedProject/FolderGuid`: ソリューション上のフォルダーのGUID (パスと排他)
    - `NestedProject/ProjectName`: プロジェクト名 (GUIDと排他)
    - `NestedProject/ProjectGuid`: プロジェクトGUID (プロジェクト名と排他)

### ソリューションフォルダーに追加する
`NestedProjects` 設定を使用するとマージ後にプロジェクトをソリューションフォルダーへ移動できます。ベースのソリューションにソリューションフォルダーが存在しない場合には自動で追加しますが、ソリューションフォルダーの定義が設定ファイルに必要です。

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\ChatApp.Server.sln</MergeTargetSolution>
    <SolutionFolders>
        <!-- Unity という名前のソリューションフォルダーを GUID とともに定義する -->
        <SolutionFolder FolderPath="Unity" Guid="{55739033-89BA-48AE-B482-843AFD452468}">
    </SolutionFolder>
    <NestedProjects>
        <NestedProject ProjectName="Assembly-CSharp" FolderPath="Unity" />
        <NestedProject ProjectName="Assembly-CSharp-Editor" FolderPath="Unity" />
    </NestedProjects>
</SlnMergeSettings>
```

## ライセンス
MIT License