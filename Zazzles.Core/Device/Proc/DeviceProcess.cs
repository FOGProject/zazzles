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


 
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Zazzles.Core.Device.Proc
{
    public class DeviceProcess
    {
        private readonly ILogger _logger;
        private readonly IProcess _processAPI;

        public DeviceProcess(ILogger<DeviceProcess> logger, IProcess processAPI)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processAPI = processAPI ?? throw new ArgumentNullException(nameof(processAPI));
        }

        public bool Kill(string name, bool all = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            return _processAPI.Kill(name, all);
        }

        Process ImpersonateProcess(string file, string arguments, string user, string password)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user));

            return _processAPI.ImpersonateProcess(file, arguments ?? string.Empty, user, password ?? string.Empty);
        }

        Process PipeProcessToDisplay(string file, string arguments)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file));

            return _processAPI.PipeProcessToDisplay(file, arguments ?? string.Empty);
        }
    }
}