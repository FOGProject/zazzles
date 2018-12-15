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
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub.IPC;
using Polly;
using System.Net.Http;

namespace Zazzles.Core.Middlware
{
    class HttpPollingAgent : AbstractIPCAgent
    {
        bool _authenticated;
        private readonly HttpClient _httpClient;
        private readonly Uri _endpoint;


        public HttpPollingAgent(Uri endpoint, X509Certificate[] whitelist, bool strict, ILogger<AbstractIPCAgent> logger, IParser parser)
            : base(logger, parser)
        {
            _endpoint = endpoint;
            _webclient = new WhitelistWebClient(whitelist, strict);

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(8)
                });
        }

        public HttpPollingAgent(Uri endpoint, ILogger<AbstractIPCAgent> logger, IParser parser)
            : base(logger, parser)
        {
            _endpoint = endpoint;
            _httpClient = new HttpClient();
        }

        public override bool Connect()
        {
            return true;
        }

        public override bool Disconnect()
        {
            return true;
        }

        protected override bool Send(byte[] msg)
        {
            _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _httpClient
                .DeleteAsync("https://example.com/api/products/1");
                response.EnsureSuccessStatusCode();
            });
            return true;
        }

        public override void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
