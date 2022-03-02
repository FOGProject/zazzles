/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2022 FOG Project
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
using System.Threading;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace Zazzles.BusComponents
{
    internal class BusClient
    {
        private const string LogName = "Bus::Client";
        private const int RetryTime = 30*1000;
        private ManualResetEvent AutoRetryEvent;
        private Thread AutoRetryThread;
        public WebSocket Socket { get; }

        public BusClient(int port)
        {
            AutoRetryEvent = new ManualResetEvent(false);
            AutoRetryThread = new Thread(RetrySocketConnection) {IsBackground = true};

            Socket = new WebSocket("ws://127.0.0.1:" + port + "/");
            Socket.Error += SocketOnError;
            Socket.Closed += SocketOnClosed;
            Socket.Opened += SocketOnOpened;

            AutoRetryThread.Start();
        }

        private void RetrySocketConnection()
        {
            while (true)
            {
                AutoRetryEvent.WaitOne();

                Thread.Sleep(RetryTime);
                if(!IsRetrying())
                    continue;

                try
                {
                    Socket.Open();
                }
                catch (Exception ex)
                {
                    Log.Error(LogName, "Could not reconnect, will retry in " + RetryTime/1000 + " seconds");
                    Log.Error(LogName, ex);

                }
            }
        }

        private void SocketOnOpened(object sender, EventArgs eventArgs)
        {
            Log.Entry(LogName, "Connection established");
            AutoRetryEvent.Reset();
        }

        private void SocketOnClosed(object sender, EventArgs eventArgs)
        {
            if (Socket.State == WebSocketState.Open || IsRetrying()) return;

            Log.Error(LogName, "Connection lost, socket was closed");
            AutoRetryEvent.Set();
        }

        private void SocketOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            Log.Error(LogName, errorEventArgs.Exception.Message);
            if (Socket.State == WebSocketState.Open || IsRetrying()) return;

            Log.Error(LogName, "Connection lost due to socket error");
            AutoRetryEvent.Set();
        }

        public bool Start()
        {
            try
            {
                Socket.Open();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not establish connection");
                Log.Error(LogName, ex);
                AutoRetryEvent.Set();
            }

            return true;
        }

        public bool Stop()
        {
            try
            {
                Socket.Close();
                Socket.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not stop");
                Log.Error(LogName, ex);
            }

            return false;
        }

        /// <summary>
        ///     Send a message to the bus server
        /// </summary>
        /// <param name="message">The message to emit</param>
        public void Send(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                Socket.Send(message);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not send message");
                Log.Error(LogName, ex);
            }
        }

        private bool IsRetrying()
        {
            return AutoRetryEvent.WaitOne(0); 
        }
    }
}