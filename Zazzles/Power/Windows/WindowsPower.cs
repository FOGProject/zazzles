/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2021 FOG Project
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Zazzles.PowerComponents
{
    internal class WindowsPower : IPower
    {
        private const string LogName = "Power";

        public Task Shutdown(string comment, Power.ShutdownOptions options = Power.ShutdownOptions.Abort, string message = null)
        {
            return Power.QueueShutdown($"/s /c \"{comment}\" /t 0", options, message);
        }

        public Task Restart(string comment, Power.ShutdownOptions options = Power.ShutdownOptions.Abort, string message = null)
        {
            return Power.QueueShutdown($"/r /c \"{comment}\" /t 0", options, message);
        }

        public void LogOffUser()
        {
            Power.CreateTask("/l");
        }

        public void Hibernate()
        {
            Power.CreateTask("/h");
        }

        public void LockWorkStation()
        {
            lockWorkStation();
        }

        public void CreateTask(string parameters, string message)
        {
            if(string.IsNullOrWhiteSpace(message))
                message = string.Empty;

            ExitWindows action = ExitWindows.LogOff;
            if (parameters.StartsWith("/r"))
            {
                action = ExitWindows.Reboot;
            } else if (parameters.StartsWith("/s"))
            {
                action = ExitWindows.ShutDown;
            }

            if (action != ExitWindows.Reboot && action != ExitWindows.PowerOff)
            {
                var proc = Process.Start("shutdown", parameters);
                proc.WaitForExit(60 * 1000);
                return;
            }

            if (!TokenAdjuster.EnablePrivilege("SeShutdownPrivilege", true))
            {
                Log.Error(LogName, "Failed to obtain needed permissions, entering saftey net to ensure a shutdown");
                SafteyNet(parameters);
                return;
            }
            var reboot = (action == ExitWindows.Reboot);

            AttemptShutdowns(reboot, message, false, 6);

            Log.Entry(LogName, "Gracefull shutdown requests failed, attempting to force shutdown");
            AttemptShutdowns(reboot, message, true, 3);

            Log.Error(LogName, "Failed to bypass shutdown blocks, entering saftey net to ensure a shutdown");
            SafteyNet(parameters);
        }

        private void AttemptShutdowns(bool reboot, string message, bool force, int attempts)
        {
            var reason = ShutdownReason.MajorApplication | ShutdownReason.MinorMaintenance | ShutdownReason.FlagPlanned;
            for (int i = 0; i < attempts; i++)
            {
                Log.Entry(LogName, $"Attempt {i + 1}/{attempts} to shutdown computer");
                var result = PowerUtilities.InitiateSystemShutdown(message, 0, force, reboot, reason);
                Log.Entry(LogName, $"--> API call returned {result}, will re-attempt in 5 minutes");

                // Busy wait for 5 minutes to ensure the request went through (it may be blocked)
                for (int j = 0; j < 5; j++)
                {
                    Thread.Sleep(60 * 1000);
                }
            }
        }

        private void SafteyNet(string parameters)
        {
            while (true)
            {
                var proc = Process.Start("shutdown", parameters);
                proc.WaitForExit(60 * 1000);
                Log.Entry(LogName, "Issued shutdown request, will re-issue in 5 minutes");
                Thread.Sleep(5* 60 * 1000);
            }
        }

        //Load the ability to lock the computer from the native user32 dll
        [DllImport("user32")]
        private static extern void lockWorkStation();
    }

    internal static class PowerUtilities
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int InitiateSystemShutdownEx(string lpMachineName, string lpMessage, 
            uint dwTimeout, bool bForceAppsClosed,
            bool bRebootAfterShutdown,ShutdownReason dwReason);

        public static int ExitWindows(ExitWindows exitWindows, ShutdownReason reason)
        {
            return ExitWindowsEx(exitWindows, reason);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">The message to record</param>
        /// <param name="timeout">How long to show the shutdown dialog box, in seconds. If zero, there is no prompt and it cannot be aborted easily</param>
        /// <param name="force">Forcibly close all applications, even if they have unsaved changes</param>
        /// <param name="reboot">Restart the machine immediately after shutdown</param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static int InitiateSystemShutdown(string message, uint timeout, bool force, bool reboot, ShutdownReason reason)
        {
            return InitiateSystemShutdownEx(null, message, timeout, force, reboot, reason);
        }
    }


    [Flags]
    internal enum ExitWindows : uint
    {
        // ONE of the following:
        LogOff = 0x00,
        ShutDown = 0x01,
        Reboot = 0x02,
        PowerOff = 0x08,
        RestartApps = 0x40,
        // plus AT MOST ONE of the following two:
        Force = 0x04,
        ForceIfHung = 0x10,
    }

    [Flags]
    public enum ShutdownReason : uint
    {
        None = 0,

        MajorApplication = 0x00040000,
        MajorHardware = 0x00010000,
        MajorLegacyApi = 0x00070000,
        MajorOperatingSystem = 0x00020000,
        MajorOther = 0x00000000,
        MajorPower = 0x00060000,
        MajorSoftware = 0x00030000,
        MajorSystem = 0x00050000,

        MinorBlueScreen = 0x0000000F,
        MinorCordUnplugged = 0x0000000b,
        MinorDisk = 0x00000007,
        MinorEnvironment = 0x0000000c,
        MinorHardwareDriver = 0x0000000d,
        MinorHotfix = 0x00000011,
        MinorHung = 0x00000005,
        MinorInstallation = 0x00000002,
        MinorMaintenance = 0x00000001,
        MinorMMC = 0x00000019,
        MinorNetworkConnectivity = 0x00000014,
        MinorNetworkCard = 0x00000009,
        MinorOther = 0x00000000,
        MinorOtherDriver = 0x0000000e,
        MinorPowerSupply = 0x0000000a,
        MinorProcessor = 0x00000008,
        MinorReconfig = 0x00000004,
        MinorSecurity = 0x00000013,
        MinorSecurityFix = 0x00000012,
        MinorSecurityFixUninstall = 0x00000018,
        MinorServicePack = 0x00000010,
        MinorServicePackUninstall = 0x00000016,
        MinorTermSrv = 0x00000020,
        MinorUnstable = 0x00000006,
        MinorUpgrade = 0x00000003,
        MinorWMI = 0x00000015,

        FlagUserDefined = 0x40000000,
        FlagPlanned = 0x80000000
    }

    internal sealed class TokenAdjuster
    {
        // PInvoke stuff required to set/enable security privileges
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int TOKEN_ADJUST_PRIVILEGES = 0X00000020;
        private const int TOKEN_QUERY = 0X00000008;
        private const int TOKEN_ALL_ACCESS = 0X001f01ff;
        private const int PROCESS_QUERY_INFORMATION = 0X00000400;

        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern int OpenProcessToken(
            IntPtr ProcessHandle, // handle to process
            int DesiredAccess, // desired access to process
            ref IntPtr TokenHandle // handle to open access token
            );

        [DllImport("kernel32", SetLastError = true),
         SuppressUnmanagedCodeSecurity]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int AdjustTokenPrivileges(
            IntPtr TokenHandle,
            int DisableAllPrivileges,
            IntPtr NewState,
            int BufferLength,
            IntPtr PreviousState,
            ref int ReturnLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            ref LUID lpLuid);

        public static bool EnablePrivilege(string lpszPrivilege, bool bEnablePrivilege)
        {
            bool retval = false;
            int ltkpOld = 0;
            IntPtr hToken = IntPtr.Zero;
            TOKEN_PRIVILEGES tkp = new TOKEN_PRIVILEGES();
            tkp.Privileges = new int[3];
            TOKEN_PRIVILEGES tkpOld = new TOKEN_PRIVILEGES();
            tkpOld.Privileges = new int[3];
            LUID tLUID = new LUID();
            tkp.PrivilegeCount = 1;
            if (bEnablePrivilege)
                tkp.Privileges[2] = SE_PRIVILEGE_ENABLED;
            else
                tkp.Privileges[2] = 0;
            if (LookupPrivilegeValue(null, lpszPrivilege, ref tLUID))
            {
                Process proc = Process.GetCurrentProcess();
                if (proc.Handle != IntPtr.Zero)
                {
                    if (OpenProcessToken(proc.Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
                        ref hToken) != 0)
                    {
                        tkp.PrivilegeCount = 1;
                        tkp.Privileges[2] = SE_PRIVILEGE_ENABLED;
                        tkp.Privileges[1] = tLUID.HighPart;
                        tkp.Privileges[0] = tLUID.LowPart;
                        const int bufLength = 256;
                        IntPtr tu = Marshal.AllocHGlobal(bufLength);
                        Marshal.StructureToPtr(tkp, tu, true);
                        if (AdjustTokenPrivileges(hToken, 0, tu, bufLength, IntPtr.Zero, ref ltkpOld) != 0)
                        {
                            // successful AdjustTokenPrivileges doesn't mean privilege could be changed
                            if (Marshal.GetLastWin32Error() == 0)
                            {
                                retval = true; // Token changed
                            }
                        }
                        TOKEN_PRIVILEGES tokp = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(tu, typeof(TOKEN_PRIVILEGES));
                        Marshal.FreeHGlobal(tu);
                    }
                }
            }
            if (hToken != IntPtr.Zero)
            {
                CloseHandle(hToken);
            }
            return retval;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            internal int LowPart;
            internal int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            private LUID Luid;
            private int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_PRIVILEGES
        {
            internal int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal int[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct _PRIVILEGE_SET
        {
            private int PrivilegeCount;
            private int Control;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // ANYSIZE_ARRAY = 1
            private LUID_AND_ATTRIBUTES[] Privileges;
        }
    }
}