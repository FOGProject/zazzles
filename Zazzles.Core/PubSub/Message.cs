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

namespace Zazzles.Core.PubSub
{
    public class Message<T>
    {
        public MetaData MetaData { get; set; }
        public T Payload { get; set; }

        public Message(T payload, MessageScope scope, MessageOrigin origin)
        {
            Payload = payload;
            MetaData = new MetaData(scope, origin);
        }

        public Message(T payload, MetaData metaData)
        {
            Payload = payload;
            MetaData = metaData;
        }

        public override string ToString()
        {
            return $"{MetaData.ToString() ?? string.Empty}, {Payload.ToString() ?? string.Empty}";
        }
    }
}
