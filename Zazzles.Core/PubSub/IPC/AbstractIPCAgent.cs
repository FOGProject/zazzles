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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace Zazzles.Core.PubSub.IPC
{
    public abstract class AbstractIPCAgent : IDisposable
    {
        protected readonly ILogger _logger;
        protected readonly IParser _parser;
        private readonly Dictionary<Type, Action<IParser, Transport>> _typeCasters;
        private readonly object casterLock = new object();
        private readonly Policy _retryPolicy;


        public abstract Task<bool> Connect();
        public abstract Task<bool> Disconnect();
        protected abstract Task<bool> Send(byte[] msg);
        public abstract void Dispose();

        public AbstractIPCAgent(ILogger<AbstractIPCAgent> logger, IParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _typeCasters = new Dictionary<Type, Action<IParser, Transport>>();

            _retryPolicy = Policy
                .Handle<RetryableException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(8)
                });
        }

        public async Task<bool> Send<T>(Message<T> msg) where T : class
        {
            var payload = await _parser.Serialize(msg.Payload);
            var transport = new Transport(typeof(T), payload, msg.MetaData);

            var serTransport = await _parser.Serialize(transport);

            return await Send(serTransport);
        }

        protected async Task<bool> OnReceive(byte[] message)
        {
            using (_logger.BeginScope(nameof(OnReceive)))
            {
                try
                {
                    _logger.LogTrace("Received IPC message");
                    var transport = await _parser.Deserialize<Transport>(message);
                    Type type = transport.PayloadType;
                    _logger.LogTrace("Message type: '{type}'", type);
                    Action<IParser, Transport> caster = null;

                    lock (casterLock)
                    {
                        if (!_typeCasters.ContainsKey(type)) return false;
                        _logger.LogTrace("Typecaster found");

                        caster = _typeCasters[type];
                    }

                    if (caster == null) return false;
                    caster(_parser, transport);
                    return true;
                }
                // Catch all exceptions to prevent malicious messages from crashing the process
                catch (Exception ex)
                {
                    _logger.LogError("Failed to cast IPC message", ex);
                }

                return false;
            }

        }

        public bool RegisterTypeCaster(Type type, Action<IParser, Transport> caster)
        {
            lock(casterLock)
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
}
