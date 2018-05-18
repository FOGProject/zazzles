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

 
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub;
using Zazzles.Core.System.Power.DataContract;
using Zazzles.Core.System.User;

namespace Zazzles.Core.System.Power
{
    public enum PowerAction
    {
        Abort,
        PerformImmediately,
        Shutdown,
        Reboot
    }
    public enum UserOptions
    {
        None,
        Abort,
        Delay
    }
    /// <summary>
    ///     Handle all shutdown requests
    ///     The windows shutdown command is used instead of the win32 api because it notifies the user prior
    /// </summary>
    public class SystemPower : IDisposable
    {
        private const int DEFAULT_GRACE_PERIOD_MINUTES = 10;
        public const int MAX_DELAY_TIME_MINUTES = 8*60;

        private TaskQueue _taskQueue;
        private readonly object _taskLock = new object();

        private readonly AutoResetEvent _abortEvent = new AutoResetEvent(false);
        private readonly object _remoteEventLock = new object();
        private bool _isLocking = false;

        private readonly IPower _powerAPI;
        private readonly ILogger _logger;
        private readonly Bus _bus;
        private readonly SystemUsers _systemUsers;


        public SystemPower(ILogger<SystemPower> logger, Bus bus, SystemUsers systemUsers, IPower powerAPI, bool remoteLocking = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _systemUsers = systemUsers ?? throw new ArgumentNullException(nameof(systemUsers));
            _powerAPI = powerAPI ?? throw new ArgumentNullException(nameof(powerAPI));

            bus.Subscribe<PowerRequest>(OnPowerRequest);

            if (remoteLocking)
                bus.Subscribe<PowerEvent>(OnPowerEvent);
        }

        public void ReleaseSystemLockIfHeld()
        {
            lock (_remoteEventLock)
            {
                _abortEvent.Set();
                _abortEvent.Reset();
            }
        }

        private void OnPowerRequest(Message<PowerRequest> req)
        {
            switch (req.Payload.Action)
            {
                case PowerAction.Abort:
                    AbortShutdown();
                    break;
                case PowerAction.PerformImmediately:
                    if (_taskQueue != null && !_taskQueue.Executed)
                        _taskQueue.ExecuteNow();
                    break;
                case PowerAction.Reboot:
                    Reboot(req.Payload.Comment);
                    break;
                case PowerAction.Shutdown:
                    Shutdown(req.Payload.Comment);
                    break;
                default:
                    break;
            }
        }

        private void OnPowerEvent(Message<PowerEvent> msg)
        {
            switch(msg.Payload.Action)
            {
                case PowerAction.Abort:
                    lock (_remoteEventLock)
                    {
                        _abortEvent.Set();
                    }
                    break;
                case PowerAction.Reboot:
                case PowerAction.Shutdown:
                    lock (_remoteEventLock)
                    {
                        if (_isLocking)
                            return;

                        _isLocking = true;
                        Task.Run(() =>
                        {
                            lock (SystemLock.Lock)
                            {
                                _abortEvent.WaitOne();
                                _isLocking = false;
                            }
                        });
                    }
                    break;
                default:
                    break;
            }

        }

        public bool IsTaskPending()
        {
            lock (_taskLock)
            {
                return _taskQueue != null;
            }
        }

        private void DelayShutdown(TimeSpan span)
        {
            lock(_taskLock)
            {
                if (_taskQueue == null || _taskQueue.Executed)
                    return;

                var delayAmount = _taskQueue.DelayTotal + span;
                var allowedDelay = MAX_DELAY_TIME_MINUTES - delayAmount.TotalMinutes;
                if (allowedDelay <= 0)
                    return;

                if (allowedDelay < span.TotalMinutes)
                {
                    span = TimeSpan.FromMinutes(allowedDelay);
                }

                _taskQueue.Delay(span);
            }
        }

        /// <summary>
        ///     Create a shutdown command
        /// </summary>
        /// <param name="parameters">The parameters to use</param>
        private void ProcessEvent(PowerEvent powerEvent)
        {
            if (powerEvent == null)
                throw new ArgumentNullException(nameof(powerEvent));

            if (powerEvent.Action != PowerAction.Abort)
            {
                lock (SystemLock.Lock)
                {
                    InvokeEvent(powerEvent);
                }
            } else
            {
                InvokeEvent(powerEvent);
            }

        }

        private void InvokeEvent(PowerEvent powerEvent)
        {
            _bus.Publish(powerEvent, MessageScope.Global);
            _powerAPI.InvokeEvent(powerEvent);
        }

        public bool QueueEvent(PowerEvent powerEvent, Func<bool> abortCheck = null)
        {
            lock(_taskLock)
            {
                if (_taskQueue != null && !_taskQueue.Executed)
                    return false;

                if (!_systemUsers.AnyLoggedIn())
                {
                    InvokeEvent(powerEvent);
                    return true;
                }

                if (_taskQueue != null)
                    _taskQueue.Dispose();

                _taskQueue = new TaskQueue(powerEvent, ProcessEvent, abortCheck);
            }

            return true;
        }

        public void Shutdown(string message, UserOptions options = UserOptions.Abort, Func<bool> abortCheck = null)
        {
            var powerEvent = new PowerEvent(PowerAction.Shutdown, options, DateTime.UtcNow, message);
            QueueEvent(powerEvent, abortCheck);
        }

        public void Reboot(string message, UserOptions options = UserOptions.Abort, Func<bool> abortCheck = null)
        {
            var powerEvent = new PowerEvent(PowerAction.Reboot, options, DateTime.UtcNow, message);
            QueueEvent(powerEvent, abortCheck);
        }

        /// <summary>
        ///     Entry off the current user
        /// </summary>
        public void LogOffUser()
        {
            _powerAPI.LogOffUser();
        }

        /// <summary>
        ///     Hibernate the computer
        /// </summary>
        public void Hibernate()
        {
            _powerAPI.Hibernate();
        }

        /// <summary>
        ///     Lock the workstation
        /// </summary>
        public void LockWorkStation()
        {
            _powerAPI.LockWorkStation();
        }

        /// <summary>
        ///     Abort a shutdown if it is not to late
        /// </summary>
        public void AbortShutdown()
        {
            lock(_taskLock)
            {
                if (_taskQueue == null || _taskQueue.Executed)
                    return;

                _taskQueue.Dispose();
                _taskQueue = null;
            }
            var pEvent = new PowerEvent(PowerAction.Abort, UserOptions.None, DateTime.UtcNow);
            _bus.Publish(pEvent, MessageScope.Global);
        }

        public void Dispose()
        {
            _abortEvent.Dispose();
            if (_taskQueue != null)
                _taskQueue.Dispose();
            _taskQueue = null;
        }
    }
}
