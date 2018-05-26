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


using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Zazzles.Core.Device;
using Zazzles.Core.Device.Power;
using Zazzles.Core.PubSub;
using Zazzles.Modules.Updater.DataContract;

namespace Zazzles.Core.Modules.Updater
{
    /// <summary>
    ///     Update the FOG Service
    /// </summary>
    public class AppUpdaterModule : AbstractModule
    {
        private const char VERSION_DELIMITER = '.';

        private readonly string[] _upgradeFiles;
        private readonly DevicePower _power;
        private readonly IUpdater _updater;


        public AppUpdaterModule(ILogger<AppUpdaterModule> logger, Bus bus, DevicePower power, 
            IUpdater updater, string[] upgradeFiles) : base(logger, bus)
        {
            _upgradeFiles = upgradeFiles ?? throw new ArgumentNullException(nameof(upgradeFiles));
            _power = power ?? throw new ArgumentNullException(nameof(power));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));

            _bus.Subscribe<AppVersion>(OnAppVersion);
        }

        private void OnAppVersion(Message<AppVersion> msg)
        {
            if (msg == null || msg.Payload == null || msg.MetaData == null)
                throw new ArgumentNullException(nameof(msg));

            if (msg.MetaData.Origin == MessageOrigin.Remote)
                return;

            if (string.IsNullOrWhiteSpace(msg.Payload.Version))
                return;

            if (!ShouldUpgrade(msg.Payload.Version, ""))
                return;

            lock(DeviceLock.Lock)
            {
              //  var updaterPath = Path.Combine(Settings.Location, "tmp", _updater.GetInstallerName());
              //  Communication.DownloadFile("/client/" + _updater.GetInstallerName(), updaterPath);

                var authority = new X509Certificate2();
             //   if (!Authenticode.IsValid(updaterPath, authority))
              //      return;

                PrepareUpdateHelpers();
                // Tell power we are updating
                ApplyUpdate();
            }
        }

        private void ApplyUpdate(string processToKill = null)
        {
            _updater.StopService();

           // ProcessHandler.KillAllEXE(processToKill);
            _updater.ApplyUpdate();

            _updater.StartService();

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updating.info")))
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updating.info"));
        }


        private bool ShouldUpgrade(string target, string current)
        {
            var targetSplit = target.Split(VERSION_DELIMITER);
            var currentSplit = current.Split(VERSION_DELIMITER);

            if (targetSplit.Length != currentSplit.Length)
                return false;

            for (var i = 0; i < targetSplit.Length; i++)
            {
                var targetSection = int.Parse(targetSplit[i]);
                var currentSection = int.Parse(currentSplit[i]);

                if (currentSection > targetSection)
                    return false;
                if (targetSection > currentSection)
                    return true;
            }

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

         //   var filePaths =
         //       Directory.GetFiles(Settings.Location, "*.dll*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);

        //    files.AddRange(filePaths);

            foreach (var file in files)
            {
         //       var src = Path.Combine(Settings.Location, file);
          //      var target = Path.Combine(Settings.Location, "tmp", file);

           //     if (File.Exists(target))
          //          File.Delete(target);

           //     using (var inf = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
           //     using (var outf = new FileStream(target, FileMode.Create))
           //     {
           //         int b;
           //         while ((b = inf.ReadByte()) != -1)
           //             outf.WriteByte((byte)b);

           //         inf.Close();
           //         outf.Close();
           //     }
           }
        }

        public override void Dispose()
        {
            _bus.Unsubscribe<AppVersion>(OnAppVersion);
        }
    }
}