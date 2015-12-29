/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2015 FOG Project
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

using Zazzles.Middleware;

namespace Zazzles.Commands.Middleware
{
    internal class CommunicationCommand : ICommand
    {
        private const string LogName = "Console::Middleware::Communication";

        public bool Process(string[] args)
        {
            if (args[0].Equals("?") || args[0].Equals("help"))
            {
                Help();
                return true;
            }

            if (args.Length < 2) return false;

            if (args[0].Equals("response"))
            {
                var response = Communication.GetText(args[1]);
                Log.Entry(LogName, "Response = " + response);

                return true;
            }

            if (args.Length <= 2) return false;

            if (args[0].Equals("post"))
            {
                var response = Communication.Post(args[1], args[2]);
                Log.Entry(LogName, "Response = " + response);
                return true;
            }

            if (args[0].Equals("download"))
            {
                var success = Communication.DownloadFile(args[1], args[2]);
                Log.Entry(LogName, "Passed: " + success);
                return true;
            }

            return false;
        }

        private static void Help()
        {
            Log.WriteLine("Available commands");
            Log.WriteLine("--> download           [url]     [download_path]");
            Log.WriteLine("--> response           [url]");
            Log.WriteLine("--> post               [url]     [parameters]");
        }
    }
}