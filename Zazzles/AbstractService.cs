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
using System.Linq;
using System.Threading;
using Zazzles.Middleware;
using Zazzles.Modules;

namespace Zazzles
{
    public abstract class AbstractService
    {
        protected const int DEFAULT_SLEEP_TIME = 60;
        protected const int MIN_SLEEP_TIME = 30;

        private readonly Thread _moduleThread;

        protected AbstractService()
        {
            Name = "Service";

            Log.Entry("Zazzles", "Creating main thread");
            _moduleThread = new Thread(BootStrapModules)
            {
                Priority = ThreadPriority.Normal,
                IsBackground = false
            };

            Log.Entry("Zazzles", "Service construction complete");
        }

        public string Name { get; protected set; }
        protected Response LoopData;
        protected abstract Response GetLoopData();
        protected abstract IModule[] GetModules();
        protected abstract void Load();
        protected abstract void Unload();

        /// <summary>
        ///     Start the service
        /// </summary>
        public virtual void Start()
        {
            Log.NewLine();
            Log.Entry(Name, "Starting service");
            // Only start if a valid server address is present
            if (string.IsNullOrEmpty(Configuration.ServerAddress))
            {
                Log.Error(Name, "ServerAddress not found! Exiting.");
                return;
            }
            _moduleThread.Start();
        }

        private void BootStrapModules()
        {
            Load();
            ModuleLooper();
        }

        /// <summary>
        ///     Loop through all the modules until an update or shutdown is pending
        /// </summary>
        protected virtual void ModuleLooper()
        {
            Log.NewLine();

            // Only run the service if there isn't a shutdown or update pending
            // However, keep looping if a power operation is only Requested as
            // the request may be aborted
            while (!Power.ShuttingDown && !Power.Updating)
            {
                LoopData = GetLoopData() ?? new Response();
                // Stop looping as soon as a shutdown or update pending
                foreach (var module in GetModules().TakeWhile(module => !Power.ShuttingDown && !Power.Updating))
                {
                    // Entry file formatting
                    Log.NewLine();
                    Log.PaddedHeader(module.GetName());
                    Log.Entry("Client-Info", $"Client Version: {Settings.Get("Version")}");
                    Log.Entry("Client-Info", $"Client OS:      {Settings.OS}");
                    Log.Entry("Client-Info", $"Server Version: {Settings.Get("ServerVersion")}");

                    try
                    {
                        var subResponse = LoopData.GetSubResponse(module.GetName().ToLower());
                        if (subResponse == null)
                            continue;

                        module.Start(subResponse);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Name, "Unable to run module");
                        Log.Error(Name, ex);
                    }

                    // Entry file formatting
                    Log.Divider();
                    Log.NewLine();
                }

                // Skip checking for sleep time if there is a shutdown or update pending
                if (Power.ShuttingDown || Power.Updating) break;

                // Once all modules have been run, sleep for the set time
                var sleepTime = GetSleepTime() ?? DEFAULT_SLEEP_TIME;
                Log.Entry(Name, $"Sleeping for {sleepTime} seconds");
                Thread.Sleep(sleepTime * 1000);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>The number of seconds to sleep between running each module</returns>
        protected abstract int? GetSleepTime();

        /// <summary>
        ///     Stop the service
        /// </summary>
        public virtual void Stop()
        {
            Log.Entry(Name, "Stop requested");
            if (_moduleThread.IsAlive)
                _moduleThread.Abort();
            Unload();
        }
    }
}