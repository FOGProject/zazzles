/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2022 FOG Project
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

using System.Collections.Generic;
using System.Linq;
using Zazzles.Debugger.Commands;
using Zazzles.Debugger.Commands.CBus;
using Zazzles.Debugger.Commands.Middleware;
using Zazzles.Debugger.Commands.Notification;
using Zazzles.Debugger.Commands.Process;
using Zazzles.Debugger.Commands.Settings;
using Zazzles.Debugger.Commands.User;

namespace Zazzles.Debugger
{
    public class Debugger
    {
        private const string Name = "Debugger";
        private readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>
        {
            {"bus", new BusCommand()},
            {"middleware", new MiddlewareCommand()},
            {"notfiication", new NotificationCommand()},
            {"process", new ProcessCommand()},
            {"settings", new SettingsCommand()},
            {"user", new UserCommand()}
        };

        public void AddCommand(string keyword, ICommand command)
        {
            Commands.Add(keyword, command);
        }

        public bool ProcessCommand(string[] command)
        {
            if (command.Length == 0) return false;
            if (command.Length == 1 && command[0].Equals("exit")) return true;

            if (command[0].Equals("?") || command[0].Equals("help"))
            {
                Help();
                return false;
            }

            if (command.Length > 1 && Commands.ContainsKey(command[0]))
                if (Commands[command[0]].Process(command.Skip(1).ToArray()))
                    return false;

            Log.Error(Name, "Unknown command");

            return false;
        }

        public void Help()
        {
            Log.WriteLine("Available commands (append ? to any command for more information)");
            foreach (var keyword in Commands.Keys)
                Log.WriteLine("--> " + keyword);
        }
    }
}