//===================================================
//    GemiNaut, a friendly browser for Gemini space on Windows

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

using GemiNaut.Singletons;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GemiNaut.Serialization.Commandline
{
    internal class ExecuteProcess
    {
        private string _debugLog;

        public string DebugLog
        {
            get { return _debugLog; }
            set
            {
                _debugLog = value;
            }
        }

        public Tuple<int, string, string> ExecuteCommand(string fileName, bool captureStdOut, bool captureStdErr)
        {
            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = captureStdOut;
            p.StartInfo.RedirectStandardError = captureStdErr;
            p.StartInfo.FileName = fileName;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string stdErr = "";
            string stdOut = "";
            int exitCode = -1;

            try
            {
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.

                if (captureStdErr)
                {
                    stdErr = p.StandardError.ReadToEnd();
                }
                if (captureStdOut)
                {
                    stdOut = p.StandardOutput.ReadToEnd();
                }

                p.WaitForExit();

                //string errors = p.StandardError.ReadToEnd();
                exitCode = p.ExitCode;
            }
            catch (Exception err)
            {
                if (err.Message == "The system cannot find the file specified")
                {
                    stdErr = "GemiNaut attempted to launch a helper app with the command line : " 
                        + fileName 
                        + " but the program could not be found. It may have been removed in error by security software.";
                }
                else
                {
                    stdErr = err.ToString();
                }
            }
            return new Tuple<int, string, string>(exitCode, stdOut, stdErr);
        }
        /// <summary>
        /// Execute Command line and return results as a tuple: (exitCode,stdout,stderr)
        /// based on https://msdn.microsoft.com/en-us/library/system.diagnostics.process.standardoutput.aspx
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Tuple<int, string, string> ExecuteCommand(string fileName)
        {
            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = fileName;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            string errors = p.StandardError.ReadToEnd();
            int exitCode = p.ExitCode;

            LogCommand(fileName);
            LogCommand("exit code: " + exitCode);
            LogCommand("errors: " + errors);
            LogCommand("======================================");

            p.WaitForExit();

            return new Tuple<int, string, string>(exitCode, output, errors);
        }

        private void LogCommand(string command)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var logLocation = Path.GetFullPath(desktopPath + "\\GemiNautCommandLog.log");

            //disabled for now, uncomment to help debug integrations
            //File.AppendAllText(logLocation, command + "\r\n");

        }

        /// <summary>
        /// executes a command using execCommand, but logs the command and results
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// 
        public Tuple<int, string, string> LoggedExecute(string command)
        {
            var exec = new ExecuteProcess();

            var log = SimpleLogger.Instance;

            log.Log(command);

            var result = ExecuteCommand(command);

            var output = result.Item2;
            var errors = result.Item3;
            var returnCode = result.Item1;

            if (returnCode != 0)
            {
                log.Log("  return code: " + result.Item1);
            }
            else
            {
                log.Log("  result: success.");
            }

            if (errors.Length > 0)
            {
                string truncatedErrors = new string(errors.Take(200).ToArray());
                log.Log("  errors (first 100 chars): " + truncatedErrors + "...");
            }

            if (output.Length > 0)
            {
                string truncatedOut = new string(output.Take(200).ToArray());
                log.Log("  output (first 100 chars): " + truncatedOut + "...");
            }

            log.Log("\n");
            return result;
        }

        public Tuple<int, string, string> LoggedExecute(string command, bool captureStdOut, bool captureStdErr)
        {
            var exec = new ExecuteProcess();

            var log = SimpleLogger.Instance;

            log.Log(command);

            var result = ExecuteCommand(command, captureStdOut, captureStdErr);

            var output = result.Item2;
            var errors = result.Item3;
            var returnCode = result.Item1;

            if (returnCode != 0)
            {
                log.Log("  return code: " + result.Item1);
            }
            else
            {
                log.Log("  result: success.");
            }

            if (errors.Length > 0)
            {
                string truncatedErrors = new string(errors.Take(200).ToArray());
                log.Log("  errors (first 100 chars): " + truncatedErrors + "...");
            }

            if (output.Length > 0)
            {
                string truncatedOut = new string(output.Take(200).ToArray());
                log.Log("  output (first 100 chars): " + truncatedOut + "...");
            }

            log.Log("\n");
            return result;
        }
    }
}
