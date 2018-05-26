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
using System.Security;
using Zazzles.Core.Device.Proc;

namespace Zazzles.Windows.Device.Proc
{
    public class WindowsProcess : IProcess
    {
        private const int MAX_WAIT_TIME = 30 * 1000;

        // user@domain
        // Uses string, inherent security concerns, perhaps use SecureString in the future
        public Process ImpersonateProcess(string file, string arguments, string user, string password)
        {
            string domain = null;

            var userInfo = user.Split('@');
            if (userInfo.Length == 2)
            {
                user = userInfo[1];
                domain = userInfo[0];
            }

            var proc = new Process
            {
                StartInfo = {
                    FileName = file,
                    Arguments = arguments,
                    Domain = domain,
                    UserName = user
                }
            };

            var ssPwd = new SecureString();
            for (int x = 0; x < password.Length; x++)
            {
                ssPwd.AppendChar(password[x]);
            }

            proc.StartInfo.Password = ssPwd;

            return proc;
        }

        public Process PipeProcessToDisplay(string file, string arguments)
        {
            throw new NotImplementedException();
        }

        public bool Kill(string name, bool all = false)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill();
                if (!all)
                    break;
            }

            return true;
        }
    }
}
