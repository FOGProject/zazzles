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
using System.IO;
using System.Net;
using Zazzles.Middleware.Bindings;

namespace Zazzles.Middleware
{
    public static class Communication
    {
        private const string LogName = "Middleware::Communication";
        private static IServerBinding _binding;

        /// <summary>
        /// Bind server communication to RemoteRX and RemoteTX Bus channels
        /// </summary>
        /// <returns>True on success</returns>
        public static bool BindServerToBus()
        {
            _binding = new SocketIO();
            if (_binding.Bind()) return true;

            _binding = new Polling();
            return _binding.Bind();
        }

        /// <summary>
        /// UnBind server communication to RemoteRX and RemoteTX Bus channels
        /// </summary>
        /// <returns>True on success</returns>
        public static bool UnBindServerFromBus()
        {
            return _binding.UnBind();
        }

        /// <summary>
        ///     Downloads a file and creates necessary directories
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <param name="filePath">The location to save the file</param>
        /// <returns>True if the download was successful</returns>
        public static bool DownloadFile(string postfix, string filePath)
        {
            if (string.IsNullOrEmpty(postfix))
                throw new ArgumentException("A postfix must be provided!", nameof(postfix));

            return DownloadExternalFile(Configuration.ServerAddress + postfix, filePath);
        }

        /// <summary>
        ///     Download a file from an external server
        /// </summary>
        /// <param name="url">The URL to download from</param>
        /// <param name="filePath">The path to save the file to</param>
        /// <returns>True if successful</returns>
        public static bool DownloadExternalFile(string url, string filePath)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("A URL must be provided!", nameof(url));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("A file path must be provided!", nameof(filePath));

            Log.Entry(LogName, $"URL: {url}");

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }

                using (var client = new WebClient())
                {
                    client.DownloadFile(url, filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not download file");
                Log.Error(LogName, ex);
                return false;
            }

            return File.Exists(filePath);
        }
    }
}