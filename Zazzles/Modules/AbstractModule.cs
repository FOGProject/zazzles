/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2016 FOG Project
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
using Newtonsoft.Json.Linq;

namespace Zazzles.Modules
{
    /// <summary>
    ///     The base of all FOG Modules
    /// </summary>
    public abstract class AbstractModule<TMessageContainer> : IEventProcessor
    {
        public abstract string Name { get; protected set; }
        public abstract Settings.OSType Compatiblity { get; protected set; }
        public abstract EventProcessorType Type { get; protected set; }

        public virtual void ProcessEvent(JObject data)
        {
            if (!Settings.IsCompatible(Compatiblity))
                throw new Exception($"{Name} is not compatible with {Settings.OS}");

            var message = data.ToObject<TMessageContainer>();
            OnEvent(message);
        }

        public EventProcessorType GetEventProcessorType()
        {
            return Type;
        }

        protected abstract void OnEvent(TMessageContainer message);
    }
}