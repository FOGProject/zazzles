/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2016 FOG Project
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 3
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */


using System;
using System.Diagnostics;
using System.IO;

namespace Zazzles
{
    public static class ProcessHandler
    {
        private const string LogName = "Process";

        /// <summary>
        /// Run an EXE located in the client's directory
        /// </summary>
        /// <param name="file">The name of the EXE to run</param>
        /// <param name="param">Parameters to run the EXE with</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int RunClientEXE(string file, string param, bool wait = true)
        {
            string[] stdout;
            return RunClientEXE(file, param, wait, out stdout);
        }

        /// <summary>
        /// Run an EXE located in the client's directory
        /// </summary>
        /// <param name="file">The name of the EXE to run</param>
        /// <param name="param">Parameters to run the EXE with</param>
        /// <param name="stdout">An array to place stdout, split by lines</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int RunClientEXE(string file, string param, bool wait, out string[] stdout)
        {
            if (string.IsNullOrEmpty(file))
                throw new ArgumentException("File name must be provided!", nameof(file));

            return RunEXE(Path.Combine(Settings.Location, file), param, wait, out stdout);
        }

        /// <summary>
        /// Run an EXE
        /// </summary>
        /// <param name="filePath">The path of the EXE to run</param>
        /// <param name="param">Parameters to run the EXE with</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int RunEXE(string filePath, string param, bool wait = true)
        {
            string[] stdout;
            return RunEXE(filePath, param, wait, out stdout);
        }

        /// <summary>
        /// Run an EXE
        /// </summary>
        /// <param name="filePath">The path of the EXE to run</param>
        /// <param name="param">Parameters to run the EXE with</param>
        /// <param name="stdout">An array to place stdout, split by lines</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int RunEXE(string filePath, string param, bool wait, out string[] stdout)
        {
            // If the current OS is Windows, simply run the process
            if (Settings.OS == Settings.OSType.Windows)
                return Run(filePath, param, wait, out stdout);

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            // Re-write the param information to include mono
            param = $"mono {filePath} {param}";
            param = param.Trim();


            // Create a process with /bin/bash as the FileName so that we can run multiple commands in one line
            // This is needed for ensuring any GUIs will be rendered on the screen
            var procInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true

            };

            Log.Debug(LogName, "Running process...");
            Log.Debug(LogName, "--> Filepath:   " + procInfo.FileName);
            Log.Debug(LogName, "--> Parameters: " + param);

            stdout = null;

            using (var proc = new Process { StartInfo = procInfo })
            {
                proc.Start();

                // Pipe any GUI to the first display
                using (var sw = proc.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine("export DISPLAY=:0;" + param);
                    }
                }

                if (wait)
                {
                    var rawOutput = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    stdout = SplitOutput(rawOutput);
                }

                if (!proc.HasExited)
                    return -1;

                Log.Debug(LogName, $"--> Exit Code = {proc.ExitCode}");
                return proc.ExitCode;
            }
        }


        /// <summary>
        /// Run a process 
        /// </summary>
        /// <param name="filePath">The path of the executable to run</param>
        /// <param name="param">Parameters to run the process with</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int Run(string filePath, string param, bool wait = true)
        {
            string[] stdout;
            return Run(filePath, param, wait, out stdout);
        }

        /// <summary>
        /// Run a process 
        /// </summary>
        /// <param name="filePath">The path of the executable to run</param>
        /// <param name="param">Parameters to run the process with</param>
        /// <param name="stdout">An array to place stdout, split by lines</param>
        /// <param name="wait">Wait for the process to exit</param>
        /// <returns>The exit code of the process. Will be -1 if wait is false.</returns>
        public static int Run(string filePath, string param, bool wait, out string[] stdout)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            Log.Debug(LogName, "Running process...");
            Log.Debug(LogName, "--> Filepath:   " + filePath);
            Log.Debug(LogName, "--> Parameters: " + param);

            stdout = null;

            var procInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = filePath,
                Arguments = param,
                RedirectStandardOutput = true
            };

            try
            {
                using (var proc = new Process { StartInfo = procInfo })
                {
                    proc.Start();
                    if (wait)
                    {
                        var rawOutput = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        stdout = SplitOutput(rawOutput);
                    }

                    if (!proc.HasExited)
                        return -1;

                    Log.Debug(LogName, $"--> Exit Code = {proc.ExitCode}");
                    return proc.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to run process");
                Log.Error(LogName, ex);
            }

            return -1;
        }

        /// <summary>
        /// Create an process as another user. The process will not be run automatically. 
        /// This can only be done on unix systems.
        /// </summary>
        /// <param name="filePath">The path of the executable to run</param>
        /// <param name="param">Parameters to run the process with</param>
        /// <param name="user">The user to impersonate</param>
        /// <returns>The process created</returns>
        public static Process CreateImpersonatedClientEXE(string filePath, string param, string user)
        {
            if (Settings.OS == Settings.OSType.Windows)
                throw new NotSupportedException();
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("User name must be provided!", nameof(user));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            var fileName = "su";
            var arguments = "- " + user + " -c \"mono " + Path.Combine(Settings.Location, filePath) + " " + param;
            arguments = arguments.Trim();
            arguments = arguments + "\"";


            Log.Debug(LogName, "Creating impersonated process...");
            Log.Debug(LogName, "--> Filepath:   " + fileName);
            Log.Debug(LogName, "--> Parameters: " + arguments);

            var proc = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = arguments
                }
            };

            return proc;
        }

        /// <summary>
        /// Kill all instances of a process
        /// </summary>
        /// <param name="name">The name of the process</param>
        public static void KillAll(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Process name must be provided!", nameof(name));

            try
            {
                foreach (var process in Process.GetProcessesByName(name))
                    process.Kill();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not kill all processes named " + name);
                Log.Error(LogName, ex);
            }
        }

        /// <summary>
        ///  Kill all instances of an EXE
        /// </summary>
        /// <param name="name">The name of the process</param>
        public static void KillAllEXE(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Process name must be provided!", nameof(name));

            if (Settings.OS == Settings.OSType.Windows)
            {
                KillAll(name);
                return;
            }

            Run("pkill", "-f " + name, true);
        }

        /// <summary>
        ///     Kill the first instance of a process
        /// </summary>
        /// <param name="name">The name of the process</param>
        public static void Kill(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Process name must be provided!", nameof(name));

            try
            {
                var processes = Process.GetProcessesByName(name);
                if (processes.Length > 0)
                {
                    processes[0].Kill();
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to kill process " + name);
                Log.Error(LogName, ex);
            }
        }

        /// <summary>
        ///     Kill the first instance of an EXE
        /// </summary>
        /// <param name="name">The name of the EXE</param>
        public static void KillEXE(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Process name must be provided!", nameof(name));

            if (Settings.OS == Settings.OSType.Windows)
            {
                Kill(name);
                return;
            }

            Run("pgrep " + name + " | while read -r line; do kill $line; exit; done", "", true);
        }

        private static string[] SplitOutput(string output)
        {
            return output.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }
    }
}