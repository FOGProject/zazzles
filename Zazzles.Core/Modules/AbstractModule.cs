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
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub;
using Zazzles.Core.Settings;

namespace Zazzles.Core.Modules
{
    /// <summary>
    ///     The base of all FOG Modules
    /// </summary>
    public abstract class AbstractModule : IDisposable
    {
        protected readonly ILogger _logger;
        protected readonly Bus _bus;

        protected AbstractModule(ILogger<AbstractModule> logger, Bus bus, OSType compatibility)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            //if (!Settings.IsCompatible(Compatiblity))
            //{
            //    Log.Entry(Name, "Module is not compatible with " + Settings.OS);
            //    return;
            //}
            throw new NotImplementedException();
        }

        public abstract void Dispose();
    }
}