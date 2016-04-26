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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zazzles.Data;
using Zazzles.Middleware;

namespace Zazzles.Modules.Updater
{
    /// <summary>
    ///     Update the FOG Service
    /// </summary>
    public class ClientUpdater : AbstractModule<UpdateMessage>
    {
        private string[] _upgradeFiles;

        public ClientUpdater(string[] upgradeFiles)
        {
            Name = "ClientUpdater";
            this._upgradeFiles = upgradeFiles;
        }

        protected override void DoWork(Response data, UpdateMessage msg)
        {
            var localVersion = Settings.Get("Version");
            var serverVersion = msg.Version;

            if (string.IsNullOrWhiteSpace(serverVersion))
            {
                Log.Error(Name, "No version provided by server");
                return;
            }

            try
            {
                var updaterPath = Path.Combine(Settings.Location, "tmp", "SmartInstaller.exe");

                if (File.Exists(updaterPath))
                    File.Delete(updaterPath);

                var server = int.Parse(serverVersion.Replace(".", ""));
                var local = int.Parse(localVersion.Replace(".", ""));

                if (server <= local) return;

                // Ensure the update is authentic
                Communication.DownloadFile("/client/" + "SmartInstaller.exe", updaterPath);
                if (!IsAuthenticate(updaterPath)) return;

                PrepareUpdateHelpers();
                Power.Updating = true;
            }
            catch (Exception ex)
            {
                Log.Error(Name, "Unable to parse versions");
                Log.Error(Name, ex);
            }
        }

        private bool IsAuthenticate(string filePath)
        {
            var signeeCert = RSA.ExtractDigitalSignature(filePath);
            var targetSigner = RSA.FOGProjectCertificate();
            if (RSA.IsFromCA(targetSigner, signeeCert))
            {
                Log.Entry(Name, "Update file is authentic");
                return true;
            }

            Log.Error(Name, "Update file is not authentic");
            return false;
        }

        //Prepare the downloaded update
        private void PrepareUpdateHelpers()
        {
            var files = new List<string>
            {
                "settings.json",
                "token.dat"
            };

            files.AddRange(_upgradeFiles);

            var filePaths =
                Directory.GetFiles(Settings.Location, "*.dll*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);

            files.AddRange(filePaths);

            foreach (var file in files)
            {
                try
                {
                    //File.Copy(Path.Combine(Settings.Location, file),
                    //    Path.Combine(Settings.Location, "tmp", file), true);

                    var src = Path.Combine(Settings.Location, file);
                    var target = Path.Combine(Settings.Location, "tmp", file);

                    if(File.Exists(target))
                        File.Delete(target);

                    using (var inf = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var outf = new FileStream(target, FileMode.Create))
                    {
                        int b;
                        while ((b = inf.ReadByte()) != -1)
                            outf.WriteByte((byte)b);

                        inf.Close();
                        outf.Close();
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(Name, "Unable to prepare file:" + file);
                    Log.Error(Name, ex);
                }
            }
        }
    }
}