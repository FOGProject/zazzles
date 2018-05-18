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


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Zazzles.Core.Settings
{
    [Flags]
    public enum OSType
    {
        None    = 1,
        Windows = 2,
        Mac     = 4,
        Linux   = 8,
    }
    /*
   public class Settings
   {
       // In the future consider putting these in a key-value map based on the enum
       private IDictionary<string, string> _persistent;
       private IDictionary<string, string> _session;

       public OSType OS { get; }
       public string Location { get; }

       private readonly ILogger _logger;
       private readonly IStorage _storage;

       public Settings(IStorage storage, ILogger<Settings> logger)
       {
           _logger = logger;
           _storage = storage;

           using (_logger.BeginScope(nameof(Settings)))
           {

               OS = calculateOS();
               _logger.LogTrace("Set OS to {os}", OS.ToString());

               Location = calculateBinaryPath();
               _logger.LogTrace("Set binary location to {location}", Location);
           }

       }

       private IDictionary<string, string> getStorageCache(StorageType sType)
       {
           switch (sType)
           {
               case StorageType.Persistent:
                   return _persistent;
               default:
                   return _session;
           }
       }

       private string calculateBinaryPath()
       {
           using(_logger.BeginScope(nameof(calculateBinaryPath)))
           {
               try
               {
                   var assemblyPath = Assembly.GetExecutingAssembly().Location;
                   _logger.LogTrace("Assembly located at '{assemblyPath}'", assemblyPath);

                   var folder =  Path.GetDirectoryName(assemblyPath);
                   _logger.LogTrace("Assembly folder located at '{folder}'", folder);

                   return folder;
               }
               catch (NotSupportedException ex)
               {
                   _logger.LogCritical("Assembly location is not supported", ex);
               }
               catch (Exception ex)
               {
                   _logger.LogCritical("Unable to get binary path", ex);
               }

               return null;
           }

       }

       private OSType calculateOS()
       {
           using(_logger.BeginScope(nameof(calculateOS)))
           {
               OSType osType;
               var pid = PlatformID.Win32NT;
               try
               {
                   pid = Environment.OSVersion.Platform;
               } catch (InvalidOperationException ex)
               {
                   _logger.LogCritical("Unable to get platform id, defaulting to Win32NT", ex);
               }

               switch (pid)
               {
                   case PlatformID.MacOSX:
                       osType = OSType.Mac;
                       break;
                   case PlatformID.Unix:
                       _logger.LogTrace("Detected a Unix system, checking the kernel name");
                       string[] stdout;
                       ProcessHandler.Run("uname", "", true, out stdout);

                       if (stdout != null)
                       {
                           var kerInfo = string.Join(" ", stdout).Trim().ToLower();
                           _logger.LogTrace("uname info: {kerInfo}", kerInfo);
                           if (kerInfo.Contains("darwin"))
                           {
                               _logger.LogTrace("Found darwin kernel, selecting OSX");
                               osType = OSType.Mac;
                               break;
                           }
                       }

                       osType = OSType.Linux;
                       break;
                   default:
                       osType = OSType.Windows;
                       break;
               }

               return osType;
           }
       }

       /// <summary>
       ///     Check if the current OS is compatible with the given type
       /// </summary>
       /// <param name="supportedOSTypes">The type of OsS to check for compatibility with</param>
       /// <returns>True if compatible</returns>
       public bool IsOSCompatible(OSType supportedOSTypes)
       {
           using (_logger.BeginScope(nameof(IsOSCompatible)))
           {
               _logger.LogTrace("Checking if '{compatbility}' contains '{os}'", 
                   supportedOSTypes.ToString(), OS.ToString());
               return supportedOSTypes.HasFlag(OS);
           }
       }

       /// <summary>
       ///     Reparse the settings.json file
       /// </summary>
       public void Reload()
       {
           using (_logger.BeginScope(nameof(Reload)))
           {
               try
               {
                   _logger.LogTrace("Loading persistent");
                   _persistent = _storage.Load(StorageType.Persistent);
                   _logger.LogTrace("Loading session");
                   _session = _storage.Load(StorageType.Session);
               }
               catch (Exception ex)
               {
                   _logger.LogCritical("Unable to reload settings", ex);
                   throw ex;
               }
           }
       }

       /// <summary>
       /// Flush all memory caches to the storage adapter
       /// </summary>
       public void FlushAll()
       {
           using (_logger.BeginScope(nameof(FlushAll)))
           {
               foreach (StorageType sType in Enum.GetValues(typeof(StorageType)))
               {
                   Flush(sType);
               }
           }
       }

       /// <summary>
       ///     Flushes a specific memory cache to the storage adapter
       /// </summary>
       /// <returns>True if successful</returns>
       public void Flush(StorageType sType)
       {
           using (_logger.BeginScope(nameof(Flush)))
           {
               _logger.LogTrace("Flushing '{sType}'", sType.ToString());

               try
               {
                   var toSave = getStorageCache(sType);
                   _storage.Save(toSave, sType);
               }
               catch (Exception ex)
               {
                   _logger.LogCritical("Unable to flush settings", ex);
                   throw ex;
               }
           }
       }

       private void SaveSession()
       {
           using (_logger.BeginScope(nameof(SaveSession)))
           {
               try
               {
                   _logger.LogTrace("Saving session settings");
                   _storage.Save(_session, StorageType.Session);
               }
               catch (Exception ex)
               {
                   _logger.LogCritical("Unable to save session settings", ex);
               }

           }
       }

       /// <summary>
       /// </summary>
       /// <param name="key">The setting to retrieve</param>
       /// <returns>The value of a setting. Will return an empty string if the key is not present.</returns>
       public string Get(string key)
       {
           if (string.IsNullOrEmpty(key))
               throw new ArgumentException("Key must be provided!", nameof(key));

           using (_logger.BeginScope(nameof(SaveSession)))
           {
               _logger.LogTrace("Retrieving value of '{key}'", key);

               try
               {
                   string value = null;

                   if (_session != null)
                   {
                       _logger.LogTrace("Attempting to load value from session data");
                       _session.TryGetValue(key, out value);
                   }

                   if (string.IsNullOrEmpty(value?.ToString()))
                   {
                       _logger.LogTrace("Attempting to load value from persistent data");
                       _persistent.TryGetValue(key, out value);
                   }

                   if (string.IsNullOrEmpty(value))
                   {
                       _logger.LogTrace("Blank or null retrieved");
                       return string.Empty;

                   }

                   return value.Trim();
               }
               catch (Exception ex)
               {
                   _logger.LogWarning("Error retrieving value", ex);
               }

               return string.Empty;
           }
       }

       /// <summary>
       ///     Set the value of a setting. Will automatically save.
       /// </summary>
       /// <param name="key">The name of the setting</param>
       /// <param name="value">The new value of the setting</param>
       public void Set(string key, string value, StorageType sType = StorageType.Session, bool flush = true)
       {
           if (string.IsNullOrEmpty(key))
               throw new ArgumentException("Key must be provided!", nameof(key));
           if (value == null)
               throw new ArgumentNullException(nameof(value));

           using (_logger.BeginScope(nameof(Set)))
           {
               _logger.LogTrace("Setting '{key'} to '{value}' in storage '{sType}', and flush '{flush}'",
                   key, value, sType.ToString(), flush);

               var readStore = getStorageCache(sType);

               if (readStore == null)
               {
                   _logger.LogCritical("Setting storage for '{sType}' is null, this should not happen, setting it",
                       sType.ToString());

                   readStore = new Dictionary<string, string>();
               }
               readStore[key] = value;

               if (flush)
               {
                   Flush(sType);
               }
           }
       }
   }*/
}

