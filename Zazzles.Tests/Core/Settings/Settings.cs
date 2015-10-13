/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2015 FOG Project
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

using System.IO;
using Zazzles.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace FOGService.Tests.Core.Settings
{
    [TestFixture]
    public class SettingsTests
    {
        [SetUp]
        public void Init()
        {
            WriteSettings();
            Log.Output = Log.Mode.Console;
            Zazzles.Core.Settings.SetPath("settings.json");
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
        public void BadGet()
        {
            Assert.IsNullOrEmpty(Zazzles.Core.Settings.Get("NO_EXIST"));
            Assert.IsNullOrEmpty(Zazzles.Core.Settings.Get("https"));
        }

        [Test]
        public void Get()
        {
            Assert.AreEqual(Https, Zazzles.Core.Settings.Get("HTTPS"));
            Assert.AreEqual(Tray, Zazzles.Core.Settings.Get("Tray"));
            Assert.AreEqual(Server, Zazzles.Core.Settings.Get("Server"));
            Assert.AreEqual(Webroot, Zazzles.Core.Settings.Get("WebRoot"));
            Assert.AreEqual(Version, Zazzles.Core.Settings.Get("Version"));
            Assert.AreEqual(Company, Zazzles.Core.Settings.Get("Company"));
            Assert.AreEqual(Rootlog, Zazzles.Core.Settings.Get("RootLog"));
        }

        [Test]
        public void Set()
        {
            Zazzles.Core.Settings.Set("foo", "bar");
            Assert.AreEqual("bar", Zazzles.Core.Settings.Get("foo"));
        }
    }
}