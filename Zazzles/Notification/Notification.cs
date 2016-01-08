/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2015 FOG Project
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
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Zazzles
{
    public static class Notification
    {
        private static object _recordLock = new object();
        private static Dictionary<string, JObject> _onGoingList = new Dictionary<string, JObject>();
        private static object _onGoingLock = new object();

        public static JObject ToJSON(string title, string message, string subjectID)
        {
            dynamic json = new JObject();
            json.title = title;
            json.message = message;
            json.subjectID = subjectID;
            return json;
        }

        public static void Emit(string title, string message, string subjectID = "", bool onGoing = false, bool global = true)
        {
            Emit(ToJSON(title, message, subjectID), onGoing, global);
        }

        public static void Emit(JObject data, bool onGoing = false, bool global = true)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data["subjectID"] == null)
                throw new ArgumentException("A subjectID section in data must be provided!", nameof(data));

            lock (_onGoingLock)
            {
                if (_onGoingList.ContainsKey(data["subjectID"].ToString()))
                {
                    Bus.MessageQueue.Remove(_onGoingList[data["subjectID"].ToString()].ToString());
                    _onGoingList.Remove(data["subjectID"].ToString());
                }

                if (global && onGoing)
                {
                    _onGoingList.Add(data["subjectID"].ToString(), data);
                    Bus.MessageQueue.Add(data.ToString());
                }
            }

            if (global)
                Record(data);

            Bus.Emit(Bus.Channel.Notification, data, global);
        }

        public static void Record(JObject data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data["title"] == null)
                throw new ArgumentException("A title section in data must be provided!", nameof(data));

            Record(data["title"].ToString());
        }

        public static void Record(string title)
        {
            if(string.IsNullOrEmpty(title))
                throw new ArgumentException("A title must be provided!", nameof(title));

            lock (_recordLock)
            {
                var filePath = CalculateLogName();

                var logDir = Path.GetDirectoryName(filePath);

                if (logDir != null && !Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                //Write message to log file
                var logWriter = new StreamWriter(filePath, true);
                logWriter.WriteLine($"{DateTime.Now.ToShortDateString()} {title}");
                logWriter.Close();
            }
        }

        private static string CalculateLogName()
        {
            var logPath = Path.Combine(Settings.Location, "logs", DateTime.Today.ToString("yy-MM-dd"));
            return logPath;
        }
    }
}