/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2023 FOG Project
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
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json.Linq;
using Zazzles.Data;
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

        private const string LogName = "Power";
        private const int DefaultGracePeriod = 10;
        public const int MaxDelayTime = 8*60;
        private static int AggregatedDelayTime = 0;

        // Variables needed for aborting a shutdown
        private static Timer _timer;
        private static bool _delayed;
        private static dynamic _requestData = new JObject();
        private static Func<bool> _shouldAbortFunc;
        private static readonly IPower Instance;
        private static readonly Task CompletedTask = Task.FromResult(false);

        public static bool ShuttingDown { get; private set; }
        public static bool Requested { get; private set; }
        public static bool Updating { get; set; }

        static Power()
        {
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

            Bus.Subscribe(Bus.Channel.Power, ParseBus);
        }

        public static bool IsActionPending()
        {
            return ShuttingDown || Updating || Requested;
        }

        private static void ParseBus(dynamic data)
        {
            if (data.action == null)
                return;

            string action = data.action.ToString();
            action = action.Trim();

            if (action.Equals("abort"))
                AbortShutdown();
            else if (action.Equals("shuttingdown") && Bus.GetCurrentMode() == Bus.Mode.Client)
                ShuttingDown = true;
            else if (action.Equals("help"))
                HelpShutdown(data);
            else if (action.Equals("delay"))
                DelayShutdown(data);
            else if (action.Equals("now"))
                ExecutePendingShutdown();
            else if (action.Equals("request"))
                Requested = true;
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
            if (data.delay == null)
                return;
            int delayTime = data.delay;
            var friendlyDelayTime = Time.FormatMinutes(delayTime);

            var suggestedDelayAggregate = AggregatedDelayTime + delayTime;
            if (suggestedDelayAggregate > MaxDelayTime)
            {
                Log.Error(LogName, $"Requested delay of {friendlyDelayTime} exceeds the maximum total delay of {Time.FormatMinutes(MaxDelayTime)}");
                return;
            }

            AggregatedDelayTime = suggestedDelayAggregate;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (delayTime < 1)
                return;

            Log.Entry(LogName, $"Delayed power action by {friendlyDelayTime}");

            _delayed = true;
            _timer = new Timer(TimeSpan.FromMinutes(delayTime).TotalMilliseconds);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        /// <summary>
        ///     Called when a shutdown is requested via the Bus
        /// </summary>
        /// <param name="data">The shutdown data to use</param>
        private static void HelpShutdown(dynamic data)
        {
            if (data.type == null)
                return;
            if (data.reason == null)
                return;

            string type = data.type.ToString();
            type = type.Trim();

            if (type.Equals("shutdown"))
                Shutdown(data.reason.ToString(), ShutdownOptions.Abort, data.reason.ToString());
            else if (type.Equals("reboot"))
                Restart(data.reason.ToString(), ShutdownOptions.Abort, data.reason.ToString());
        }

        /// <summary>
        ///     Create a shutdown command
        /// </summary>
        /// <param name="parameters">The parameters to use</param>
        public static void CreateTask(string parameters, string message = "")
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _shouldAbortFunc = null;
            _requestData = new JObject();

            Log.Entry(LogName, "Creating shutdown request");
            Log.Entry(LogName, "Parameters: " + parameters);

            dynamic json = new JObject();
            json.action = "shuttingdown";
            Bus.Emit(Bus.Channel.Power, json, true);

            Instance.CreateTask(parameters, message);

            Requested = false;
            ShuttingDown = true;
        }

        public static Task QueueShutdown(string parameters, ShutdownOptions options = ShutdownOptions.Abort, 
            string message = null, int gracePeriod = -1)
        {
            // If no user is logged in, skip trying to notify users
            if (!User.AnyLoggedIn())
            {
                CreateTask(parameters, message);
                return CompletedTask;
            }

            // Check if a task is already in progress
            if (_timer != null && _timer.Enabled)
            {
                Log.Entry(LogName, "Power task already in-progress");
                return CompletedTask;
            }

            Requested = true;
            _delayed = false;
            AggregatedDelayTime = 0;

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

            _requestData = new JObject();
            _requestData.action = "request";
            _requestData.period = gracePeriod;
            _requestData.options = options;
            _requestData.command = parameters;
            _requestData.aggregatedDelayTime = AggregatedDelayTime;
            _requestData.message = message ?? string.Empty;

            Bus.Emit(Bus.Channel.Power, _requestData, true);
            _timer = new Timer(gracePeriod*1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
            return CompletedTask;
        }

        private static bool ShouldAbort()
        {
            if (_shouldAbortFunc == null || !_shouldAbortFunc()) return false;
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
                    if (ShouldAbort()) return;

                    string message = null;
                    if (_requestData.message != null)
                        message = _requestData.message.ToString();
                    var options = (_requestData.options == null) ?
                        ShutdownOptions.None :
                        Enum.Parse(typeof(ShutdownOptions), _requestData.options.ToString());

                    QueueShutdown(_requestData.command.ToString(), options, message, (int) _requestData.period);
                    return;
                }

                if (ShouldAbort()) return;

                CreateTask(_requestData.command.ToString(), _requestData.message.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not create shutdown command from request");
                Log.Error(LogName, ex);
            }
        }

        public static Task Shutdown(string comment, ShutdownOptions options = ShutdownOptions.Abort, string message = null)
        {
            return Instance.Shutdown(comment, options, message);
        }

        public static Task Restart(string comment, ShutdownOptions options = ShutdownOptions.Abort, string message = null)
        {
            return Instance.Restart(comment, options, message);
        }

        public static void Shutdown(string comment, Func<bool> abortCheckFunc, ShutdownOptions options = ShutdownOptions.Abort,
            string message = null)
        {
            _shouldAbortFunc = abortCheckFunc;
            Shutdown(comment, options, message);
        }

        public static void Restart(string comment, Func<bool> abortCheckFunc, ShutdownOptions options = ShutdownOptions.Abort,
            string message = null)
        {
            _shouldAbortFunc = abortCheckFunc;
            Restart(comment, options, message);
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
            ShuttingDown = false;
            Requested = false;
            AggregatedDelayTime = 0;

            if (_timer == null) return;

            _timer.Stop();
            _timer.Close();
            _timer = null;

            dynamic abortJson = new JObject();
            abortJson.action = "abort";
            _shouldAbortFunc = null;
            Bus.Emit(Bus.Channel.Power, abortJson, true);
        }
    }
}