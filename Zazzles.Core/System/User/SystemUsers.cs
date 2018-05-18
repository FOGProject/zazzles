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
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Zazzles.Core.System.User
{
    public class SystemUsers
    {
        private readonly IUser _userAPI;
        private readonly ILogger _logger;

        public SystemUsers(ILogger<SystemUsers> logger, IUser userAPI)
        {
            _userAPI = userAPI;
            _logger = logger;
        }

        /// <summary>
        /// </summary>
        /// <returns>True if a user is logged in</returns>
        public bool AnyLoggedIn()
        {
            using (_logger.BeginScope(nameof(AnyLoggedIn)))
            {
                try
                {
                    var loggedInUsers = GetAllLoggedInUsers();
                    return loggedInUsers.Count() > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Unable to calculate how many users are logged on", ex);
                    throw ex;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>The current username</returns>
        /// <exception cref="BlankUserNameException">Thrown if the user identity cannot be discovered</exception>
        public string GetCurrentUserName()
        {
            using (_logger.BeginScope(nameof(GetCurrentUserName)))
            {
                var username = Environment.UserName;

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Environment UserName is blank");
                    throw new BlankUserNameException();
                }
                else
                {
                    _logger.LogTrace("Environment.UserName set to '{username}'", username);
                }

                return username;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>The inactivity time of the current user in seconds</returns>
        public int GetInactivityTime()
        {
            using (_logger.BeginScope(nameof(GetInactivityTime)))
            {
                try
                {
                    var inactivityS = _userAPI.GetInactivityTime();
                    _logger.LogTrace("User has been inactive for {seconds}", inactivityS);
                    return inactivityS;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Unable to get inactivity time", ex);
                    throw ex;
                }
            }

        }

        /// <summary>
        /// </summary>
        /// <returns>A list of usernames</returns>
        public IEnumerable<string> GetAllLoggedInUsers()
        {
            using (_logger.BeginScope(nameof(GetAllLoggedInUsers)))
            {
                try
                {
                    var loggedInUsers = _userAPI.GetUsersLoggedIn();
                    return loggedInUsers;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Unable to retrieve all logged in users", ex);
                    throw ex;
                }
            }
        }
    }
}