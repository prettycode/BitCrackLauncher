using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using NBitcoin;

namespace BitCrackLauncher
{
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

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            ProgramConfig config = new(args);

            if (!config.IsDebug && IsProgramAlreadyRunning())
            {
                Console.Beep();
                Console.Beep();
                Console.Beep();
                return;
            }

            if (!File.Exists(config.CrackerPath))
            {
                throw new InvalidOperationException($"BitCrack executable not found at: '{config.CrackerPath}'");
            }

            if (!Directory.Exists(Path.GetDirectoryName(config.SolutionOutputFilePattern)))
            {
                throw new InvalidOperationException($"Solution directory does not exit: '{config.SolutionOutputFilePattern}'");
            }

            SetupProcessExitHandlers(config.CrackerPath);

            await SpawnCrackerInstances(config);
        }

        private static async Task SpawnCrackerInstances(ProgramConfig config)
        {
            while (true)
            {
                if (await IsCrackingCompleted(config.CrackerPath, config.SolutionOutputFilePattern))
                {
                    return;
                }

                KillCrackerInstances(config.CrackerPath);

                var startTime = DateTime.UtcNow;

                for (int i = 0; i < config.ConcurrentCrackerInstancesCount; i++)
                {
                    ProcessStartInfo crackerProcessInfo = CreateCrackerProcessStartInfo(
                        config.SolutionOutputFilePattern,
                        config.CrackerPath,
                        config.Puzzle.AddressToCrack,
                        config.Puzzle.GetRandomKeyspace(),
                        out string crackerCommand);

                    Console.WriteLine($"Opening separate command prompt running:\n\n{crackerCommand}\n");

                    Process.Start(crackerProcessInfo);
                }

                Console.Write($"Waiting {config.TimeBeforeRestart.Humanize()} before restarting in new keyspaces. Restarting in: ");

                var endTime = startTime.Add(config.TimeBeforeRestart);
                var timePosition = Console.CursorLeft;
                var timeTop = Console.CursorTop;

                for (TimeSpan remainingTime; (remainingTime = endTime - DateTime.UtcNow) > TimeSpan.Zero;)
                {
                    Console.SetCursorPosition(timePosition, timeTop);
                    Console.Write($"{remainingTime.Hours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}...");

                    await Task.Delay(1000);
                }

                Console.WriteLine("\n\nRestarting in new keyspaces...\n");
            }
        }

        private static bool IsProgramAlreadyRunning()
        {
            return Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1;
        }

        private static void SetupProcessExitHandlers(string crackerPath)
        {
            // e.g. Closing the window
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => KillCrackerInstances(crackerPath);

            // e.g. Ctrl-C
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                KillCrackerInstances(crackerPath);
                Environment.Exit(0);
            };
        }

        private static async Task<bool> IsCrackingCompleted(string crackerPath, string solutionOutputFilePattern)
        {
            static void openInShell(string path) => Process.Start(new ProcessStartInfo
            {
                Arguments = path,
                FileName = "explorer.exe"
            });

            string solutionDirectory = Path.GetDirectoryName(solutionOutputFilePattern);
            string[] solutionFiles = Directory.GetFiles(solutionDirectory, Path.GetFileName(solutionOutputFilePattern));

            if (solutionFiles.Length == 0)
            {
                return false;
            }

            Console.WriteLine($"\nFound solution in '{Path.GetDirectoryName(crackerPath)}'!\n\nPress Enter key to exit.");

            openInShell(solutionDirectory);
            openInShell(solutionFiles[0]);

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter))
            {
                Console.Beep();
                await Task.Delay(100);
            }

            return true;
        }

        private static ProcessStartInfo CreateCrackerProcessStartInfo(
            string solutionOutputFilePattern,
            string crackerPath,
            string addressToCrack,
            string randomKeyspaceStart,
            out string command)
        {
            if (!Path.GetFileName(solutionOutputFilePattern).Contains('*'))
            {
                throw new ArgumentException($"Filename is missing an asterisk: '{solutionOutputFilePattern}', e.g. 'solution.*.txt'",
                    nameof(solutionOutputFilePattern));
            }

            Thread.Sleep(1);

            string unixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string privateKeyOutputPath = solutionOutputFilePattern.Replace("*", unixTimeMs);

            command = $"\"{crackerPath}\" --keyspace {randomKeyspaceStart} {addressToCrack} -o \"{privateKeyOutputPath}\"";

            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{command}\"",
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Minimized
            };
        }

        private static void KillCrackerInstances(string crackerPath)
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(crackerPath));

            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }

    internal class BitcoinPuzzle
    {
        public readonly string AddressToCrack;
        public readonly BigInteger KeyspaceStartInclusive;
        public readonly BigInteger KeyspaceEndInclusive;

        public BitcoinPuzzle(string addressToCrack, string hexRangeLowInclusive, string hexRangeHighInclusive)
        {
            if (!IsValidAddress(addressToCrack))
            {
                throw new ArgumentException("Invalid public key.", nameof(addressToCrack));
            }

            this.AddressToCrack = addressToCrack;

            if (!BigInteger.TryParse(hexRangeLowInclusive, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out this.KeyspaceStartInclusive))
            {
                throw new ArgumentException("Invalid hexadecimal number.", nameof(hexRangeLowInclusive));
            }

            if (!BigInteger.TryParse(hexRangeHighInclusive, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out this.KeyspaceEndInclusive))
            {
                throw new ArgumentException("Invalid hexadecimal number.", nameof(hexRangeHighInclusive));
            }
        }

        public string GetRandomKeyspace()
        {
            return GetRandomBigInteger(this.KeyspaceStartInclusive, this.KeyspaceEndInclusive).ToString("X");
        }

        private static bool IsValidAddress(string address)
        {
            try
            {
                BitcoinAddress.Create(address, Network.Main);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static BigInteger GetRandomBigInteger(BigInteger minValueInclusive, BigInteger maxValueInclusive)
        {
            if (minValueInclusive >= maxValueInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(minValueInclusive), $"Cannot be greater than or equal to {maxValueInclusive}");
            }

            BigInteger range = maxValueInclusive - minValueInclusive + 1;
            int bytesNeeded = range.GetByteCount();
            byte[] bytes = new byte[bytesNeeded];
            BigInteger result;

            do
            {
                RandomNumberGenerator.Fill(bytes);

                // Ensure a positive value
                bytes[bytesNeeded - 1] &= 0x7F;

                result = new BigInteger(bytes);
            }
            while (result < 0 || result >= range);

            return result + minValueInclusive;
        }
    }
}