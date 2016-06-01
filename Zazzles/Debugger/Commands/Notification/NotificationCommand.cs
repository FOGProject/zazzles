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

namespace Zazzles.Debugger.Commands.Notification
{
    internal class NotificationCommand : ICommand
    {
        private const string LogName = "Console::Notification";

        public bool Process(string[] args)
        {
            if (args[0].Equals("?") || args[0].Equals("help"))
            {
                Help();
                return true;
            }

            if (args[0].Equals("emit") && args.Length > 2)
            {
                Zazzles.Notification.Emit(args[1], args[2]);
                return true;
            }

            return false;
        }

        private static void Help()
        {
            Log.WriteLine("Available commands");
            Log.WriteLine("--> emit [Title] [Body]");
        }
    }
}