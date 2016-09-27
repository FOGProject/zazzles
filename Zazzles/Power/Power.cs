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
using System.Timers;
using Newtonsoft.Json.Linq;
using Zazzles.Data;
using Zazzles.DataContracts;
using Zazzles.PowerComponents;

namespace Zazzles
{
    /// <summary>
    ///     Handle all shutdown requests
    ///     The windows shutdown command is used instead of the win32 api because it notifies the user prior
    /// </summary>
    public static class Power
    {
        public enum ShutdownOptions
        {
            None,
            Abort,
            Delay
        }

        public enum Status
        {
            None,
            OperationRequested,
            ShuttingDown,
            Updating
        }

        public enum BusCommands
        {
            Abort,
            Delay,
            ExecuteNow
        }

        public enum BusStatusUpdates
        {
            ShutdownRequested,
            ShutdownInProgress
        }

        public enum Actions
        {
            Shutdown,
            Restart,
            Hibernate,
            Lock,
            Logoff,
        }

        private const string LogName = "Power";
        private static readonly IPower Instance;
        public static Status State;

        // Variables needed to delay or abort a shutdown
        private static Timer _timer;
        private static bool _delayed;
        private static PowerAction _requestData;
        private static Func<bool> _shouldAbortFunc;
        private const int DefaultGracePeriod = 10;
        public const int MaxDelayTime = 8 * 60;
        private static int _aggregatedDelayTime;

        static Power()
        {
            State = Status.None;
            switch (Settings.OS)
            {
                case Settings.OSType.Mac:
                    Instance = new MacPower();
                    break;
                case Settings.OSType.Linux:
                    Instance = new LinuxPower();
                    break;
                default:
                    Instance = new WindowsPower();
                    break;
            }

            Bus.Subscribe(Bus.Channel.Power, ParsePower);
            Bus.Subscribe(Bus.Channel.PowerRequest, ParsePowerRequest);
        }

        public static bool IsActionPending()
        {
            return State != Status.None;
        }

        /// <summary>
        /// Server -> Client communication
        /// </summary>
        /// <param name="data"></param>
        private static void ParsePower(dynamic data)
        {
            // Do not accept any commands if the bus is in server mode
            if (Bus.Mode == Bus.Role.Server)
                return;

            if (data.action == null)
                return;

            BusStatusUpdates action = Enum.Parse(typeof(BusStatusUpdates), data.action.ToString(), true);

            switch (action)
            {
                case BusStatusUpdates.ShutdownInProgress:
                    State = Status.ShuttingDown;
                    break;
                case BusStatusUpdates.ShutdownRequested:
                    State = Status.OperationRequested;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Client -> Server communication
        /// </summary>
        /// <param name="data"></param>
        private static void ParsePowerRequest(dynamic data)
        {
            // Do not accept any commands if the bus is in client mode
            if (Bus.Mode == Bus.Role.Client)
                return;

            if (data.action == null)
                return;

            BusCommands action = Enum.Parse(typeof(BusCommands), data.action.ToString(), true);

            switch (action)
            {
                case BusCommands.Abort:
                    if (_requestData.Option == ShutdownOptions.Abort)
                        break;
                    AbortShutdown();
                    break;
                case BusCommands.Delay:
                    if (_requestData.Option != ShutdownOptions.None)
                        break;
                    DelayShutdown(_requestData);
                    break;
                case BusCommands.ExecuteNow:
                    ExecutePendingShutdown();
                    break;
                default:
                    break;
            }
        }

        private static void ExecutePendingShutdown()
        {
            if (_timer == null)
                return;

            _timer.Stop();
            TimerElapsed(null, null);
        }

        private static void DelayShutdown(dynamic data)
        {
            PowerDelayRequest delayRequest = data.ToObject<PowerDelayRequest>();

            var friendlyDelayTime = Time.FormatMinutes(delayRequest.Delay);

            var suggestedDelayAggregate = _aggregatedDelayTime + delayRequest.Delay;
            if (suggestedDelayAggregate > MaxDelayTime)
            {
                Log.Error(LogName, $"Requested delay of {friendlyDelayTime} exceeds the maximum total delay of {Time.FormatMinutes(MaxDelayTime)}");
                return;
            }

            _aggregatedDelayTime = suggestedDelayAggregate;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (delayRequest.Delay < 1)
                return;

            Log.Entry(LogName, $"Delayed power action by {friendlyDelayTime}");

            Notification.Emit("Shutdown Delayed", $"Shutdown has been delayed for {friendlyDelayTime}");

            _delayed = true;
            _timer = new Timer(delayRequest.Delay*1000*60);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();

            if (Settings.OS == Settings.OSType.Windows)
                return;

            ProcessHandler.Run("wall", $"-n <<< \"Shutdown has been delayed by {friendlyDelayTime} \"", true);
        }

        /// <summary>
        ///     Create a shutdown command
        /// </summary>
        /// <param name="parameters">The parameters to use</param>
        public static void CreateTask(string parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _shouldAbortFunc = null;
            _requestData = null;

            Log.Entry(LogName, "Creating shutdown request");
            Log.Entry(LogName, $"Parameters: {parameters}");

            Instance.CreateTask(parameters);

            dynamic json = new JObject();
            json.action = BusStatusUpdates.ShutdownInProgress.ToString();
            Bus.Emit(Bus.Channel.Power, json, true);

            State = Status.ShuttingDown;
        }

        public static void QueueShutdown(Actions type, string parameters, 
            ShutdownOptions options = ShutdownOptions.Abort, 
            string message = null, int gracePeriod = -1)
        {
            // If no user is logged in, skip trying to notify users
            if (!User.AnyLoggedIn())
            {
                CreateTask(parameters);
                return;
            }

            // Check if a task is already in progress
            if (_timer != null && _timer.Enabled)
            {
                Log.Entry(LogName, "Power task already in-progress");
                return;
            }

            State = Status.OperationRequested;
            _delayed = false;
            _aggregatedDelayTime = 0;

            // Load the grace period from Settings or use the default one
            try
            {
                if (gracePeriod == -1)
                    gracePeriod = (!string.IsNullOrEmpty(Settings.Get("PromptTime"))) 
                        ? int.Parse(Settings.Get("PromptTime")) 
                        : DefaultGracePeriod*60;
            }
            catch (Exception)
            {
                gracePeriod = DefaultGracePeriod*60;
            }

            // Generate the request data
            Log.Entry(LogName, $"Creating shutdown command in {gracePeriod} seconds");

            _requestData = new PowerAction()
            {
                Type = type,
                PromptTime = gracePeriod,
                Option = options,
                Command = parameters,
                AggregatedDelayTime = _aggregatedDelayTime,
                Message = message ?? "This computer needs to perform maintenance."
            };

            Bus.Emit(Bus.Channel.Power, _requestData, true);
            _timer = new Timer(gracePeriod*1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();

            // Notify all open consoles about the shutdown (for ssh users)
            if (Settings.OS == Settings.OSType.Windows)
                return;
            ProcessHandler.Run("wall", $"-n <<< \"Shutdown will occur in {gracePeriod} seconds\"", true);
        }

        private static bool ShouldAbort()
        {
            if (_shouldAbortFunc == null || !_shouldAbortFunc())
                return false;

            Log.Entry(LogName, "Shutdown aborted by calling module");
            AbortShutdown();
            return true;
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_delayed)
                {
                    _timer.Dispose();

                    if (ShouldAbort())
                        return;

                    QueueShutdown(_requestData.Type, _requestData.Command, ShutdownOptions.None, 
                        _requestData.Message, _requestData.PromptTime);
                    return;
                }

                if (ShouldAbort())
                    return;

                CreateTask(_requestData.Command);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not create shutdown command from request");
                Log.Error(LogName, ex);
            }
        }

        public static void Shutdown(string comment, ShutdownOptions options = 
            ShutdownOptions.Abort, string message = null, int seconds = 0)
        {
            Instance.Shutdown(comment, options, message, seconds);
        }

        public static void Restart(string comment, ShutdownOptions options = 
            ShutdownOptions.Abort, string message = null, int seconds = 0)
        {
            Instance.Restart(comment, options, message, seconds);
        }

        public static void Shutdown(string comment, Func<bool> abortCheckFunc, 
            ShutdownOptions options = ShutdownOptions.Abort, string message = null, int seconds = 0)
        {
            _shouldAbortFunc = abortCheckFunc;
            Shutdown(comment, options, message, seconds);
        }

        public static void Restart(string comment, Func<bool> abortCheckFunc,
            ShutdownOptions options = ShutdownOptions.Abort, string message = null, int seconds = 0)
        {
            _shouldAbortFunc = abortCheckFunc;
            Restart(comment, options, message, seconds);
        }

        /// <summary>
        ///     Entry off the current user
        /// </summary>
        public static void LogOffUser()
        {
            Instance.LogOffUser();
        }

        /// <summary>
        ///     Hibernate the computer
        /// </summary>
        public static void Hibernate()
        {
            Instance.Hibernate();
        }

        /// <summary>
        ///     Lock the workstation
        /// </summary>
        public static void LockWorkStation()
        {
            Instance.LockWorkStation();
        }

        /// <summary>
        ///     Abort a shutdown if it is not to late
        /// </summary>
        public static void AbortShutdown()
        {
            Log.Entry(LogName, "Aborting shutdown");
            State = Status.None;
            _aggregatedDelayTime = 0;

            if (_timer == null)
                return;

            _timer.Stop();
            _timer.Close();
            _timer = null;

            dynamic abortJson = new JObject();
            abortJson.action = BusCommands.Abort.ToString();
            _shouldAbortFunc = null;
            Bus.Emit(Bus.Channel.Power, abortJson, true);

            Notification.Emit("Shutdown Aborted", "Shutdown has been aborted");
        }
    }
}