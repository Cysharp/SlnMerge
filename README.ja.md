[![GitHub Actions](https://github.com/Cysharp/SlnMerge/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/SlnMerge/actions) [![Releases](https://img.shields.io/github/release/Cysharp/SlnMerge.svg)](https://github.com/Cysharp/SlnMerge/releases)

# SlnMerge

Unity でソリューションファイル生成時に指定したソリューションをマージするエディタ拡張です。

Unity とは別にサーバーサイドの C# プロジェクトおよびソリューションがあるようなケースで、Unity のソリューションで同時に開くことができるようになります。

![](docs/images/SlnMerge-Image-01.png)

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [動作確認環境](#%E5%8B%95%E4%BD%9C%E7%A2%BA%E8%AA%8D%E7%92%B0%E5%A2%83)
- [使い方](#%E4%BD%BF%E3%81%84%E6%96%B9)
  - [1. SlnMerge をインストールする](#1-slnmerge-%E3%82%92%E3%82%A4%E3%83%B3%E3%82%B9%E3%83%88%E3%83%BC%E3%83%AB%E3%81%99%E3%82%8B)
  - [2. `プロジェクト名.sln.mergesettings` ファイルでマージしたいソリューションを指定する](#2-%E3%83%97%E3%83%AD%E3%82%B8%E3%82%A7%E3%82%AF%E3%83%88%E5%90%8Dslnmergesettings-%E3%83%95%E3%82%A1%E3%82%A4%E3%83%AB%E3%81%A7%E3%83%9E%E3%83%BC%E3%82%B8%E3%81%97%E3%81%9F%E3%81%84%E3%82%BD%E3%83%AA%E3%83%A5%E3%83%BC%E3%82%B7%E3%83%A7%E3%83%B3%E3%82%92%E6%8C%87%E5%AE%9A%E3%81%99%E3%82%8B)
- [設定](#%E8%A8%AD%E5%AE%9A)
  - [ソリューションフォルダーに追加する](#%E3%82%BD%E3%83%AA%E3%83%A5%E3%83%BC%E3%82%B7%E3%83%A7%E3%83%B3%E3%83%95%E3%82%A9%E3%83%AB%E3%83%80%E3%83%BC%E3%81%AB%E8%BF%BD%E5%8A%A0%E3%81%99%E3%82%8B)
- [トラブルシューティング](#%E3%83%88%E3%83%A9%E3%83%96%E3%83%AB%E3%82%B7%E3%83%A5%E3%83%BC%E3%83%86%E3%82%A3%E3%83%B3%E3%82%B0)
  - [常にソリューションファイルが再生成され、Visual Studioに競合ダイアログが表示される](#%E5%B8%B8%E3%81%AB%E3%82%BD%E3%83%AA%E3%83%A5%E3%83%BC%E3%82%B7%E3%83%A7%E3%83%B3%E3%83%95%E3%82%A1%E3%82%A4%E3%83%AB%E3%81%8C%E5%86%8D%E7%94%9F%E6%88%90%E3%81%95%E3%82%8Cvisual-studio%E3%81%AB%E7%AB%B6%E5%90%88%E3%83%80%E3%82%A4%E3%82%A2%E3%83%AD%E3%82%B0%E3%81%8C%E8%A1%A8%E7%A4%BA%E3%81%95%E3%82%8C%E3%82%8B)
- [ライセンス](#%E3%83%A9%E3%82%A4%E3%82%BB%E3%83%B3%E3%82%B9)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## 動作確認環境
- Unity 2022.3+
- Windows 11 & macOS 10.15
- Microsoft Visual Studio 2022/2026
- JetBrains Rider 2025.x

レガシーソリューションフォーマット (.sln) とモダンなソリューションフォーマット (.slnx) の両方に対応しています。

## 使い方
### 1. SlnMerge をインストールする
Package Manager を使用して git リポジトリからパッケージをインストールできます。

![](docs/images/SlnMerge-Image-02.png)

```
https://github.com/Cysharp/SlnMerge.git?path=src
```

### 2. `プロジェクト名.sln.mergesettings` ファイルでマージしたいソリューションを指定する
Unity によって生成されるソリューションファイル名に .mergesettings をつけた名前の設定ファイルを用意します。

例えば MyUnityApp プロジェクトの場合は MyUnityApp.sln が生成されるので MyUnityApp.sln.mergesettings ファイルを作成します。

> [!NOTE]
> `.slnx` フォーマットを使用している場合には以降の `.sln` は `.slnx` と読み替えてください。`.sln` が指定されている場合、`.slnx` をフォールバック対象として取り扱います。

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\MyUnityApp.Server.sln</MergeTargetSolution>
</SlnMergeSettings>
```

> [!NOTE]
> `MergeTargetSolution` 要素が存在しない場合ファイルには `プロジェクト名.Merge.sln` が読み込まれます。
> `MergeTargetSolution` 要素が存在し、要素の内容が空の場合には暗黙的に空のソリューションとマージされます。これはフォルダー分けの機能のみを使用したい場合に役立ちます。

## 設定
mergesettings ファイルには次の設定項目があります。

- `Disabled`: SlnMerge を無効にするかどうか (デフォルト: `false`)
- `MergeTargetSolution`: マージしたいソリューションのパス
- `NestedProjects`: ネストするプロジェクトを指定します。通常ソリューションフォルダーとして利用します
    - `NestedProject/FolderPath`: ソリューション上のフォルダーパス (存在しない場合は生成)
    - `NestedProject/ProjectName`: プロジェクト名
        - ワイルドカードが使用可能です (`?`, `*`)
- `ProjectConflictResolution`: マージ元とマージ先でソリューション内に同名のプロジェクトがある場合の処理方法 (`PreserveAll`, `PreserveUnity`, `PreserveOverlay`) (デフォルト: `PreserveUnity`)
    - `PreserveAll`: すべてのプロジェクトを残します (Unity とマージ対象のソリューションのプロジェクトの両方)
    - `PreserveUnity`: Unity が生成したソリューションのプロジェクトを残します (マージ対象のソリューションのプロジェクトを破棄)
    - `PreserveOverlay`: 上書きするソリューションのプロジェクトを残します (Unity が生成したソリューションのプロジェクトを破棄)

### ソリューションフォルダーに追加する
`NestedProjects` 設定を使用するとマージ後にプロジェクトをソリューションフォルダーへ移動できます。ベースのソリューションにソリューションフォルダーが存在しない場合には自動で追加します。

```xml
<SlnMergeSettings>
    <MergeTargetSolution>..\ChatApp.Server.sln</MergeTargetSolution>
    <NestedProjects>
        <NestedProject ProjectName="Assembly-CSharp" FolderPath="Unity" />
        <NestedProject ProjectName="Assembly-CSharp-Editor" FolderPath="Unity" />
    </NestedProjects>
</SlnMergeSettings>
```

## トラブルシューティング
### 常にソリューションファイルが再生成され、Visual Studioに競合ダイアログが表示される
1. Unity Editor を閉じる
2. Unity Editor が生成した .csproj と .sln を削除する
3. プロジェクトを Unity Editor で開きなおす

マージ対象のプロジェクトと Unity が生成するソリューションで同名のプロジェクトが存在する場合、`ProjectConflictResolution` オプションを使用して3つの方法でコンフリクトを解決できます。

- すべてのプロジェクトを維持
- マージ対象のソリューションのプロジェクトを維持
- Unity が生成したソリューションのプロジェクトを維持 (デフォルト)

## ライセンス
MIT License

```
Copyright (c) 2019 Cysharp, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

This library includes code derived from the following open source libraries.

================================================================================
Microsoft.VisualStudio.SolutionPersistence
https://github.com/microsoft/vs-solutionpersistence
================================================================================
The MIT License (MIT)

Copyright (c) Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
