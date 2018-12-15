/*
    Copyright(c) 2014-2018 FOG Project

    The MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using Zazzles.Core.Device.Proc;

namespace Zazzles.Unix.Device.Proc
{
    public class UnixProcess : IProcess
    {
        private const int MAX_WAIT_TIME = 30 * 1000;

        public Process ImpersonateProcess(string file, string arguments, string user, string password)
        {
            var procInfo = new ProcessStartInfo
            {
                FileName = "su",
                Arguments = $"- {user} -c \"{file} {arguments}\"",
            };

            var proc = new Process()
            {
                StartInfo =
                {
                    FileName = "su",
                    Arguments = $"- {user} -c \"{file} {arguments}\""
                }
            };

            return proc;
        }

        public Process PipeProcessToDisplay(string file, string arguments)
        {
            var procInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            var proc = new Process { StartInfo = procInfo };
            proc.Start();

            using (var sw = proc.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine($"export DISPLAY=:0;{file} {arguments}");
                }
            }

            return proc;
        }

        public bool Kill(string name, bool all = false)
        {
            // -x enforces exact matches
            // -n selects only the newest matching process

            var argument = $"-x {name}";
            if (!all)
                argument = "-n " + argument;

            var procInfo = new ProcessStartInfo
            {
                FileName = "pkill",
                Arguments = argument
            };

            try
            {
                using (var proc = new Process { StartInfo = procInfo })
                {
                    proc.Start();
                    proc.WaitForExit(MAX_WAIT_TIME);
                    return (proc.ExitCode == 0);
                }
            }
            catch (Exception ex)
            {
                // TODO: Log it
            }

            return false;
        }
    }
}
