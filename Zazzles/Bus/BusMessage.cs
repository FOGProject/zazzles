/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2023 FOG Project
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zazzles
{
    public class BusMessage
    {
        /// <summary>
        /// Who the message will be sent to
        /// </summary>
        public enum MessageScope
        {
            Local,
            Server,
            Individual,
            Global
        }
        
        [JsonProperty(Required = Required.Always)]
        public Bus.Channel Channel { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public JObject Data { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string Origin { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public MessageScope Scope { get; private set; }

        public BusMessage(Bus.Channel channel, JObject data, MessageScope scope)
        {
            Data = data;
            Channel = channel;
            Scope = scope;
            Origin = "5";
        }
    }
}
