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

    public class SystemPower : IDisposable
    {
        private const int DEFAULT_GRACE_PERIOD_MINUTES = 10;
        public const int MAX_DELAY_TIME_MINUTES = 8 * 60;

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

            using (_logger.BeginScope(nameof(SystemPower)))
            {
                _logger.LogTrace("RemoteLocking set to '{remoteLocking}'", remoteLocking);
                if (remoteLocking)
                    bus.Subscribe<PowerEvent>(OnPowerEvent);
            }
        }


        public void ReleaseSystemLockIfHeld()
        {
            using (_logger.BeginScope(nameof(ProcessEvent)))
            {
                _logger.LogTrace("Acquiring remote event lock");
                lock (_remoteEventLock)
                {
                    _logger.LogTrace("Toggling reset event");
                    _abortEvent.Set();
                    _abortEvent.Reset();
                }
            }
        }

        private void OnPowerRequest(Message<PowerRequest> req)
        {
            using (_logger.BeginScope(nameof(OnPowerRequest)))
            {
                _logger.LogTrace("Recieved PowerRequest {req}", req);
            }

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
            using (_logger.BeginScope(nameof(OnPowerEvent)))
            {
                _logger.LogTrace("Recieved PowerEvent {msg}", msg);

                switch (msg.Payload.Action)
                {
                    case PowerAction.Abort:
                        _logger.LogTrace("Acquiring remote event lock");
                        lock (_remoteEventLock)
                        {
                            _logger.LogTrace("Triggering reset event");
                            _abortEvent.Set();
                        }
                        break;
                    case PowerAction.Reboot:
                    case PowerAction.Shutdown:
                        _logger.LogTrace("Acquiring remote event lock");
                        lock (_remoteEventLock)
                        {
                            if (_isLocking)
                            {
                                _logger.LogTrace("A remote lock is already in progress, skipping");
                                return;
                            }

                            _isLocking = true;
                            _logger.LogTrace("Spawning lock task");
                            Task.Run(() =>
                            {
                                _logger.LogTrace("Acquiring SystemLock");
                                lock (SystemLock.Lock)
                                {
                                    _logger.LogTrace("Keeping system locked until an abort is signaled");
                                    _abortEvent.WaitOne();
                                    _logger.LogTrace("Abort signal received, releasing SystemLock");
                                    _isLocking = false;
                                }
                            });
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public bool IsTaskPending()
        {
            using (_logger.BeginScope(nameof(IsTaskPending)))
            {
                var isPending = false;

                _logger.LogTrace("Acquiring task lock");
                lock (_taskLock)
                {
                    isPending = _taskQueue != null;
                }

                _logger.LogTrace("Task Pending is '{isPending}'", isPending);
                return isPending;
            }

        }

        private bool DelayShutdown(TimeSpan span)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));

            using (_logger.BeginScope(nameof(DelayShutdown)))
            {
                _logger.LogTrace("Requested delay shutdown for '{span}'", span);
                _logger.LogTrace("Acquiring task lock");
                lock (_taskLock)
                {
                    if (_taskQueue == null)
                    {
                        _logger.LogTrace("No actice task defined");
                        return false;
                    }

                    if (_taskQueue.Executed)
                    {
                        _logger.LogTrace("Task has already executed");
                        return false;
                    }

                    var delayAmount = _taskQueue.DelayTotal + span;
                    var allowedDelay = MAX_DELAY_TIME_MINUTES - delayAmount.TotalMinutes;
                    if (allowedDelay <= 0)
                    {
                        _logger.LogTrace("Cannot delay anymore");
                        return false;
                    }

                    if (allowedDelay < span.TotalMinutes)
                    {
                        span = TimeSpan.FromMinutes(allowedDelay);
                        _logger.LogInformation("Adjusting new delay to be '{span}'", span);
                    }

                    _taskQueue.Delay(span);
                    return true;
                }
            }
        }

        /// <summary>
        ///     Create a shutdown command
        /// </summary>
        /// <param name="parameters">The parameters to use</param>
        private void ProcessEvent(PowerEvent powerEvent, Exception abortException)
        {
            if (powerEvent == null)
                throw new ArgumentNullException(nameof(powerEvent));

            using (_logger.BeginScope(nameof(ProcessEvent)))
            {
                _logger.LogTrace("Processing {powerEvent}", powerEvent);

                if (abortException != null)
                    _logger.LogCritical("Abort check threw exception", abortException);

                if (powerEvent.Action != PowerAction.Abort)
                {
                    _logger.LogTrace("Acquiring SystemLock");
                    lock (SystemLock.Lock)
                    {
                        _logger.LogTrace("Invoking");
                        InvokeEvent(powerEvent);
                    }
                }
                else
                {
                    _logger.LogTrace("Invoking");
                    InvokeEvent(powerEvent);
                }
            }
        }

        private void InvokeEvent(PowerEvent powerEvent)
        {
            if (powerEvent == null)
                throw new ArgumentNullException(nameof(powerEvent));

            _bus.Publish(powerEvent, MessageScope.Global);
            _powerAPI.InvokeEvent(powerEvent);
        }

        public bool QueueEvent(PowerEvent powerEvent, Func<bool> abortCheck = null)
        {
            if (powerEvent == null)
                throw new ArgumentNullException(nameof(powerEvent));

            using (_logger.BeginScope(nameof(QueueEvent)))
            {
                if (abortCheck != null)
                    _logger.LogTrace("AbortCheck is defined");

                _logger.LogTrace("Acquiring SystemLock");
                lock (_taskLock)
                {
                    if (_taskQueue != null && !_taskQueue.Executed)
                    {
                        _logger.LogTrace("Active task already present");
                        return false;
                    }

                    if (!_systemUsers.AnyLoggedIn())
                    {
                        _logger.LogInformation("No users logged in, performing immediately");
                        InvokeEvent(powerEvent);
                        return true;
                    }

                    if (_taskQueue != null)
                        _taskQueue.Dispose();

                    _logger.LogTrace("Creating and queueing task");
                    _taskQueue = new TaskQueue(powerEvent, ProcessEvent, abortCheck);
                }
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
        public bool AbortShutdown()
        {
            using (_logger.BeginScope(nameof(AbortShutdown)))
            {
                _logger.LogTrace("Acquiring task lock");
                lock (_taskLock)
                {
                    if (_taskQueue == null)
                    {
                        _logger.LogTrace("No actice task defined");
                        return false;
                    }

                    if (_taskQueue.Executed)
                    {
                        _logger.LogTrace("Task has already executed");
                        return false;
                    }

                    _logger.LogTrace("DeQueuing task");
                    _taskQueue.Dispose();
                    _taskQueue = null;
                }
                var pEvent = new PowerEvent(PowerAction.Abort, UserOptions.None, DateTime.UtcNow);
                _bus.Publish(pEvent, MessageScope.Global);

                return true;
            }
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
