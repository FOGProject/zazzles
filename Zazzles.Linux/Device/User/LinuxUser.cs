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