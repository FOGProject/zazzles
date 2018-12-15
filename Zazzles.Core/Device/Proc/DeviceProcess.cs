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

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Security;

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

        Process ImpersonateUserProcess(
            ProcessStartInfo startInfo,
            SecureString password = null)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));
            if (string.IsNullOrWhiteSpace(startInfo.UserName))
                throw new ArgumentNullException(nameof(startInfo.UserName));

            return _processAPI.ImpersonateUserProcess(startInfo, password);
        }

        Process CreateInteractiveProcess(ProcessStartInfo startInfo)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            return _processAPI.CreateInteractiveProcess(startInfo);
        }
    }
}