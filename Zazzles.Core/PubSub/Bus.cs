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
using PubSub;
using Zazzles.Core.PubSub.IPC;

namespace Zazzles.Core.PubSub
{
    public enum MessageScope
    {
        Local,
        Global
    }

    public enum MessageOrigin
    {
        Self,
        Remote
    }

    /// <summary>
    ///     A pub/sub that also has IPC support. This enables system-wide events to be published
    /// </summary>
    /// 
    public class Bus : IDisposable
    {
        private readonly ILogger _logger;
        private readonly AbstractIPCAgent _ipcAgent;
        private readonly Hub _hub;

        public Bus(ILogger<Bus> logger, AbstractIPCAgent ipcAgent = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            using (_logger.BeginScope(nameof(Bus)))
            {
                _logger.LogTrace("Initializing pub/sub hub");
                _hub = new Hub();

                if (ipcAgent != null)
                {
                    _logger.LogTrace("IPC agent defined, attaching to hub");
                    _ipcAgent = ipcAgent;

                    _logger.LogTrace("Connecting IPC agent");
                    _ipcAgent.Connect();

                }
                else
                {
                    _logger.LogTrace("No ipc agent defined");
                }
            }

        }

        /// <summary>
        ///  When a BusMessage is published via the hub, relay it via the IPC agent
        /// </summary>
        /// <param name="msg">The message to relay via IPC agent</param>
        private void relayMessageToIPC<T>(Message<T> msg) where T : class
        {
            using (_logger.BeginScope(nameof(relayMessageToIPC)))
            {
                _logger.LogTrace("Message received");
                // Only broadcast messages marked for global
                if (msg.MetaData.Scope != MessageScope.Global)
                {
                    _logger.LogTrace("MessageScope not marked for global, skipping");
                    return;
                }

                // Only broadcast messages we originated
                if (msg.MetaData.Origin != MessageOrigin.Self)
                {
                    _logger.LogTrace("Message did not originate from this process, skipping");
                    return;
                }

                _logger.LogTrace("Sending message via IPC agent");
                _ipcAgent.Send(msg);
            }
        }

        /// <summary>
        ///     Emit a message to all listeners
        /// </summary>
        /// <param name="data">The object to serialize and send</param>
        /// <param name="scope">Where to send the message (e.g. only local or other processes)</param>
        public void Publish<T>(T data, MessageScope scope) where T : class
        {
            var msg = new Message<T>(data, scope, MessageOrigin.Self);
            Publish(msg);
            relayMessageToIPC(msg);
        }

        private void Publish<T>(Message<T> msg) where T : class
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            using (_logger.BeginScope(nameof(Publish)))
            {
                _logger.LogTrace("Publishing message with scope '{scope}', origin '{origin}', and timestamp of '{timestamp}'",
                    msg.MetaData.Scope, msg.MetaData.Origin, msg.MetaData.SentTimestamp);

                _hub.Publish(msg);
            }
        }

        private bool RegisterTypeCaster<T>() where T : class
        {
            if (_ipcAgent == null) return false;

            using (_logger.BeginScope(nameof(RegisterTypeCaster)))
            {
                var type = typeof(T);
                _logger.LogTrace("Creating caster for type '{type}'", type);
                var caster = new Action<IParser, Transport>((parser, transport) =>
                {
                    try
                    {
                        transport.MetaData.Origin = MessageOrigin.Remote;
                        transport.MetaData.Scope = MessageScope.Global;
                        transport.MetaData.ReceiveTimestamp = DateTime.UtcNow;
                        _logger.LogTrace("Received message via IPC agent, message sent at '{sent}', and received at '{received}'",
                            transport.MetaData.SentTimestamp, transport.MetaData.ReceiveTimestamp);


                        var payload = parser.Deserialize<T>(transport.Payload);
                        var msg = new Message<T>(payload, transport.MetaData);
                        Publish(msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to deserialize '{payload}' of type '{type}'", 
                            ex, 
                            transport.Payload, type);
                    }

                });
                return _ipcAgent.RegisterTypeCaster(type, caster);
            }
        }


        /// <summary>
        ///     Subscribe an action
        /// </summary>
        /// <param name="action"></param>
        public void Subscribe<T>(Action<Message<T>> action) where T : class
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (_logger.BeginScope(nameof(Unsubscribe)))
            {
                _logger.LogTrace("Registering type caster");
                RegisterTypeCaster<T>();

                _logger.LogTrace("Subscribing '{name}' with message type '{mtype}'",
                    nameof(action), nameof(T));

                _hub.Subscribe(action);
            }
        }

        /// <summary>
        ///     Unsubscribe an action
        /// </summary>
        /// <param name="action"></param>
        public void Unsubscribe<T>(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (_logger.BeginScope(nameof(Unsubscribe)))
            {
                _logger.LogTrace("Unsubscribing '{name}' with message type '{mtype}'",
                    nameof(action), nameof(T));

                _hub.Unsubscribe(action);
            }
        }

        public void Dispose()
        {
            using (_logger.BeginScope(nameof(Dispose)))
            {
                if (_ipcAgent == null)
                {
                    _logger.LogTrace("IPC Agent not set");
                    return;
                }

                _logger.LogTrace("Disposing IPC Agent");
                _ipcAgent.Dispose();
            }
        }
    }
}