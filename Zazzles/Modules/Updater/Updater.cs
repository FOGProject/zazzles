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
        public enum BusActions
        {
            Start,
            Stop
        }
        private readonly string[] _upgradeFiles;
        private const string RemoteDownloadUrl = "/client/SmartInstaller.exe";

        public ClientUpdater(string[] upgradeFiles)
        {
            Name = "ClientUpdater";
            ShutdownFriendly = false;
            this._upgradeFiles = upgradeFiles;
        }

        protected override void DoWork(Response data, UpdateMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Version))
            {
                Log.Error(Name, "No version provided by server");
                return;
            }

            var updaterPath = Path.Combine(Settings.Location, "tmp", "SmartInstaller.exe");
            if (File.Exists(updaterPath))
                File.Delete(updaterPath);

            if (!NeedUpgrade(msg.Version))
            {
                Log.Entry(Name, "No update needed");
                return;
            }

            Communication.DownloadFile(RemoteDownloadUrl, updaterPath);
            if (!IsAuthenticate(updaterPath))
                return;

            PrepareUpdateHelpers();
            Power.State = Power.Status.Updating;
        }

        private static bool NeedUpgrade(string serverVersion)
        {
            const char versionDelimeter = '.';
            var localVersion = Settings.Get("Version");
            var server = serverVersion.Split(versionDelimeter);
            var local = localVersion.Split(versionDelimeter);
            var needUpgrade = false;

            for (var i = 0; i < server.Length; i++)
            {
                var serverSection = int.Parse(server[i]);
                var localSection = int.Parse(local[i]);

                if (localSection > serverSection)
                    return false;

                if (serverSection > localSection)
                {
                    needUpgrade = true;
                    break;
                }
            }

            return needUpgrade;
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

        /// <summary>
        /// Move the needed upgrading files into a temporary directory
        /// so that they won't be touched during the update process
        /// </summary>
        private void PrepareUpdateHelpers()
        {
            var files = new List<string>
            {
                "settings.json",
                "token.dat"
            };

            files.AddRange(_upgradeFiles);

            var filePaths = Directory.GetFiles(Settings.Location, "*.dll*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName);
            files.AddRange(filePaths);

            foreach (var file in files)
            {
                var src = Path.Combine(Settings.Location, file);
                var target = Path.Combine(Settings.Location, "tmp", file);
                SafeFileCopy(src, target);
            }
        }

        /// <summary>
        /// Perform a byte-for-byte copy of each file inorder to circumvent OS-specific file locks
        /// </summary>
        /// <param name="src">The filepath of the source file</param>
        /// <param name="dst">The filepath of the destination file</param>
        private void SafeFileCopy(string src, string dst)
        {
            if (File.Exists(dst))
                File.Delete(dst);

            try
            {
                using (var inf = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var outf = new FileStream(dst, FileMode.Create))
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
                Log.Error(Name, "Unable to copy file");
                Log.Error(Name, ex);
            }
        }
    }
}