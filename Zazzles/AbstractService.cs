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
using System.Threading;
using Zazzles.Middleware;
using Zazzles.Modules;

namespace Zazzles
{
    public abstract class AbstractService
    {
        private readonly Thread _eventWaiterThread;

        private readonly Dictionary<string, AbstractModule> _modules;
        private readonly Queue<dynamic> _eventQueue;

        private object _modulesLock = new object();
        private object _eventQueueLock = new object(); 
        public string Name { get; protected set; }

        protected abstract Dictionary<string, AbstractModule> GetModules();
        protected abstract void Load();
        protected abstract void Unload();

        protected AbstractService()
        {
            _eventWaiterThread = new Thread(EventWaiter) { IsBackground = false };

            _modules = GetModules();
            _eventQueue = new Queue<dynamic>();
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

        protected virtual void EventWaiter()
        {
            while (!Power.ShuttingDown && !Power.Updating)
            {
                ProcessQueue();
                Thread.Sleep(5 * 1000);
            }
        }

        private void ProcessQueue()
        {
            dynamic eventData;

            lock (_eventQueueLock)
            {
                if (_eventQueue.Count == 0)
                    return;

                eventData = _eventQueue.Dequeue();
            }

            RunModule(eventData.module, eventData);
        }

        private void RunModule(string id, dynamic data)
        {
            try
            {
                AbstractModule module;
                lock (_modulesLock)
                {
                    module = _modules[id];
                }
                module.ProcessEvent(data);
            }
            catch (Exception ex)
            {
                Log.Error(Name, "Failed to run " + id.ToString());
                Log.Error(Name, ex);
            }
        }

        protected virtual void ProcessEvent(dynamic data)
        {
            if (data.module == null) return;

            AbstractModule.ModuleType type;
            lock (_modulesLock)
            {
                if (!_modules.ContainsKey(data.module)) return;
                type = _modules[data.module].GetType();
            }

            if (data.sync == null && type == AbstractModule.ModuleType.Asynchronous)
            {
                RunModule(data.module, data);
                return;
            }

            lock (_eventQueueLock)
            {
                _eventQueue.Enqueue(data);
            }
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