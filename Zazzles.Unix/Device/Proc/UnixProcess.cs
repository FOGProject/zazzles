/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2018 FOG Project
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
