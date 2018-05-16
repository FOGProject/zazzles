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
using System.Collections.Generic;

namespace Zazzles.Core.PubSub.IPC
{
    public abstract class AbstractIPCAgent : IDisposable
    {
        protected readonly ILogger _logger;
        protected readonly IParser _parser;
        private readonly Dictionary<Type, Action<IParser, Transport>> _typeCasters;

        public abstract bool Connect();
        public abstract bool Disconnect();
        protected abstract bool Send(byte[] msg);
        public abstract void Dispose();

        public AbstractIPCAgent(ILogger logger, IParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _typeCasters = new Dictionary<Type, Action<IParser, Transport>>();
        }

        public bool Send<T>(Message<T> msg) where T : class
        {
            var payload = _parser.Serialize(msg.Payload);
            var transport = new Transport(typeof(T), payload, msg.MetaData);

            var serTransport = _parser.Serialize(transport);
            return Send(serTransport);
        }

        protected void OnReceive(byte[] message)
        {
            using (_logger.BeginScope(nameof(OnReceive)))
            {
                try
                {
                    _logger.LogTrace("Received IPC message");
                    var transport = _parser.Deserialize<Transport>(message);
                    Type type = transport.PayloadType;
                    _logger.LogTrace("Message type: '{type}'", type);

                    if (!_typeCasters.ContainsKey(type)) return;
                    _logger.LogTrace("Typecaster found");

                    var caster = _typeCasters[type];

                    caster(_parser, transport);
                }
                // Catch all exceptions to prevent malicious messages from crashing the process
                catch (Exception ex)
                {
                    _logger.LogError("Failed to cast IPC message", ex);
                }
            }

        }

        public bool RegisterTypeCaster(Type type, Action<IParser, Transport> caster)
        {
            if (_typeCasters == null) return false;
            if (_typeCasters.ContainsKey(type)) return false;

            using (_logger.BeginScope(nameof(RegisterTypeCaster)))
            {
                _logger.LogTrace("Creating caster for type '{type}'", type);
                _typeCasters.Add(type, caster);
            }
            return true;
        }
    }
}
