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
