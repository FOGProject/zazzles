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
using System.Timers;
using Zazzles.Core.Device.Power.DataContract;

namespace Zazzles.Core.Device.Power
{
    internal class TaskQueue : IDisposable
    {
        private readonly PowerEvent _powerEvent;
        private readonly Timer _timer;
        public readonly TimeSpan DelayTotal;

        private readonly object _timerLock = new object();

        private readonly Action<PowerEvent, Exception> _executor;
        private readonly Func<bool> _abortCheck;

        public bool Executed { get; private set; }
        public UserOptions Options { get { return _powerEvent.Options; } }

        public TaskQueue(PowerEvent action, Action<PowerEvent, Exception> executor, Func<bool> check)
        {
            Executed = false;

            _powerEvent = action;

            DelayTotal = new TimeSpan();
            _executor = executor;
            _abortCheck = check;

            _timer = new Timer(0);
            _timer.Elapsed += OnTimerElapse;
            _timer.AutoReset = false;
            ConfigureTimer();
        }
          

        public void ExecuteNow()
        {
            lock (_timerLock)
            {
                _timer.Stop();
                OnTimerElapse();
            }
        }

        public void Delay(TimeSpan delay)
        {
            lock(_timerLock)
            {
                DelayTotal.Add(delay);
                _powerEvent.AtTime.Add(delay);

                ConfigureTimer();
            }
        }

        private void ConfigureTimer()
        {
            lock(_timerLock)
            {
                _timer.Stop();
                var now = DateTime.UtcNow;
                TimeSpan delta = _powerEvent.AtTime - now;
                double ms = (int)delta.TotalMilliseconds;
                _timer.Interval = ms;
                _timer.Start();
            }
        }

        private void OnTimerElapse(object sender = null, ElapsedEventArgs e = null)
        {
            lock (_timerLock)
            {
                if (Executed)
                    return;
                Executed = true;

                Exception abortEX = null;
                if (_abortCheck != null)
                {
                    try
                    {
                        if(_abortCheck())
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        abortEX = ex;
                    }
                }

                _executor(_powerEvent, abortEX);
            }
        }

        public void Dispose()
        {
            Executed = true;
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }
    }
}
