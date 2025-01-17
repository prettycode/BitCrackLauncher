using System;
using System.IO;

namespace BitCrackLauncher;

internal class ProgramConfig
{
    public BitcoinPuzzle Puzzle { get; set; } = new("1BY8GQbnueYofwSuFAT3USAhGjPrkxDdW9", "40000000000000000", "7ffffffffffffffff");

    public TimeSpan TimeBeforeRestart { get; set; } = TimeSpan.Parse("08:00:00");

    public int ConcurrentCrackerInstancesCount { get; set; } = 5;

    public string CrackerPath { get; set; } = @"C:\GitHub\BitCrackLauncher\clBitCrack.exe";

    public string SolutionOutputFilePattern { get { return Path.Combine(Path.GetDirectoryName(this.CrackerPath), "solution.*.txt"); } }

    public readonly bool IsDebug;

    public ProgramConfig(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return;
        }

        if (args.Length > 1 || !(String.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Only one command-line argument, 'debug', is supported.");
        }

        this.Puzzle = new("1E6NuFjCi27W5zoXg8TRdcSRq84zJeBW3k", "10", "1f");
        this.TimeBeforeRestart = TimeSpan.Parse("00:00:30");
        this.ConcurrentCrackerInstancesCount = 10;
        this.IsDebug = true;
    }
}