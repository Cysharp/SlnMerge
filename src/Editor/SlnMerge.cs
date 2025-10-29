// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

// ReSharper disable All

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SlnMerge.Legacy;

namespace SlnMerge
{
    public static class SlnMerge
    {
        public static bool TryMerge(string solutionFilePath, ISlnMergeLogger logger, [NotNullWhen(true)] out string? resultSolutionContent)
        {
            return TryMerge(solutionFilePath, File.ReadAllText(solutionFilePath), logger, out resultSolutionContent);
        }

        public static bool TryMerge(string solutionFilePath, string solutionFileContent, ISlnMergeLogger logger, [NotNullWhen(true)] out string? resultSolutionContent)
        {
            return SlnMergeLegacy.TryMerge(solutionFilePath, solutionFileContent, logger, out resultSolutionContent);
        }
    }
}
