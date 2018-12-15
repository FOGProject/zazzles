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
                int targetSection;
                int currentSection;

                if (!int.TryParse(targetSplit[i], out targetSection))
                    return false;
                if (!int.TryParse(currentSplit[i], out currentSection))
                    return false;

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

        //    var filePaths =
        //        Directory.GetFiles(Settings.Location, "*.dll*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);

        //    files.AddRange(filePaths);

            foreach (var file in files)
            {
            //    var src = Path.Combine(Settings.Location, file);
            //    var target = Path.Combine(Settings.Location, "tmp", file);

            //    if (File.Exists(target))
            //        File.Delete(target);

            //    using (var inf = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //    using (var outf = new FileStream(target, FileMode.Create))
            //    {
            //        int b;
            //        while ((b = inf.ReadByte()) != -1)
            //            outf.WriteByte((byte)b);

            //        inf.Close();
            //        outf.Close();
            //    }
           }
        }

        public override void Dispose()
        {
            _bus.Unsubscribe<AppVersion>(OnAppVersion);
        }
    }
}