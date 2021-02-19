/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2021 FOG Project
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
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Zazzles
{
    /// <summary>
    ///     Handle all interaction with the log file
    /// </summary>
    public static class Log
    {
        /// <summary>
        ///     The level of logging that should be recorded
        /// </summary>
        public enum Level
        {
            Normal,
            Debug,
            Warn,
            Error
        }

        /// <summary>
        ///     Where to output the log to
        /// </summary>
        public enum Mode
        {
            File,
            Console,
            Quiet
        }

        private const long DefaultMaxLogSize = 502400;
        public const int HeaderLength = 78;
        private const string LogName = "Log";
        public static string FilePath { get; set; }
        public static long MaxSize { get; set; }
        public static Mode Output { get; set; }
        private static object locker = new object();
        
        static Log()
        {
            // Suppress settings errors on initialization
            Output = Mode.Quiet;
            var temp = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(temp))
                temp = Path.GetTempPath();
            if (!string.IsNullOrEmpty(temp) && !Directory.Exists(temp))
                Directory.CreateDirectory(temp);
            FilePath = Path.Combine(temp, "FOGService.install.log");
            Output = Mode.File;

            MaxSize = DefaultMaxLogSize;
        }

        /// <summary>
        ///     Entry a message
        /// </summary>
        /// <param name="level">The logging level</param>
        /// <param name="caller">The name of the calling method or class</param>
        /// <param name="message">The message to log</param>
        public static void Entry(Level level, string caller, string message)
        {
            if (caller == null)
                throw new ArgumentNullException(nameof(caller));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            #if DEBUG
            #else
            if (level == Level.Debug) return;
            #endif

            var prefix = "";

            if (level == Level.Debug || level == Level.Error)
                prefix = level.ToString().ToUpper() + ": ";


            WriteLine(level,
                $" {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()} {caller} {prefix}{message}");
        }

        /// <summary>
        ///     Entry a message
        /// </summary>
        /// <param name="caller">The name of the calling method or class</param>
        /// <param name="message">The message to log</param>
        public static void Entry(string caller, string message)
        {
            Entry(Level.Normal, caller, message);
        }

        public static void Error(string caller, string message)
        {
            Entry(Level.Error, caller, message);
        }

        public static void Warn(string caller, string message)
        {
            Entry(Level.Warn, caller, message);
        }

        public static void Error(string caller, Exception ex)
        {
            Entry(Level.Error, caller, ex.Message);
        }

        public static void Debug(string caller, string message)
        {

#if DEBUG
            Entry(Level.Debug, caller, message);
#endif
        }

        public static void Action(string action)
        {
            var builder = new StringBuilder(action);

            for (var i = action.Length; i < HeaderLength-6; i++)
            {
                builder.Append(".");
            }

            Write(builder.ToString());
        }

        public static void ActionResult(bool success)
        {
            if(success)
                ActionPass();
            else
                ActionFail();
        }

        /// <summary>
        /// Log a colored [Pass]
        /// </summary>
        public static void ActionPass()
        {
            Write("[");
            Write("Pass", ConsoleColor.Green);
            WriteLine("]");
        }

        /// <summary>
        /// Log a colored [Fail]
        /// </summary>
        public static void ActionFail()
        {
            Write("[");
            Write("Fail", ConsoleColor.Red);
            WriteLine("]");
        }

        /// <summary>
        ///     Write a new line to the log
        /// </summary>
        public static void NewLine()
        {
            WriteLine("");
        }

        /// <summary>
        ///     Write a divider to the log
        /// </summary>
        public static void Divider()
        {
            Header("");
        }

        /// <summary>
        ///     Write a header to the log
        /// </summary>
        /// <param name="text">The text to put in the center of the header</param>
        public static void Header(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var headerSize = (double) ((HeaderLength - text.Length))/2;

            var builder = new StringBuilder();

            // Construct the first section
            for (var i = 0; i < (int) Math.Ceiling(headerSize); i++)
                builder.Append("-");

            // Add the text
            builder.Append(text);

            // Construct the last section
            for (var i = 0; i < ((int) Math.Floor(headerSize)); i++)
                builder.Append("-");

            WriteLine(builder.ToString());
        }

        /// <summary>
        ///     Create one header with a divider above and below it
        /// </summary>
        /// <param name="text">The text to put in the center of the header</param>
        public static void PaddedHeader(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            Divider();
            Header(text);
            Divider();
        }

        /// <summary>
        ///     Write text to the log
        /// </summary>
        /// <param name="level">The logging level</param>
        /// <param name="text">The text to write</param>
        /// <param name="color">The color of the text if in Console mode, White will use the default color</param>
        public static void Write(Level level, string text, ConsoleColor color = ConsoleColor.White)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if(level != Level.Debug)
                Bus.Emit(Bus.Channel.Log, MessageToJSON(text));

            switch (Output)
            {
                case Mode.Quiet:
                    break;
                case Mode.Console:
                    Console.ResetColor();

                    if(color != ConsoleColor.White)
                        Console.ForegroundColor = color;

                    if (level == Level.Error)
                        Console.BackgroundColor = ConsoleColor.Red;
                    if (level == Level.Debug)
                        Console.BackgroundColor = ConsoleColor.Blue;

                    Console.Write(text);
            
                    break;
                default:
                    try
                    {
                        lock (locker)
                        {
                            var logDir = Path.GetDirectoryName(FilePath);

                            if(logDir != null && !Directory.Exists(logDir))
                                Directory.CreateDirectory(logDir);

                            var logFile = new FileInfo(FilePath);

                            //Delete the log file if it excedes the max log size
                            if (logFile.Exists && logFile.Length > MaxSize)
                                CleanLog(logFile);

                            //Write message to log file
                            var logWriter = new StreamWriter(FilePath, true);
                            logWriter.Write(text);
                            logWriter.Close();
                        }
                    }
                    catch
                    {
                        //If logging fails then nothing can really be done to silently notify the user
                    }
                    break;
            }
        }

        private static JObject MessageToJSON(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            dynamic json = new JObject();
            json.message = message;
            return json;
        }

        /// <summary>
        ///     Write text to the log
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="color">The color of the text if in Console mode</param>
        public static void Write(string text, ConsoleColor color = ConsoleColor.White)
        {
            Write(Level.Normal, text, color);
        }

        /// <summary>
        ///     Write a line to the log
        /// </summary>
        /// <param name="line">The line to write</param>
        /// <param name="color">The color of the text if in Console mode</param>
        public static void WriteLine(string line, ConsoleColor color = ConsoleColor.White)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            Write(line + "\r\n", color);
        }

        /// <summary>
        ///     Write a line to the log
        /// </summary>
        /// <param name="line">The line to write</param>
        /// <param name="level">The logging level</param>
        /// <param name="color">The color of the text if in Console mode</param>
        public static void WriteLine(Level level, string line, ConsoleColor color = ConsoleColor.White)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            Write(level, line + "\r\n", color);
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            Entry(LogName, "Unhandled exception caught");
            Entry(LogName, $"    Terminating: {ex.IsTerminating}");
            Entry(LogName, $"    Hash code: {ex.ExceptionObject.ToString()}");
        }

        /// <summary>
        ///     Wipe the log
        /// </summary>
        /// <param name="logFile"></param>
        private static void CleanLog(FileSystemInfo logFile)
        {
            if (logFile == null)
                throw new ArgumentNullException(nameof(logFile));

            try
            {
                logFile.Delete();
            }
            catch (Exception ex)
            {
                Error(LogName, "Failed to delete log file");
                Error(LogName, ex.Message);
            }
        }
    }
}