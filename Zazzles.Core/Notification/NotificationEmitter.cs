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

namespace Zazzles.Core.Notification
{
    public class NotificationEmitter
    {
        private ILogger _logger;
        private Bus _bus;

        public NotificationEmitter(Bus bus, ILogger<NotificationEmitter> logger)
        {
            _logger = logger;
            _bus = bus;
        }

        public void Emit(string title, string body, bool global = true)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrEmpty(body))
                throw new ArgumentNullException(nameof(body));

            var wrapper = new NotificationWrapper(title, body);
            var scope = (global) ? MessageScope.Global : MessageScope.Local;

            Emit(wrapper, scope);
        }

        public void Emit(NotificationWrapper wrapper, MessageScope scope)
        {
            if (wrapper == null)
                throw new ArgumentNullException(nameof(wrapper));

            using (_logger.BeginScope(nameof(Emit)))
            {
                _logger.LogTrace("Emitting message with content '{title}' - '{body}', and a scope of '{scope}'",
                    wrapper.Title, wrapper.Body, scope.ToString());

                _bus.Publish(wrapper, scope);
            }

        }

    }
}