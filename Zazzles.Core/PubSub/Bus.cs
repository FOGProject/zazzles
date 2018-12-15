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
using System.Threading.Tasks;
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
        private async Task relayMessageToIPC<T>(Message<T> msg) where T : class
        {
            using (_logger.BeginScope(nameof(relayMessageToIPC)))
            {
                _logger.LogTrace("Message received");
                // Only broadcast messages marked for global
                if (msg.MetaData.Scope != MessageScope.Global)
                {
                    _logger.LogTrace(
                        "MessageScope not marked for global, skipping");
                    return;
                }

                // Only broadcast messages we originated
                if (msg.MetaData.Origin != MessageOrigin.Self)
                {
                    _logger.LogTrace(
                        "Message did not originate from this process, skipping");
                    return;
                }

                _logger.LogTrace("Sending message via IPC agent");
                await _ipcAgent.Send(msg);
            }
        }

        /// <summary>
        ///     Emit a message to all listeners
        /// </summary>
        /// <param name="data">The object to serialize and send</param>
        /// <param name="scope">Where to send the message (e.g. only local or other processes)</param>
        public async Task Publish<T>(T data, MessageScope scope) where T : class
        {
            var msg = new Message<T>(data, scope, MessageOrigin.Self);
            Publish(msg);
            await relayMessageToIPC(msg);
        }

        private void Publish<T>(Message<T> msg) where T : class
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            using (_logger.BeginScope(nameof(Publish)))
            {
                _logger.LogTrace(
                    "Publishing message with metadata '{metadata'}",
                    msg.MetaData);

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
                var caster = new Action<IParser, Transport>(async (parser, transport) =>
                {
                    try
                    {
                        transport.MetaData.Origin = MessageOrigin.Remote;
                        transport.MetaData.Scope = MessageScope.Global;
                        transport.MetaData.ReceiveTimestamp = DateTime.UtcNow;
                        _logger.LogTrace(
                            "Received message via IPC agent, message sent at '{sent}', and received at '{received}'",
                            transport.MetaData.SentTimestamp, transport.MetaData.ReceiveTimestamp);


                        var payload = await parser.Deserialize<T>(transport.Payload);
                        var msg = new Message<T>(payload, transport.MetaData);
                        Publish(msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "Failed to deserialize '{payload}' of type '{type}'",
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

                _logger.LogTrace(
                    "Subscribing '{name}' with message type '{mtype}'",
                    nameof(action), nameof(T));

                _hub.Subscribe(action);
            }
        }

        /// <summary>
        ///     Unsubscribe an action
        /// </summary>
        /// <param name="action"></param>
        public void Unsubscribe<T>(Action<Message<T>> action) where T : class
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (_logger.BeginScope(nameof(Unsubscribe)))
            {
                _logger.LogTrace(
                    "Unsubscribing '{name}' with message type '{mtype}'",
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