// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using System;

namespace SlnMerge
{
    public interface ISlnMergeLogger
    {
        void Warn(string message);
        void Error(string message, Exception ex);
        void Information(string message);
        void Debug(string message);
    }

    public class SlnMergeConsoleLogger : ISlnMergeLogger
    {
        public static ISlnMergeLogger Instance { get; } = new SlnMergeConsoleLogger();

        private SlnMergeConsoleLogger() { }

        public void Warn(string message)
        {
            Console.WriteLine($"[Warn] {message}");
        }

        public void Error(string message, Exception ex)
        {
            Console.Error.WriteLine($"[Error] {message}");
            Console.Error.WriteLine(ex.ToString());
        }

        public void Information(string message)
        {
            Console.WriteLine($"[Info] {message}");
        }

        public void Debug(string message)
        {
            Console.WriteLine($"[Debug] {message}");
        }
    }
}
