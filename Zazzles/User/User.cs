/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2020 FOG Project
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
using Zazzles.UserComponents;

namespace Zazzles
{
    public static class User
    {
        private const string LogName = "User";
        private static readonly IUser _instance;

        static User()
        {
            switch (Settings.OS)
            {
                case Settings.OSType.Mac:
                    _instance = new MacUser();
                    break;
                case Settings.OSType.Linux:
                    _instance = new LinuxUser();
                    break;
                default:
                    _instance = new WindowsUser();
                    break;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>True if a user is logged in</returns>
        public static bool AnyLoggedIn()
        {
            return AllLoggedIn().Count > 0;
        }

        /// <summary>
        /// </summary>
        /// <returns>The current username</returns>
        public static string Current()
        {
            try
            {
                return Environment.UserName;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to get current user");
                Log.Error(LogName, ex);
            }

            return "";
        }

        /// <summary>
        /// </summary>
        /// <returns>The inactivity time of the current user in seconds</returns>
        public static int InactivityTime()
        {
            try
            {
                return _instance.GetInactivityTime();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to get user inactivity time");
                Log.Error(LogName, ex);
            }

            return -1;
        }

        /// <summary>
        /// </summary>
        /// <returns>A list of usernames</returns>
        public static List<string> AllLoggedIn()
        {
            try
            {
                return _instance.GetUsersLoggedIn();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to get logged in users");
                Log.Error(LogName, ex);
            }

            return new List<string>();
        }
    }
}