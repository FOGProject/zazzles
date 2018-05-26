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


using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub.IPC;

namespace Zazzles.Windows.PubSub.IPC
{
    class SocketServer : AbstractIPCAgent
    {
        private readonly Socket _server;
        private readonly UnixEndPoint _endpoint;
        private readonly int _backlog;

        public SocketServer(string sock, int backlog, ILogger<AbstractIPCAgent> logger, IParser parser) : base(logger, parser)
        {
            _server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            _endpoint = new UnixEndPoint(sock);
            _backlog = backlog;
        }

        public override bool Connect()
        {
            _server.Bind(_endpoint);
            _server.Listen(_backlog);
            return true;
        }

        public override bool Disconnect()
        {
            _server.Shutdown(SocketShutdown.Both);
            return true;
        }

        protected override bool Send(byte[] msg)
        {
            _server.Send(msg);
            return true;
        }

        public override void Dispose()
        {
            Disconnect();
            _server.Dispose();
        }

    }
}
