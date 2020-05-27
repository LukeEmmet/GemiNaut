//===================================================
//    GemiNaut, a friendly browser for Gemini space on Windows
//    (and for related plucked instruments).

//    Copyright (C) 2020, Luke Emmet 

//    Email: luke [dot] emmet [at] gmail [dot] com

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.
//===================================================

using System;

namespace GemiNaut.Singletons
{



    /// <summary>
    /// simple singleton logger, based on
    /// http://csharpindepth.com/Articles/General/Singleton.aspx#cctor
    /// </summary>
    public class SimpleLogger : ObservableObject
    {
        private static readonly SimpleLogger instance = new SimpleLogger();

        private string _messages;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SimpleLogger()
        {
        }

        private SimpleLogger()
        {
        }

        public static SimpleLogger Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// log a message
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            _messages += message + "\n";
            base.RaisePropertyChangedEvent("Messages");
        }

        public void LogMessage(string message)
        {
            Log(message);
        }
        /// <summary>
        /// clears the log
        /// </summary>
        public void Clear()
        {
            _messages = "";
        }

        /// <summary>
        /// get the messages
        /// </summary>
        public string Messages
        {
            get { return _messages; }
            set { _messages = value; }
        }
    }
}