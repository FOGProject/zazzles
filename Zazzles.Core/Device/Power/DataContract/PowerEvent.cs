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

namespace Zazzles.Core.Device.Power.DataContract
{
    [DataContract(Name="PowerEvent")]
    public class PowerEvent
    {
        [DataMember(Name = "action", IsRequired = true)]
        public PowerAction Action { get; set; }

        [DataMember(Name = "comment", IsRequired = true)]
        public string Comment { get; set; }

        [DataMember(Name = "options", IsRequired = true)]
        public UserOptions Options { get; set; }

        [DataMember(Name = "atTime", IsRequired = true)]
        public DateTime AtTime { get; set; }


        public PowerEvent()
        {

        }

        public PowerEvent(
            PowerAction action,
            UserOptions options,
            DateTime at,
            string comment = "")
        {
            Action = action;
            Options = options;
            AtTime = at;
            Comment = comment;
        }

        public override string ToString()
        {
            return $"{Action}, {AtTime}, {Options}, {Comment ?? string.Empty}";
        }
    }
}
