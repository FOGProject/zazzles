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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Zazzles.Core.Device.User;

namespace Zazzles.Windows.Device.User
{
    public class WindowsUser : IUser
    {
        public enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo
        }

        public int GetInactivityTime()
        {
            var lastInputInfo = new Lastinputinfo();
            lastInputInfo.CbSize = (uint) Marshal.SizeOf(lastInputInfo);
            lastInputInfo.DwTime = 0;

            var envTicks = (uint) Environment.TickCount;

            if (!GetLastInputInfo(ref lastInputInfo))
                return 0;

            var lastInputTick = lastInputInfo.DwTime;
            var idleTime = envTicks - lastInputTick;

            return (int) idleTime/1000;
        }

        public IEnumerable<string> GetUsersLoggedIn()
        {
            var sessionIds = GetSessionIds();

            return (from sessionId in sessionIds
                where !GetUserNameFromSessionId(sessionId, false)
                    .Equals("SYSTEM")
                select GetUserNameFromSessionId(sessionId, false)).Distinct();
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref Lastinputinfo plii);

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass,
            out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        /// <summary>
        ///     Get all active session IDs
        /// </summary>
        /// <returns>A list of session IDs</returns>
        private static List<int> GetSessionIds()
        {
            var sessionIds = new List<int>();
            var properties = new[] {"SessionId"};

            var query = new SelectQuery("Win32_Process", "", properties); //SessionId
            var searcher = new ManagementObjectSearcher(query);

            foreach (var envVar in searcher.Get())
            {
                if (!sessionIds.Contains(int.Parse(envVar["SessionId"].ToString())))
                    sessionIds.Add(int.Parse(envVar["SessionId"].ToString()));
            }

            return sessionIds;
        }

        /// <summary>
        ///     Convert a session ID to its correlating username
        /// </summary>
        /// <param name="sessionId">The session ID to use</param>
        /// <param name="prependDomain">If the user's domain should be prepended</param>
        /// <returns>The username</returns>
        //https://stackoverflow.com/questions/19487541/get-windows-user-name-from-sessionid
        private static string GetUserNameFromSessionId(int sessionId, bool prependDomain)
        {
            var username = "SYSTEM";
            if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out IntPtr buffer, out int strLen) ||
                strLen <= 1) return username;
            username = Marshal.PtrToStringAnsi(buffer);
            WTSFreeMemory(buffer);
            if (!prependDomain) return username;
            if (
                !WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out strLen) ||
                strLen <= 1) return username;
            username = Marshal.PtrToStringAnsi(buffer) + "\\" + username;
            WTSFreeMemory(buffer);
            return username;
        }

        internal struct Lastinputinfo
        {
            public uint CbSize;
            public uint DwTime;
        }
    }
}