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
        private ILogger _logger;
        private IIPCAgent _ipcAgent;
        private Hub _hub;

        public Bus(IIPCAgent ipcAgent, ILogger<Bus> logger)
        {
            _logger = logger;

            using (_logger.BeginScope(nameof(Bus)))
            {
                _logger.LogTrace("Initializing pub/sub hub");
                _hub = new Hub();

                if (_ipcAgent != null)
                {
                    _logger.LogTrace("IPC agent defined, attaching to hub");
                    _ipcAgent = ipcAgent;

                    _ipcAgent.OnMessage += onIPCReceive;

                    _logger.LogTrace("Connecting IPC agent");
                    _ipcAgent.Connect();

                } else
                {
                    _logger.LogTrace("No ipc agent defined");
                }
            }

        }

        /// <summary>
        ///  When a BusMessage is published via the hub, relay it via the IPC agent
        /// </summary>
        /// <param name="msg">The message to relay via IPC agent</param>
        private void relayMessageToIPC(MessageWrapper msg)
        {
            using (_logger.BeginScope(nameof(relayMessageToIPC)))
            {
                _logger.LogTrace("Message received");
                // Only broadcast messages marked for global
                if (msg.Scope != MessageScope.Global)
                {
                    _logger.LogTrace("MessageScope not marked for global, skipping");
                    return;
                }

                // Only broadcast messages we originated
                if (msg.Origin != MessageOrigin.Self)
                {
                    _logger.LogTrace("Message did not originate from this process, skipping");
                    return;
                }

                _logger.LogTrace("Seding message via IPC agent");
                _ipcAgent.Send(msg);
            }
        }

        /// <summary>
        ///     Parse a message received remotely and emit it
        /// </summary>
        /// <param name="message"></param>
        private void onIPCReceive(object sender, MessageWrapper msg)
        {
            using (_logger.BeginScope(nameof(onIPCReceive)))
            {
                // re-set the origin incase it was somehow altered
                // TODO: Look at making the field read-only
                msg.Origin = MessageOrigin.Remote;

                msg.ReceiveTimestamp = DateTime.UtcNow;
                _logger.LogTrace("Received message via IPC agent, message sent at '{sent}', and received at '{received}'",
                    msg.SentTimestamp, msg.ReceiveTimestamp);

                Publish(msg);
            }
        }

        /// <summary>
        ///     Emit a message to all listeners
        /// </summary>
        /// <param name="data">The object to serialize and send</param>
        /// <param name="scope">Where to send the message (e.g. only local or other processes)</param>
        public void Publish<T>(T data, MessageScope scope)
        {
            var msg = new MessageWrapper(data, scope, MessageOrigin.Self);
            Publish(msg);
            relayMessageToIPC(msg);
        }

        private void Publish(MessageWrapper msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            using (_logger.BeginScope(nameof(Publish)))
            {
                _logger.LogTrace("Publishing message with scope '{scope}', origin '{origin}', and timestamp of '{timestamp}'",
                    msg.Scope, msg.Origin, msg.SentTimestamp);

                _hub.Publish(msg.Payload);
            }
        }


        /// <summary>
        ///     Subscribe an action
        /// </summary>
        /// <param name="action"></param>
        public void Subscribe<T>(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (_logger.BeginScope(nameof(Unsubscribe)))
            {
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