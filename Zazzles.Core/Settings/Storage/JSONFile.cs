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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace Zazzles.Core.Settings.Storage
{
    public class JSONFile : IStorage
    {
        private string _pPath;
        private string _sPath;

        public JSONFile(string persistentPath, string sessionPath)
        {
            _pPath = persistentPath;
            _sPath = sessionPath;
        }

        private IDictionary<string, string> LoadJSONFile(string filePath)
        {
            var persistentData = File.ReadAllText(filePath);
            var jData = JObject.Parse(persistentData);
            var dict = jData.ToObject<Dictionary<string, string>>();

            return dict;
        }

        private void SaveJSONFile(IDictionary<string, string> values, string filePath)
        {
            var jString = JsonConvert.SerializeObject(values, Formatting.Indented);
            File.WriteAllText(filePath, jString);
        }

        public IDictionary<string, string> Load(StorageType sType)
        {
            var filePath = (sType == StorageType.Persistent) 
                ? _pPath : _sPath;

            return LoadJSONFile(filePath);
        }

        public bool Save(IDictionary<string, string> data, StorageType sType)
        {
            var filePath = (sType == StorageType.Persistent)
                ? _pPath : _sPath;

            SaveJSONFile(data, filePath);

            return true;
        }
    }
}
