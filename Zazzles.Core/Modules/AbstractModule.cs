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
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub;

namespace Zazzles.Core.Modules
{
    /// <summary>
    ///     The base of all FOG Modules
    /// </summary>
    public abstract class AbstractModule : IDisposable
    {
        protected readonly ILogger _logger;
        protected readonly Bus _bus;

        protected AbstractModule(
            ILogger<AbstractModule> logger,
            Bus bus,
            OSPlatform[] compatiblePlatforms = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            // If compatiblePlatforms is null, then the module is os agnostic
            //   so return early and dont check for compatibility
            if (compatiblePlatforms == null)
                return;

            foreach (var platform in compatiblePlatforms)
                if (RuntimeInformation.IsOSPlatform(platform))
                    return;

            throw new PlatformNotSupportedException(
                "Module is not compatible with the current platform");
        }

        public abstract void Dispose();
    }
}