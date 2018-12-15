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

        // Update the timer by calculating the difference between
        //   when the event wants to fire and what the current time is
        private void ConfigureTimer()
        {
            lock(_timerLock)
            {
                _timer.Stop();

                TimeSpan delta = _powerEvent.AtTime - DateTime.UtcNow;
                _timer.Interval = delta.TotalMilliseconds;
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

                // Capture any exception when checking if we should abort
                //   if there is one, pass it to the provider executor for it
                //   to deal with
                Exception abortEX = null;
                if (_abortCheck != null)
                {
                    try
                    {
                        if(_abortCheck())
                            return;
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
                _timer.Dispose();
        }
    }
}
