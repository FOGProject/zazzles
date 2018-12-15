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
using Zazzles.Core.Device.Power;
using Zazzles.Core.Device.Proc;
using Zazzles.Core.Device.User;
using Zazzles.Core.PubSub;

namespace Zazzles.Core
{
    public class ZazzlesInstance : IDisposable
    {
        public Bus Bus {get; private set;}
        public DeviceUsers Users {get; private set;}
        public DevicePower Power {get; private set;}

        public DeviceProcess Processes {get; private set;}

        public ZazzlesInstance(
            Bus bus,
            DeviceUsers users,
            DevicePower power,
            DeviceProcess processes)
        {
            this.Bus = bus;
            this.Users = Users;
            this.Power = power;
            this.Processes = processes;
        }

        public void Dispose()
        {
            Power.Dispose();
            Bus.Dispose();
        }
    }
}
