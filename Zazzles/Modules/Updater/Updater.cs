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

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Zazzles.Data;
using Zazzles.Middleware;

namespace Zazzles.Modules.Updater
{
    /// <summary>
    ///     Update the FOG Service
    /// </summary>
    public sealed class ClientUpdater : AbstractModule<UpdaterMessage>
    {
        private string[] _upgradeFiles;
        public override string LogName { get; protected set; }
        public override sealed Settings.OSType Compatiblity { get; protected set; }
        public override EventProcessorType Type { get; protected set; }

        public ClientUpdater(string[] upgradeFiles)
        {
            LogName = "ClientUpdater";
            Compatiblity = Settings.OSType.All;
            Type = EventProcessorType.Synchronous;

            _upgradeFiles = upgradeFiles;
        }

        private bool IsAuthenticate(string filePath)
        {
            var signeeCert = RSA.ExtractDigitalSignature(filePath);
            var targetSigner = RSA.FOGProjectCertificate();
            if (RSA.IsFromCA(targetSigner, signeeCert))
            {
                Log.Entry(LogName, "Update file is authentic");
                return true;
            }

            Log.Error(LogName, "Update file is not authentic");
            return false;
        }

        //Prepare the downloaded update
        private void PrepareUpdateHelpers()
        {
            var files = new List<string>
            {
                "Zazzles.dll",
                "Newtonsoft.Json.dll",
                "settings.json",
                "token.dat"
            };

            files.AddRange(_upgradeFiles);

            foreach (var file in files)
            {
                try
                {
                    File.Copy(Path.Combine(Settings.Location, file),
                        Path.Combine(Settings.Location, "tmp", file), true);
                }
                catch (Exception ex)
                {
                    Log.Error(LogName, "Unable to prepare file:" + file);
                    Log.Error(LogName, ex);
                }
            }
        }

        protected override void OnEvent(UpdaterMessage message)
        {
            var localVersion = Settings.Get("Version");
            try
            {
                var updaterPath = Path.Combine(Settings.Location, "tmp", "SmartInstaller.exe");

                if (File.Exists(updaterPath))
                    File.Delete(updaterPath);

                var server = int.Parse(message.Version.Replace(".", ""));
                var local = int.Parse(localVersion.Replace(".", ""));

                if (server <= local) return;

                // Ensure the update is authentic
                Communication.DownloadFile(Configuration.ServerAddress + "/getclient", updaterPath);
                if (!IsAuthenticate(updaterPath)) return;

                PrepareUpdateHelpers();
                Power.Updating = true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to parse versions");
                Log.Error(LogName, ex);
            }
        }
    }
}