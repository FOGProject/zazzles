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


using System.Collections.Generic;
using System.Threading;
using Zazzles.Middleware;
using Zazzles.Modules;

namespace Zazzles
{
    public abstract class AbstractService
    {
        private readonly Dictionary<string, AbstractModule> _modules;
        private readonly Thread _eventWaiterThread;

        public string Name { get; protected set; }
        protected abstract Dictionary<string, AbstractModule> GetModules();
        protected abstract void Load();
        protected abstract void Unload();

        protected AbstractService()
        {
            _eventWaiterThread = new Thread(EventWaiter)
            {
                Priority = ThreadPriority.Normal,
                IsBackground = false
            };

            _modules = GetModules();
            Name = "Service";
        }

        /// <summary>
        ///     Start the service
        /// </summary>
        public virtual void Start()
        {
            // Only start if a valid server address is present
            if (string.IsNullOrEmpty(Configuration.ServerAddress))
            {
                Log.Error(Name, "ServerAddress not found! Exiting.");
                return;
            }

            Communication.BindServerToBus();
            Bus.Subscribe(Bus.Channel.RemoteRX, ProcessEvent);

            Load();
            _eventWaiterThread.Start();
        }

        /// <summary>
        ///     Loop through all the modules until an update or shutdown is pending
        /// </summary>
        protected virtual void EventWaiter()
        {
            while (!Power.ShuttingDown && !Power.Updating)
            {
                Thread.Sleep(5 * 1000);
            }
        }

        protected virtual void ProcessEvent(dynamic data)
        {
            if (data.module == null) return;
            if (!_modules.ContainsKey(data.module)) return;

            _modules[data.module].ProcessEvent(data);
        }

        /// <summary>
        ///     Stop the service
        /// </summary>
        public virtual void Stop()
        {
            Log.Entry(Name, "Stop requested");

            Communication.UnBindServerFromBus();
            Bus.Unsubscribe(Bus.Channel.RemoteRX, ProcessEvent);

            _eventWaiterThread.Abort();
            Unload();
        }
    }
}