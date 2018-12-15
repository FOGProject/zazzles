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
using System.Runtime.Serialization;

namespace Zazzles.Core.PubSub.IPC
{
    [DataContract]
    public class Transport
    {
        [DataMember(Name = "metadata", IsRequired = true)]
        public MetaData MetaData { get; set; }

        [DataMember(Name = "payload", IsRequired = true)]
        public byte[] Payload { get; set; }

        [DataMember(Name = "payloadType", IsRequired = true)]
        public SerializableType PayloadType { get; set; }


        public Transport(Type type, byte[] payload, MessageScope scope, MessageOrigin origin)
        {
            PayloadType = type;
            Payload = payload;
            MetaData = new MetaData(scope, origin);
        }

        public Transport(Type type, byte[] payload, MetaData meta)
        {
            PayloadType = type;
            Payload = payload;
            MetaData = meta;
        }

        public Transport()
        {

        }
    }
}
