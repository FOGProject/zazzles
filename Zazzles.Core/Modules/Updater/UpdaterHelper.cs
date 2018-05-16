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

 /*
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Zazzles.Modules.Updater
{
    public static class UpdaterHelper
    {
        private const string LogName = "UpdaterHelper";

        public static void ApplyUpdate<TWindows, TMac, TLinux>(string processToKill)
            where TWindows : IUpdate
            where TMac : IUpdate
            where TLinux : IUpdate
        {
            IUpdate instance;
            switch (Settings.OS)
            {
                case Settings.OSType.Mac:
                    instance = Activator.CreateInstance<TMac>();
                    break;
                case Settings.OSType.Linux:
                    instance = Activator.CreateInstance<TLinux>();
                    break;
                default:
                    instance = Activator.CreateInstance<TWindows>();
                    break;
            }

            ApplyUpdate(instance, processToKill);
        }

        private static void ApplyUpdate(IUpdate instance, string processToKill)
        {
            Log.Entry(LogName, "Shutting down service...");
            instance.StopService();

            Log.Entry(LogName, "Killing remaining processes...");
            ProcessHandler.KillAllEXE(processToKill);
            Log.Entry(LogName, "Applying update...");
            instance.ApplyUpdate();

            Log.Entry(LogName, "Starting service...");
            instance.StartService();

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updating.info")))
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updating.info"));
        }

        public static bool Updating()
        {
            var updateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updating.info");
            var fileFound = File.Exists(updateFile);
            Thread.Sleep(10 * 1000);

            return fileFound;
        }
    }
}
*/