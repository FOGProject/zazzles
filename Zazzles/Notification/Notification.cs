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

namespace Zazzles
{
    public static class Notification
    {
        public static JObject ToJSON(string title, string message)
        {
            dynamic json = new JObject();
            json.title = title;
            json.message = message;
            return json;
        }

        public static void Emit(string title, string message, bool onGoing = false, bool global = true)
        {
            Emit(ToJSON(title, message), onGoing, global);
        }

        public static void Emit(JObject data, bool onGoing = false, bool global = true)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Bus.Emit(Bus.Channel.Notification, data, global);
        }

    }
}