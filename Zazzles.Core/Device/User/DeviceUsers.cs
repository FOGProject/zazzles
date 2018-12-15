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
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Zazzles.Core.Device.User
{
    public class DeviceUsers
    {
        private readonly IUser _userAPI;
        private readonly ILogger _logger;

        public DeviceUsers(ILogger<DeviceUsers> logger, IUser userAPI)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userAPI = userAPI ?? throw new ArgumentNullException(nameof(userAPI));
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
                    _logger.LogWarning(
                        "Unable to calculate how many users are logged on", ex);
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
                    _logger.LogTrace(
                        "Environment.UserName set to '{username}'", username);
                }

                return username;
            }
        }

        /// <summary>
        /// Log off a specific user
        /// </summary>
        /// <returns>True if succesfull</returns>
        public bool LogOffUser(string username)
        {
            return _userAPI.LogOffUser(username);
        }

        /// <summary>
        /// Log off the current user
        /// </summary>
        /// <returns>True if succesfull</returns>
        public bool LogOffCurrentUser()
        {
            return _userAPI.LogOffUser(GetCurrentUserName());
        }

        /// <summary>
        /// </summary>
        /// <returns>The inactivity time of the current user in seconds</returns>
        public TimeSpan GetInactivityTime()
        {
            using (_logger.BeginScope(nameof(GetInactivityTime)))
            {
                try
                {
                    var inactivity = _userAPI.GetInactivityTime();
                    _logger.LogTrace(
                        "User has been inactive for {inactivity}", inactivity);
                    return inactivity;
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
                    _logger.LogWarning(
                        "Unable to retrieve all logged in users", ex);
                    throw ex;
                }
            }
        }
    }
}