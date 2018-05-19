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


using System;
using System.Runtime.Serialization;

namespace Zazzles.Core.PubSub
{
    [DataContract]
    public class MetaData
    {
        [IgnoreDataMember]
        public MessageOrigin Origin { get; set; }
        [IgnoreDataMember]
        public DateTime ReceiveTimestamp { get; set; }

        [DataMember(Name = "timestamp", IsRequired = true)]
        public DateTime SentTimestamp { get; set; }

        [DataMember(Name = "scope", IsRequired = true)]
        public MessageScope Scope { get; set; }

        public MetaData()
        {

        }

        public MetaData(MessageScope scope, MessageOrigin origin = MessageOrigin.Remote)
        {
            Scope = scope;
            SentTimestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{Origin}, {Scope}, {ReceiveTimestamp}, {SentTimestamp}";
        }
    }
}
