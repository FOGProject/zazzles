/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2020 FOG Project
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
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Zazzles.Tests.Bus
{
    [TestFixture]
    public class BusTests
    {
        private AutoResetEvent _resetRevent;
        private string _message = "";

        [SetUp]
        public void Init()
        {
            _resetRevent = new AutoResetEvent(false);
            Zazzles.Bus.Subscribe(Zazzles.Bus.Channel.Debug, RecieveMessage);
        }


        private void RecieveMessage(JObject data)
        {
            _message = data["message"].ToString();
            _resetRevent.Set();
        }

        [Test]
        public void NullEmit()
        {
            Assert.Throws<ArgumentNullException>(() => Zazzles.Bus.Emit(Zazzles.Bus.Channel.Debug, null));
        }

        [Test]
        public void NullSubscribe()
        {
            Assert.Throws<ArgumentNullException>(() => Zazzles.Bus.Subscribe(Zazzles.Bus.Channel.Debug, null));
        }

        [Test, MaxTime(3000)]
        public void LocalEmit()
        {
            var expected = "HelloWorld@123$";

            var data = new JObject
            {
                {"message", expected}
            };

            Zazzles.Bus.Emit(Zazzles.Bus.Channel.Debug, data);
            _resetRevent.WaitOne();
            Assert.AreEqual(expected, _message);
        }

        [Test]
        public void Unsubscribe()
        {
            var expected = "HelloWorld@123555$";

            var data = new JObject
            {
                {"message", expected}
            };

            Zazzles.Bus.Unsubscribe(Zazzles.Bus.Channel.Debug, RecieveMessage);
            Zazzles.Bus.Emit(Zazzles.Bus.Channel.Debug, data);
            Thread.Sleep(2000);
            Assert.AreNotEqual(expected, _message);
        }
    }
}