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

using Newtonsoft.Json.Linq;

namespace Zazzles.Modules
{
    /// <summary>
    ///     The base of all FOG Modules
    /// </summary>
    public abstract class AbstractModule
    {
        protected AbstractModule()
        {
            Name = "Generic Module";
            Compatiblity = Settings.OSType.All;
        }

        //Basic variables every module needs
        public string Name { get; protected set; }
        public Settings.OSType Compatiblity { get; protected set; }

        /// <summary>
        ///     Called to Start the module. Filters out modules that are not compatible
        /// </summary>
        public void Start(JObject data)
        {
            if (!Settings.IsCompatible(Compatiblity))
            {
                Log.Entry(Name, "Module is not compatible with " + Settings.OS);
                return;
            }

            Log.Entry(Name, "Running...");
            DoWork(data);
        }

        /// <summary>
        ///     Called after Start() filters out disabled modules. Contains the module's functionality
        /// </summary>
        protected abstract void DoWork(JObject data);
    }
}