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
using NamedPipeWrapper;
using Zazzles.Core.PubSub.IPC;

namespace Zazzles.Windows.PubSub.IPC
{
    class NamedPipeClient : AbstractIPCAgent
    {
        NamedPipeClient<byte[]> client;
        public NamedPipeClient(string pipe, ILogger<AbstractIPCAgent> logger, IParser parser) : base(logger, parser)
        {
            client = new NamedPipeClient<byte[]>(pipe);
            client.ServerMessage += ((conn, msg) =>
            {
                OnReceive(msg);
            });
        }

        public override bool Connect()
        {
            client.Start();
            return true;
        }

        public override bool Disconnect()
        {
            client.Stop();
            return true;
        }

        protected override bool Send(byte[] msg)
        {
            client.PushMessage(msg);
            return true;
        }

        public override void Dispose()
        {
            client.Stop();
        }

    }
}
