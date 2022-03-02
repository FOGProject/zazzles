/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2022 FOG Project
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
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Zazzles.Tests.Settings
{
    [TestFixture]
    public class SettingsTests
    {
        [SetUp]
        public void Init()
        {
            WriteSettings();
            Log.Output = Log.Mode.Console;
            Zazzles.Settings.SetPath("settings.json");
        }

        [TearDown]
        public void Dispose()
        {
            File.Delete("settings.json");
        }

        private const string Https = "0";
        private const string Tray = "1";
        private const string Server = "Zazzles.jbob.io";
        private const string Webroot = "";
        private const string Version = "1.9.2";
        private const string Company = "FOG";
        private const string Rootlog = "0";

        private void WriteSettings()
        {
            var settings = new JObject
            {
                {"HTTPS", Https},
                {"Tray", Tray},
                {"Server", Server},
                {"WebRoot", Webroot},
                {"Version", Version},
                {"Company", Company},
                {"RootLog", Rootlog}
            };

            File.WriteAllText("settings.json", settings.ToString());
        }

        [Test]
        public void NullEmptySet()
        {
            Assert.Throws<ArgumentException>(() => Zazzles.Settings.Set(null, new JObject()));
            Assert.Throws<ArgumentException>(() => Zazzles.Settings.Set(string.Empty, new JObject()));
            Assert.Throws<ArgumentNullException>(() => Zazzles.Settings.Set("d", null));
        }

        [Test]
        public void NullEmptyGet()
        {
            Assert.Throws<ArgumentException>(() => Zazzles.Settings.Get(null));
            Assert.Throws<ArgumentException>(() => Zazzles.Settings.Get(string.Empty));
        }

        [Test]
        public void BadGet()
        {
            Assert.IsEmpty(Zazzles.Settings.Get("NO_EXIST"));
            Assert.IsEmpty(Zazzles.Settings.Get("https"));
        }

        [Test]
        public void Get()
        {
            Assert.AreEqual(Https, Zazzles.Settings.Get("HTTPS"));
            Assert.AreEqual(Tray, Zazzles.Settings.Get("Tray"));
            Assert.AreEqual(Server, Zazzles.Settings.Get("Server"));
            Assert.AreEqual(Webroot, Zazzles.Settings.Get("WebRoot"));
            Assert.AreEqual(Version, Zazzles.Settings.Get("Version"));
            Assert.AreEqual(Company, Zazzles.Settings.Get("Company"));
            Assert.AreEqual(Rootlog, Zazzles.Settings.Get("RootLog"));
        }

        [Test]
        public void Set()
        {
            Zazzles.Settings.Set("foo", "bar");
            Assert.AreEqual("bar", Zazzles.Settings.Get("foo"));
        }
    }
}