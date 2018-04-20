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

// ReSharper disable InconsistentNaming

using Microsoft.Win32;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Zazzles.Middleware
{

    public enum NetAdapterType
    {
        All,
        Native,
        USB,
        NonVirtual,
        Virtual
    }
    public static class Configuration
    {
        private const string LogName = "Middleware::Configuration";

        static Configuration()
        {
            ServerAddress = string.Empty;
            GetAndSetServerAddress();
            updateMacs();
        }

        public static string ServerAddress { get; set; }
        public static string TestMAC { get; set; }

        private static string _nativeMacs;
        private static string _usbMacs;
        private static string _virtualMacs;
        private const string MAC_DELIMITER = "|";

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

        private static void updateMacs()
        {
            Log.Header("MAC ANALYSIS");
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                var aggregateNative = new StringBuilder();
                var aggregateUSB = new StringBuilder();
                var aggregateVirtual = new StringBuilder();

                foreach (var nic in adapters)
                {

                   // Log.Entry("Config", $"Looking at NIC {nic.Name}");
                   // Log.Entry("Config", $"--> type:      {nic.NetworkInterfaceType.ToString()}");
                   // Log.Entry("Config", $"--> id:        {nic.Id}");
                   // Log.Entry("Config", $"--> desc:      {nic.Description}");

                    // Only consider relevant adapter types
                    switch (nic.NetworkInterfaceType)
                    {
                        case NetworkInterfaceType.Ethernet:
                        case NetworkInterfaceType.Wireless80211:
                        case NetworkInterfaceType.Ethernet3Megabit:
                        case NetworkInterfaceType.FastEthernetFx:
                        case NetworkInterfaceType.FastEthernetT:
                        case NetworkInterfaceType.GigabitEthernet:
                            break;
                        default:
                            continue;
                    }

                    // Skip the interface if it has no physical address
                    if (string.IsNullOrWhiteSpace(nic.GetPhysicalAddress().ToString()))
                    {
                        continue;
                    }

                    // Construct the mac address string with ':' between hext digits
                    var address = string.Join(":", (from z in nic.GetPhysicalAddress().GetAddressBytes() select z.ToString("X2")).ToArray());

                    var adapterHardware = getNetInterfaceType(nic);
                //    Log.Entry("Config", "--> hardware: " + adapterHardware.ToString());

                    var output = aggregateNative;
                    if (adapterHardware == NetAdapterType.USB)
                        output = aggregateUSB;
                    else if (adapterHardware == NetAdapterType.Virtual)
                        output = aggregateVirtual;

                    if (output.Length != 0)
                    {
                        output.Append(MAC_DELIMITER);
                    }
                    output.Append(address);
                }

                _virtualMacs = aggregateVirtual.ToString();
                _usbMacs = aggregateUSB.ToString();
                _nativeMacs = aggregateNative.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not get MAC addresses");
                Log.Error(LogName, ex);
            }
        }

        private static bool isNetInterfacePhysical_powershell(NetworkInterface net)
        {
            if (Settings.OS != Settings.OSType.Windows)
                return true;

            // Try the powershell approach first, if failed then use the registry approach
            var command = $"Get-NetAdapter -Physical -Name \"{net.Name}\"";
            var isPhysical = (ProcessHandler.Run("powershell.exe", $"-Command \"{command}\"") == 0);


            return isPhysical;
        }

        private static NetAdapterType getNetInterfaceType(NetworkInterface net)
        {
            if (Settings.OS != Settings.OSType.Windows)
                return NetAdapterType.Native;

            const string netControl = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
            const string instanceIdKey = "NetCfgInstanceId";
            const string deviceInstKey = "DeviceInstanceID";
            var instanceId = net.Id;

            try
            {
                var key = Registry.LocalMachine.OpenSubKey(netControl);
                if (key == null)
                    throw new NullReferenceException("Null key");

                var cfgs = key.GetSubKeyNames();
                key.Close();

                foreach (var cfg in cfgs)
                {
                    var cfgPath = $"{netControl}\\{cfg}";

                    var cfgId = RegistryHandler.GetRegisitryValue(cfgPath, instanceIdKey);
                    if (cfgId != instanceId)
                        continue;

                    var deviceInstanceId = RegistryHandler.GetRegisitryValue(cfgPath, deviceInstKey).ToLower();
                    if (deviceInstanceId.StartsWith("pci"))
                    {
                        return NetAdapterType.Native;
                    }
                    else if (deviceInstanceId.StartsWith("usb"))
                    {
                        return NetAdapterType.USB;
                    }
                    else
                    {
                        return NetAdapterType.Virtual;
                    }
                }


            }
            catch (Exception ex)
            {
                Log.Error(LogName, ex);
            }


            return NetAdapterType.Native;
        }

        /// <summary>
        ///     Get a string of all the host's valid MAC addresses
        /// </summary>
        /// <returns>A string of all the host's valid MAC addresses, split by |</returns>
        public static string MACAddresses(NetAdapterType hardware = NetAdapterType.All)
        {
            if (!string.IsNullOrEmpty(TestMAC))
                return TestMAC;

            switch (hardware)
            {
                case NetAdapterType.Native:
                    return _nativeMacs;
                case NetAdapterType.USB:
                    return _usbMacs;
                case NetAdapterType.NonVirtual:
                    return joinMacLists(_nativeMacs, _usbMacs);
                case NetAdapterType.Virtual:
                    return _virtualMacs;
                default:
                    return joinMacLists(joinMacLists(_nativeMacs, _usbMacs), _virtualMacs);
            }
        }

        private static string joinMacLists(string a, string b)
        {
            if (a.Length == 0)
            {
                return b;
            }
            else if (b.Length == 0)
            {
                return a;
            }
            else
            {
                return a + MAC_DELIMITER + b;
            }
        }
    }
}