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
using System.Linq;
using System.Net;
using System.Text;
using Zazzles.Middleware.Bindings;

// ReSharper disable InconsistentNaming

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
            Log.Entry(LogName, $"URL: {url}");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filePath))
            {
                Log.Error(LogName, "Invalid parameters");
                return false;
            }

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            Stream remoteStream = null;
            Stream localStream = null;
            WebResponse response = null;

            var err = false;

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }

                // Create a request for the specified remote file name
                var request = WebRequest.Create(url);
                // Send the request to the server and retrieve the
                // WebResponse object 
                response = request.GetResponse();
                {
                    // Once the WebResponse object has been retrieved,
                    // get the stream object associated with the response's data
                    remoteStream = response.GetResponseStream();

                    // Create the local file
                    localStream = File.Create(filePath);

                    // Allocate a 1k buffer
                    var buffer = new byte[1024];
                    int bytesRead;

                    // Simple do/while loop to read from stream until
                    // no bytes are returned
                    do
                    {
                        // Read data (up to 1k) from the stream
                        bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                        // Write the data to the local file
                        localStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not download file");
                Log.Error(LogName, ex);
                err = true;
            }
            finally
            {
                // Close the response and streams objects here 
                // to make sure they're closed even if an exception
                // is thrown at some point
                response?.Close();
                remoteStream?.Close();
                localStream?.Close();
            }

            return !err && File.Exists(filePath);
        }
    }
}