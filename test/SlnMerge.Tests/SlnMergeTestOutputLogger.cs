// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using Xunit.Abstractions;

namespace SlnMerge.Tests;

internal class SlnMergeTestOutputLogger(ITestOutputHelper output) : ISlnMergeLogger
{
    public void Warn(string message) => output.WriteLine($"[Warn] {message}");

    public void Error(string message, Exception ex) => output.WriteLine($"[Error] {message}: {ex}");

    public void Information(string message) => output.WriteLine($"[Info] {message}");

    public void Debug(string message) => output.WriteLine($"[Debug] {message}");
}
