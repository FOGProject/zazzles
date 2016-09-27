/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2016 FOG Project
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SuperWebSocket;
using WebSocket4Net;
using Zazzles.BusComponents;
using Zazzles.DataContracts;

namespace Zazzles
{
    /// <summary>
    ///     An event driven IPC interface. Can also be used to send event only within the running process.
    /// </summary>
    public static class Bus
    {
        /// <summary>
        ///     Channels are used to categorize events.
        /// </summary>
        public enum Channel
        {
            Debug,
            Power,
            PowerRequest,
            Log,
            Notification,
            Status,
            Update,
        }

        /// <summary>
        ///     Protected channels cannot be globally emmited on by clients
        /// </summary>
        private static readonly ReadOnlyCollection<Channel> ProtectedChannels = 
            new ReadOnlyCollection<Channel>(new[]
            {
                Channel.Power,
                Channel.Status,
                Channel.Update,
            });
         
        /// <summary>
        ///     The role of this bus instance. This is only needed for IPC.
        /// </summary>
        public enum Role
        {
            Server,
            Client
        }

        private const string LogName = "Bus";
        private const int Port = 1277;
        private static bool _initialized;
        private static BusServer _server;
        private static BusClient _client;
        private static Role _mode = Role.Client;

        private static readonly Dictionary<Channel, LinkedList<Action<JObject>>> Registrar =
            new Dictionary<Channel, LinkedList<Action<JObject>>>();

        public static Role Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                Dispose();
                _mode = value;
                Initializesocket();
            }
        }

        /// <summary>
        ///     Initiate the socket that connects to all other FOG bus instances
        ///     It MUST be assumed that this socket is compromised
        ///     Do NOT send security relevant data across it
        /// </summary>
        /// <returns></returns>
        private static void Initializesocket()
        {
            try
            {
                switch (_mode)
                {
                    case Role.Server:
                        _server = new BusServer();
                        _server.Socket.NewMessageReceived += socket_RecieveMessage;
                        _server.Socket.NewSessionConnected += client_connect;
                        _server.Start();
                        Log.Entry(LogName, "Became bus server");
                        _initialized = true;
                        break;
                    case Role.Client:
                        _client = new BusClient(Port);
                        _client.Socket.MessageReceived += socket_RecieveMessage;
                        _client.Start();
                        Log.Entry(LogName, "Became bus client");
                        _initialized = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not enter socket");
                Log.Error(LogName, ex);
            }
        }

        /// <summary>
        ///     Send a message to other bus instances
        /// </summary>
        /// <param name="msg">The message to send, should be in json format</param>
        private static void SendMessage(string msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            if (!_initialized)
                Initializesocket();
            if (!_initialized)
                return;

            switch (Mode)
            {
                case Role.Server:
                    _server?.Send(msg);
                    break;
                case Role.Client:
                    _client?.Send(msg);
                    break;
            }
        }

        /// <summary>
        ///     Emit a message to all listeners
        /// </summary>
        /// <param name="channel">The channel to emit on</param>
        /// <param name="data">The data to send</param>
        /// <param name="global">Should the data be sent to other processes</param>
        public static void Emit(Channel channel, object data, bool global = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Emit(channel, JObject.FromObject(data), global);
        }

        /// <summary>
        ///     Emit a message to all listeners
        /// </summary>
        /// <param name="channel">The channel to emit on</param>
        /// <param name="data">The data to send</param>
        /// <param name="global">Should the data be sent to other processes</param>
        private static void Emit(Channel channel, JObject data, bool global = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // If the emission is global, wrap the data in a BusTransport object
            // and emit the wrapper
            if (global)
            {
                var transport = new BusTransport()
                {
                    Self = true, Channel = channel, Data = data
                };

                var transportJSON = JObject.FromObject(transport).ToString();

                if (channel != Channel.Log)
                    Log.Debug(LogName, transportJSON);

                SendMessage(transportJSON);

                // If this bus instance is a client, wait for the event to be bounced-back before processing
                if (Mode == Role.Client)
                    return;
            }

            if (channel != Channel.Log)
                Log.Entry(LogName, $"Emmiting message on channel: {channel}");

            if (!Registrar.ContainsKey(channel))
                return;

            foreach (var action in Registrar[channel])
                Task.Factory.StartNew(() => action(data));
        }

        /// <summary>
        ///     Register an action with a channel. When a message is recieved on this channel, 
        ///     the method will be called.
        /// </summary>
        /// <param name="channel">The channel to register within</param>
        /// <param name="action">The action (method) to register</param>
        public static void Subscribe(Channel channel, Action<JObject> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Log.Entry(LogName, $"Registering {action.Method.Name} in channel {channel}");

            if (!Registrar.ContainsKey(channel))
                Registrar.Add(channel, new LinkedList<Action<JObject>>());
            if (Registrar[channel].Contains(action)) return;

            Registrar[channel].AddLast(action);
        }

        /// <summary>
        ///     Unregister an action from a channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="action"></param>
        public static void Unsubscribe(Channel channel, Action<JObject> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Log.Entry(LogName, $"UnRegistering {action.Method.Name} in channel {channel}");

            if (!Registrar.ContainsKey(channel)) return;
            Registrar[channel].Remove(action);
        }

        private static void client_connect(WebSocketSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
        }

        /// <summary>
        ///     Called when the server socket recieves a message
        ///     It will replay the message to all other instances, including the original sender unless told otherwise
        /// </summary>
        private static void socket_RecieveMessage(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            EmitMessageFromSocket(messageReceivedEventArgs.Message);
        }

        /// <summary>
        ///     Called when the socket client recieves a message
        /// </summary>
        private static void socket_RecieveMessage(WebSocketSession session, string value)
        {
            EmitMessageFromSocket(value);
        }

        /// <summary>
        ///     Parse a message recieved in the socket and emit it to channels confined in its instance
        /// </summary>
        /// <param name="message"></param>
        private static void EmitMessageFromSocket(string message)
        {
            try
            {
                var transport = JObject.Parse(message).ToObject<BusTransport>();
                transport.Self = false;

                if (Mode == Role.Server && ProtectedChannels.Contains(transport.Channel))
                    return;

                Emit(Channel.Debug, transport.Data.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not parse message from socket");
                Log.Error(LogName, ex);
            }
        }

        public static void Dispose()
        {
            if (!_initialized) return;

            switch (Mode)
            {
                case Role.Client:
                    _client.Stop();
                    _client = null;
                    break;
                case Role.Server:
                    _server.Stop();
                    _server = null;
                    break;
            }
        }
    }
}