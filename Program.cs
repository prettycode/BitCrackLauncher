using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;

namespace BitCrackLauncher
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            ProgramConfig config = new(args);

            if (!config.IsDebug && IsProgramAlreadyRunning())
            {
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
}