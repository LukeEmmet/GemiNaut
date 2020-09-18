//===================================================
//    GemiNaut, a friendly browser for Http space on Windows

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
using System.Collections.Generic;


namespace GemiNaut.Response
{
    public class HttpResponse
    {
        public int Status;
        public string ContentType;
        public List<string> Info;
        public List<string> Errors;
        public List<string> Notes;
        public bool Redirected;
        public bool AbandonedSize;
        public bool AbandonedTimeout;
        public string FinalUrl;
        public string RequestedUrl;

        public HttpResponse(string url)
        {
            Status = 0;
            ContentType = "";
            Info = new List<string>();
            Errors = new List<string>();
            Notes = new List<string>();
            AbandonedSize = false;
            AbandonedTimeout = false;
            Redirected = false;
            FinalUrl = url;
            RequestedUrl = url;
        }

        public void ParseGemGet(string response)
        {

            string[] split = response.Split(new Char[] { '\n' });

            foreach (string s in split)
            {
                var line = s.Trim();

                string message;

                if (line != "")
                {

                    if (line.Substring(0, 12) == "StatusCode: ")
                    {
                        Status = int.Parse(line.Substring(12));
                    }
                    else if (line.Substring(0, 14) == "Content-Type: ")
                    {
                        ContentType = line.Substring(14);
                    }
                   else if (line.Substring(0, 12) == "Redirected: ")
                    {
                        line = line.Substring(12);
                        //is redirected.
                        FinalUrl = line;
                        Redirected = true;
                    }

                    else if (line.Substring(0, 7) == "Error: ")
                    {
                        Errors.Add(line.Substring(7));
                    }
                    else if (line.Substring(0, 6) == "Info: ")
                    {
                        message = line.Substring(6);

                        if (message.Contains("File is larger than max size limit"))
                        {
                            AbandonedSize = true;
                        } else if (message.Contains("Download timed out"))
                        {
                            AbandonedTimeout = true;
                        } else
                        {
                            Info.Add(message);
                        }

                    }
                    else
                    {
                        Notes.Add(line);
                    }
                }


            }
        }
    }
}
