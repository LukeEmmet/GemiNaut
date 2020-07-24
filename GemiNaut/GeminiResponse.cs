using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GemiNaut.Response
{
    public class GeminiResponse
    {
        public int Status;
        public string Meta;
        public List<string> Info;
        public List<string> Errors;
        public List<string> Notes;
        public bool Redirected;
        public bool AbandonedSize;
        public bool AbandonedTimeout;
        public string FinalUrl;
        public string RequestedUrl;

        public GeminiResponse(string url)
        {
            Status = 0;
            Meta = "";
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

                    if (line.Substring(0, 8) == "Header: ")
                    {
                        line = line.Substring(8);

                        string[] headerSplit = line.Split(new Char[] { ' ', '\t' }, 2);

                        Status = Convert.ToInt32(headerSplit[0]);
                        Meta = headerSplit[1].Trim();

                        if (Status == 30 || Status == 31)
                        {
                            //is redirected.
                            FinalUrl = Meta;
                            Redirected = true;
                        }

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
