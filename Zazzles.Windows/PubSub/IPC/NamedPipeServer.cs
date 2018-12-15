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
