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
using System.Diagnostics;
using Zazzles.Core.Device.Power;
using Zazzles.Core.Device.Power.DataContract;

namespace Zazzles.Linux.Device.Power
{
    public class LinuxPower : IPower
    {
        public void LogOffUser()
        {
            Process.Start("logout");
        }

        public void Hibernate()
        {
            throw new NotImplementedException();
        }

        public void LockWorkStation()
        {
            // gnome-screensaver-command -l
            // physlock 
            throw new NotImplementedException();
        }

        public void InvokeEvent(PowerEvent powerEvent)
        {
            var switches = "";
            switch(powerEvent.Action)
            {
                case PowerAction.Shutdown:
                    switches = "-h";
                    break;
                case PowerAction.Reboot:
                    switches = "-r";
                    break;
                default:
                    return;
            } 
            var parameters = $"{switches} + 0 \"{powerEvent.Comment}\"";
            Process.Start("shutdown", parameters);
        }
    }
}