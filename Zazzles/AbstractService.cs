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
using Newtonsoft.Json.Linq;
using Zazzles.Middleware;
using Zazzles.Modules;

namespace Zazzles
{
    public abstract class AbstractService
    {
        private readonly Thread _eventWaiterThread;
        private readonly Thread _policyAddThread;

        private readonly Dictionary<string, AbstractModule> _modules;
        private readonly Queue<dynamic> _eventQueue;

        private object _modulesLock = new object();
        private object _eventQueueLock = new object(); 
        public string Name { get; protected set; }
        protected int PolicyWaitTime = 60;

        protected abstract Dictionary<string, AbstractModule> GetModules();
        protected abstract void Load();
        protected abstract void Unload();

        protected AbstractService()
        {
            _eventWaiterThread = new Thread(EventWaiter) { IsBackground = false };
            _policyAddThread = new Thread(PolicyWaiter) { IsBackground = true };

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
            _policyAddThread.Start();
            _eventWaiterThread.Start();
        }

        private void PolicyWaiter()
        {
            while (true)
            {
                lock (_eventQueueLock)
                {
                    lock (_modulesLock)
                    {
                        foreach (var moduleKey in _modules.Keys)
                        {
                            if (_modules[moduleKey].Type != AbstractModule.ModuleType.Policy)
                                continue;

                            dynamic message = new JObject();
                            message.module = moduleKey;

                            _eventQueue.Enqueue(message);
                        }
                    }
                }

                Thread.Sleep(PolicyWaitTime * 1000);
            } 
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
            dynamic message;

            lock (_eventQueueLock)
            {
                if (_eventQueue.Count == 0)
                    return;

                message = _eventQueue.Dequeue();
            }

            RunModule(message.module, message.data);
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
                Log.Error(Name, "Failed to run " + id);
                Log.Error(Name, ex);
            }
        }

        protected virtual void ProcessEvent(dynamic message)
        {
            if (message.module == null) return;

            AbstractModule.ModuleType type;
            lock (_modulesLock)
            {
                if (!_modules.ContainsKey(message.module)) return;
                type = _modules[message.module].GetType();
            }

            if (message.sync == null && type == AbstractModule.ModuleType.Asynchronous)
            {
                RunModule(message.module, message.data);
                return;
            }

            lock (_eventQueueLock)
            {
                _eventQueue.Enqueue(message);
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

            _policyAddThread.Abort();
            _eventWaiterThread.Abort();
            Unload();
        }
    }
}