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

// ReSharper disable InconsistentNaming

/*
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Zazzles.Middleware
{
    public static class Configuration
    {
        private const string LogName = "Middleware::Configuration";

        static Configuration()
        {
            ServerAddress = "";
            GetAndSetServerAddress();
        }

        public static string ServerAddress { get; set; }
        public static string TestMAC { get; set; }

        /// <summary>
        ///     Load the server information from the registry and apply it
        /// </summary>
        /// <returns>True if settings were updated</returns>
        public static bool GetAndSetServerAddress()
        {
            if (string.IsNullOrEmpty(Settings.Get("HTTPS")) || Settings.Get("WebRoot") == null ||
                string.IsNullOrEmpty(Settings.Get("Server")))
            {
                Log.Error(LogName, "Invalid parameters");
                return false;
            }

            ServerAddress = (Settings.Get("HTTPS").Equals("1") ? "https://" : "http://");
            ServerAddress += Settings.Get("Server") +
                             Settings.Get("WebRoot");
            return true;
        }

        /// <summary>
        ///     Get the IP address of the host
        /// </summary>
        /// <returns>The first IP address of the host</returns>
        public static string IPAddress()
        {
            var hostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(hostName);
            var address = ipEntry.AddressList;

            return (address.Length > 0) ? address[0].ToString() : "";
        }

        /// <summary>
        ///     Get a string of all the host's valid MAC addresses
        /// </summary>
        /// <returns>A string of all the host's valid MAC addresses, split by |</returns>
        public static string MACAddresses()
        {
            if (!string.IsNullOrEmpty(TestMAC)) return TestMAC;

            var macs = "";
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();

                macs = adapters.Aggregate(macs, (current, adapter) =>
                    current +
                    ("|" +
                     string.Join(":",
                         (from z in adapter.GetPhysicalAddress().GetAddressBytes() select z.ToString("X2")).ToArray())));

                macs = macs.Trim('|');
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not get MAC addresses");
                Log.Error(LogName, ex);
            }

            return macs;
        }
    }
}

*/