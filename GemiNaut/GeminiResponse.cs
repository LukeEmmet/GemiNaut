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
        public bool Redirected;
        public string FinalUrl;
        public string RequestedUrl;

        public GeminiResponse(string url)
        {
            Status = 0;
            Meta = "";
            Info = new List<string>();
            Errors = new List<string>();
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
                        Info.Add(line.Substring(6));
                    }
                    else
                    {
                        Info.Add(line);
                    }
                }


            }
        }
    }
}
