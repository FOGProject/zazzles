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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zazzles.Core.PubSub;
using Zazzles.Core.Device.Power.DataContract;
using Zazzles.Core.Device.User;

namespace Zazzles.Core.Device.Power
{
    public enum PowerAction
    {
        Abort,
        Delay,
        PerformImmediately,
        Shutdown,
        Reboot
    }

    [Flags]
    public enum UserOptions
    {
        None = 0,
        Abort = 1,
        Delay = 2,
        PerformImmediately = 4
    }

    public class DevicePower : IDisposable
    {
        private const int DEFAULT_GRACE_PERIOD_MINUTES = 10;
        public const int MAX_DELAY_TIME_MINUTES = 8 * 60;

        private TaskQueue _taskQueue;
        private readonly object _taskLock = new object();

        private readonly AutoResetEvent _abortEvent = new AutoResetEvent(false);
        private readonly object _remoteEventLock = new object();
        private volatile bool _isAsyncRemoteLocking = false;

        private readonly IPower _powerAPI;
        private readonly ILogger _logger;
        private readonly Bus _bus;
        private readonly DeviceUsers _deviceUsers;


        public DevicePower(
            ILogger<DevicePower> logger,
            Bus bus,
            DeviceUsers deviceUsers,
            IPower powerAPI,
            bool allowIPCLocking = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _deviceUsers = deviceUsers ?? throw new ArgumentNullException(nameof(deviceUsers));
            _powerAPI = powerAPI ?? throw new ArgumentNullException(nameof(powerAPI));

            bus.Subscribe<PowerRequest>(OnPowerRequest);


            // If allowIPCLocking, another process can lock us
            //   this is desired when the system-service is performing a power
            //   operation and wants to notify any user-services.
            //   The system-service should NOT have this set for security.
            using (_logger.BeginScope(nameof(DevicePower)))
            {
                _logger.LogTrace(
                    "RemoteLocking set to '{allowIPCLocking}'", allowIPCLocking);
                if (allowIPCLocking)
                    bus.Subscribe<PowerEvent>(OnRemotePowerEvent);
            }
        }

        // Force an unlock of the system-wide device lock
        // if we are currently holding it due to a *remote* power event.
        // Provide this method as a means of preventing potential DOS attacks
        // on the user services
        public void ReleaseSystemLockIfHeld()
        {
            using (_logger.BeginScope(nameof(ReleaseSystemLockIfHeld)))
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

        // Handles power requests from another process
        private void OnPowerRequest(Message<PowerRequest> req)
        {
            using (_logger.BeginScope(nameof(OnPowerRequest)))
            {
                _logger.LogTrace("Recieved PowerRequest {req}", req);
            }

            switch (req.Payload.Action)
            {
                case PowerAction.Abort:
                    AbortShutdown(true);
                    break;
                case PowerAction.Delay:
                    DelayShutdown(req.Payload.When, true);
                    break;
                case PowerAction.PerformImmediately:
                    // If a remote process wants to invoke the pending requests
                    //   right away, enforce the event policy to prevent
                    //   forcing other logged in users off without their consent
                    //   if this was not supposed to be allowed
                    PerformNow(true);
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

        // Handle power events triggered by anotheer process
        private void OnRemotePowerEvent(Message<PowerEvent> msg)
        {
            using (_logger.BeginScope(nameof(OnRemotePowerEvent)))
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
                            // Use a bool to keep track of this lock state for now
                            //  as  will need to spawn a new thread to process
                            //  a remote reboot/shutdown.
                            if (_isAsyncRemoteLocking)
                            {
                                _logger.LogTrace("A remote lock is already in progress, skipping");
                                return;
                            }

                            _isAsyncRemoteLocking = true;
                            _logger.LogTrace("Spawning lock task");
                            // Spawn a new thread lock the system-wide
                            //   device lock and hold it until an abort event
                            //   is triggered
                            Task.Run(() =>
                            {
                                _logger.LogTrace("Acquiring SystemLock");
                                lock (DeviceLock.Lock)
                                {
                                    _logger.LogTrace("Keeping system locked until an abort is signaled");
                                    _abortEvent.WaitOne();

                                    _logger.LogTrace("Abort signal received, releasing SystemLock");
                                    _isAsyncRemoteLocking = false;
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

        public bool PerformNow(bool enforcePermissions = false)
        {
            using (_logger.BeginScope(nameof(PerformNow)))
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

                    if (enforcePermissions &&
                        !_taskQueue.Options.HasFlag(UserOptions.PerformImmediately))
                    {
                        _logger.LogTrace("PerformImmediately not permitted on current task");
                        return false;
                    }

                    _taskQueue.ExecuteNow();
                    return true;
                }
            }
        }

        private bool DelayShutdown(DateTime when, bool enforcePermissions = false)
        {
            if (when == null)
                throw new ArgumentNullException(nameof(when));

            var now = DateTime.UtcNow;
            var delayAmount = when - now;
            return DelayShutdown(delayAmount, enforcePermissions);
        }


        private bool DelayShutdown(TimeSpan span, bool enforcePermissions = false)
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

                    if (enforcePermissions &&
                        !_taskQueue.Options.HasFlag(UserOptions.Delay))
                    {
                        _logger.LogTrace("Delay not permitted on current task");
                        return false;
                    }

                    // Make sure we do not delay more than MAX_DELAY_TIME_MINUTES
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

                // Aborts and delays don't require holding the device lock
                if (powerEvent.Action != PowerAction.Abort &&
                    powerEvent.Action != PowerAction.Delay)
                {
                    _logger.LogTrace("Acquiring SystemLock");
                    lock (DeviceLock.Lock)
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

            // Publish the message first before invoking it,
            //  otherwise we may be shutdown before we have a chance to notify
            _bus.Publish(powerEvent, MessageScope.Global).Wait();
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
                        _logger.LogTrace("An active task is already present");
                        return false;
                    }

                    if (!_deviceUsers.AnyLoggedIn())
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

        public void Shutdown(
            string message,
            UserOptions options = UserOptions.Abort,
            Func<bool> abortCheck = null)
        {
            var powerEvent = new PowerEvent(PowerAction.Shutdown, options, DateTime.UtcNow, message);
            QueueEvent(powerEvent, abortCheck);
        }

        public void Reboot(
            string message,
            UserOptions options = UserOptions.Abort,
            Func<bool> abortCheck = null)
        {
            var powerEvent = new PowerEvent(PowerAction.Reboot, options, DateTime.UtcNow, message);
            QueueEvent(powerEvent, abortCheck);
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
        public bool AbortShutdown(bool enforcePermissions = false)
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

                    if(enforcePermissions &&
                        !_taskQueue.Options.HasFlag(UserOptions.Abort))
                    {
                        _logger.LogTrace("Abort not permitted on current task");
                        return false;
                    }

                    _logger.LogTrace("DeQueuing task");
                    _taskQueue.Dispose();
                    _taskQueue = null;
                }
                var pEvent = new PowerEvent(PowerAction.Abort, UserOptions.None, DateTime.UtcNow);
                _bus.Publish(pEvent, MessageScope.Global).Wait();

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
