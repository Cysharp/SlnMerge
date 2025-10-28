// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;

namespace SlnMerge.Tests
{
    public class SlnMergeNullLogger : ISlnMergeLogger
    {
        public static ISlnMergeLogger Instance { get; } = new SlnMergeNullLogger();

        public void Warn(string message)
        {
        }

        public void Error(string message, Exception ex)
        {
        }

        public void Information(string message)
        {
        }

        public void Debug(string message)
        {
        }
    }
}
