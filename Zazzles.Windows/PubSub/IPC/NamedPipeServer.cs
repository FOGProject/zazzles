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
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using Zazzles.Core.PubSub.IPC;

namespace Zazzles.Windows.PubSub.IPC
{
    class NamedPipeServer : AbstractIPCAgent
    {
        NamedPipeServer<byte[]> server;
        public NamedPipeServer(string pipe, ILogger<AbstractIPCAgent> logger, IParser parser) : base(logger, parser)
        {
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid | WellKnownSidType.AccountDomainUsersSid, null), 
                PipeAccessRights.ReadWrite, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null),
                PipeAccessRights.FullControl, AccessControlType.Allow));

            server = new NamedPipeServer<byte[]>(pipe, security);
            server.ClientMessage += ((conn, msg) =>
            {
                OnReceive(msg);
            });
        }

        public override bool Connect()
        {
            server.Start();
            return true;
        }

        public override bool Disconnect()
        {
            server.Stop();
            return true;
        }

        protected override bool Send(byte[] msg)
        {
            server.PushMessage(msg);
            return true;
        }

        public override void Dispose()
        {
            server.Stop();
        }

    }
}
