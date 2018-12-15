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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zazzles.Core.Device.User;

namespace Zazzles.Linux.Device.User
{
    public class LinuxUser : IUser
    {
        public IEnumerable<string> GetUsersLoggedIn()
        {
            var usersInfo = new List<string>();

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "users",
                    Arguments = "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) break;

                    var users = line.Split(' ');
                    usersInfo.AddRange(users);

                }
            }

            return usersInfo.Distinct();
        }

        public int GetInactivityTime()
        {
            var time = "-1";

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xprintidle",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                    time = process.StandardOutput.ReadLine();
            }

            if (time != null)
                return int.Parse(time);

            return -1;
        }
    }
}