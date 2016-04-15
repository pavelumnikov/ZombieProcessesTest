using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestProcessPrimary.Utils;

namespace TestProcessPrimary
{
    class Program
    {
        private const string ChildProcessName = "TestProcessChild.exe";
        private static readonly IDictionary<int, Process> MyProcesses = new ConcurrentDictionary<int, Process>();

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("\nWrite command:");

                var input = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("start"))
                    AddProcess();

                if(input.StartsWith("stop"))
                    ParseStopProcess(input);

                if (input.Equals("list"))
                    ListProcesses();

                if (input.Equals("exit"))
                    break;
            }

            Console.WriteLine("Exiting...");
            Exit();
        }

        private static void AddProcess()
        {
            var process = CreateAndStartProcess(ChildProcessName);

            Console.WriteLine($"Starting process: id[{process.Id}], name:[{process.ProcessName}]");

            MyProcesses.Add(process.Id, process);
        }

        private static void StopSelectedProcess(Process process, bool wait)
        {
            if(process == null)
                throw new NullReferenceException(nameof(process));

            if (process.HasExited)
                return;

            process.Kill();

            if (wait)
            {
                Console.WriteLine($"Waiting for process id[{process.Id}] exit...");
                process.WaitForExit();
            }

            Console.WriteLine($"Stopped process: id[{process.Id}], name:[{process.ProcessName}]");
        }

        private static void ParseStopProcess(string input)
        {
            const string pidSelectorDelemiter = "=>";

            var hasSelectedChildProcessId = input.Contains(pidSelectorDelemiter);
            if (hasSelectedChildProcessId)
            {
                ProcessStopCommandForList(input, pidSelectorDelemiter);
            }
            else
            {
                StopProcess(null, HasWait(input));
            }
        }

        private static void ProcessStopCommandForList(string input, string delimiter)
        {
            const int informationPieceIndex = 0;
            const int firstPidDataIndex = 1;

            var inputPieces = input.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            var infoLine = inputPieces[informationPieceIndex];
            var hasWait = HasWait(infoLine);

            Parallel.For(firstPidDataIndex, inputPieces.Length, index =>
            {
                StopProcess(GetProcessIdFromString(inputPieces[index]), hasWait);
            });
        }

        private static int GetProcessIdFromString(string input)
        {
            int result;

            var inputTrimString = input.Trim(' ');
            if (!int.TryParse(inputTrimString, out result))
            {
                throw new InvalidCastException("Could not parse integer string PID to value!");
            }

            return result;
        }

        private static bool HasWait(string input)
        {
            return input.Contains("-wait");
        }

        private static void StopProcess(int? pid, bool wait)
        {
            if (!MyProcesses.Any())
            {
                Console.WriteLine("There are no processes to stop. Create one at least.");
                return;
            }

            Process process;
            int processKey;

            if (pid.HasValue)
            {
                processKey = pid.Value;

                var isSuccessfull = MyProcesses.TryGetValue(pid.Value, out process);
                if (!isSuccessfull)
                {
                    Console.WriteLine($"No such process with ID: [{pid.Value}]");
                    return;
                }
            }
            else
            {
                process = MyProcesses.Last().Value;
                processKey = process.Id;
            }

            StopSelectedProcess(process, wait);
            MyProcesses.Remove(processKey);
        }

        private static void ListProcesses()
        {
            if (!MyProcesses.Any())
            {
                Console.WriteLine("There are no created child processes!");
                return;
            }

            Console.WriteLine("All created processes:");

            foreach (var process in MyProcesses)
                Console.WriteLine($"Process: id[{process.Key}], name:[{process.Value.ProcessName}]");
        }

        private static void Exit()
        {
            if (!MyProcesses.Any())
                return;

            MyProcesses.Where(process => !process.Value.HasExited).AsParallel().ForAll(p =>
            {
                var process = p.Value;

                if(!process.HasExited)
                    process.Kill();

                process.WaitForExit();
            });
        }

        private static Process CreateAndStartProcess(string name)
        {
            const string defaultVerb = "";

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);

            Console.WriteLine($"Creating process by path: [{path}]");

            var processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ErrorDialog = false,
                Verb = defaultVerb,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if(PlatformUtils.IsWindows)
            {
                processInfo.FileName = path;
            }
            else
            {
                const string monoRunner = "mono";

                processInfo.FileName = monoRunner;
                processInfo.Arguments = path;
            }


            return Process.Start(processInfo);
        }
    }
}
