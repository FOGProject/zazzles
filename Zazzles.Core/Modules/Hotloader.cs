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

 /*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zazzles.Core.Modules
{
    public static class Hotload 
    {
        private const string LogName = "Module::Hotloader";
        private static readonly Dictionary<string, HotloadModule> _modules;
        public static List<AbstractModule> Modules
        {
            get
            {
                return _modules.Values.Select(hotMod => hotMod.Module).ToList();
            }
        }

        static Hotload()
        {
            _modules = new Dictionary<string, HotloadModule>();
        }

        public static void LoadFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Folder path must be provided!", nameof(path));
            if (!Directory.Exists(path))
                throw new ArgumentException("Folder does not exist!", nameof(path));

            var files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Load(file);
            }
        }

        public static bool Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Filepath to module must be provided!", nameof(path));
            if (_modules.ContainsKey(path))
                throw new ArgumentException("Module already loaded!", nameof(path));
            if (!File.Exists(path))
                throw new ArgumentException("File does not exist!", nameof(path));

            var ads = new AppDomainSetup
            {
                ApplicationBase = Environment.CurrentDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };
            var modDomain = AppDomain.CreateDomain(path, null, ads);
            try
            {
                var module = (IModule) modDomain.CreateInstanceAndUnwrap(path, typeof (IModule).FullName);
                _modules.Add(path, new HotloadModule
                {
                    Domain = modDomain,
                    Module = module
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, $"Failed to hotload module {path}");
                Log.Error(LogName, ex);
            }

            return false;
        }


        public static void Unload(string path)
        {
            if (!_modules.ContainsKey(path))
                return;

            AppDomain.Unload(_modules[path].Domain);
            _modules.Remove(path);
        }

        public static void UnloadAll()
        {
            foreach (var hotMod in _modules.Values)
            {
                AppDomain.Unload(hotMod.Domain);
            }
            _modules.Clear();
        }

    }

    internal class HotloadModule
    {
        public AppDomain Domain;
        public AbstractModule Module;
    }
}

*/