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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Zazzles.Middleware
{
    /// <summary>
    ///     Contains the information that the FOG Server responds with
    /// </summary>
    public class Response
    {
        private const string LogName = "Middleware::Response";
        public const string SuccessCode = "#!ok";

        public static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            {SuccessCode, "Success"},
            {"#!db", "Database error"},
            {"#!im", "Invalid MAC address format"},
            {"#!ihc", "Invalid host certificate"},
            {"#!ih", "Invalid host"},
            {"#!il", "Invalid login"},
            {"#!it", "Invalid task"},
            {"#!nvp", "Invalid Printer"},
            {"#!ng", "Module is disabled globally on the FOG server"},
            {"#!nh", "Module is disabled on the host"},
            {"#!um", "Unknown module ID"},
            {"#!ns", "No snapins"},
            {"#!nj", "No jobs"},
            {"#!np", "No Printers"},
            {"#!na", "No actions"},
            {"#!nf", "No updates"},
            {"#!time", "Invalid time"},
            {"#!ist", "Invalid security token"},
            {"#!er", "General error"}
        };

        public Response(string rawData, bool encrypted)
        {
            Encrypted = encrypted;
            try
            {
                Data = JObject.Parse(rawData);
                ReturnCode = GetField("code");
                Error = !ReturnCode.ToLower().Equals(SuccessCode);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not parse data");
                Log.Error(LogName, ex);
            }
        }

        public Response(JObject data, bool encrypted)
        {
            Data = data;
            Encrypted = encrypted;
            ReturnCode = GetField("code");
            Error = !ReturnCode.ToLower().Equals(SuccessCode);
        }

        public Response(bool error, JObject data, string returnCode, bool encrypted)
        {
            Error = error;
            Data = data;
            ReturnCode = returnCode;
            Encrypted = encrypted;
        }

        public Response()
        {
            Error = true;
            Data = new JObject();
            ReturnCode = "";
            Encrypted = false;
        }

        public bool Error { get; set; }
        public bool Encrypted { get; private set; }
        public JObject Data { get; set; }
        public string ReturnCode { get; set; }

        /// <summary>
        ///     Return the value stored at a specified key
        /// </summary>
        /// <param name="id">The ID to return</param>
        /// <returns>The value stored at key ID, if the ID is not present, return null</returns>
        public string GetField(string id)
        {
            return Data[id] != null ? Data[id].ToString() : "";
        }

        /// <summary>
        ///     Check if a field is not null or empty
        /// </summary>
        /// <param name="id">The field to check</param>
        /// <returns></returns>
        public bool IsFieldValid(string id)
        {
            return !string.IsNullOrEmpty(GetField(id));
        }

        public Response GetSubResponse(string id)
        {
            var jEntry = Data[id];
            var entry = jEntry.ToObject<JObject>();
            return new Response(entry, Encrypted);
        }

        /// <summary>
        ///     Print out all ids and values
        /// </summary>
        public void PrettyPrint()
        {
            Log.Entry(LogName, "Printing values...");
            foreach (var key in Data.Values())
                Log.Entry(LogName, "--> " + key + " = " + Data[key]);
        }
    }
}