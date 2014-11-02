namespace Bjornej.GruntLauncher
{
    using Jurassic;
    using Jurassic.Library;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Parses a Gruntfile to extract all the contained tasks by executing it and reading them trough a javascript hack. 
    /// See the Init file in the resources. 
    /// </summary>
    public static class GruntParser
    {
        private static List<string> _list = null;
        private static bool _valid = false;

        private static IDictionary<string, Tuple<DateTime, ICollection<string>>> cache = new Dictionary<string, Tuple<DateTime, ICollection<string>>>();

        /// <summary>
        /// Reads all the defined tasks in a Gruntfile whose path is passed as parameter
        /// </summary>
        /// <param name="path">The path of the  file to examine</param>
        /// <returns>A list of tasks</returns>
        public static ICollection<string> ReadAllTasks(string path)
        {
            _list = new List<string>();

            if (path.EndsWith(".ts"))
            {
                path = path.Replace(".ts", ".js").Replace(".coffee", ".js");
            }

            if (!File.Exists(path))
            {
                return _list;
            }

            var lastModifiedDate = File.GetLastWriteTime(path);

            if (cache.ContainsKey(path))
            {
                var data = cache[path];
                if (lastModifiedDate == data.Item1) {
                    return data.Item2;
                }
            }

            if (!ParseFromScriptEngine(path))
            {
                ParseFromProcess(path);
            }

            if (_list.Count == 0)
            {
                _list.Add("Cannot parse Gruntfile");
            }else
            {
                cache[path]= new Tuple<DateTime, ICollection<string>>(lastModifiedDate, _list.Distinct().ToList());
            }

            return _list.Distinct().ToList();
        }

        /// <summary>
        /// Parses task names with help of Jurassic script engine.
        /// </summary>
        private static bool ParseFromScriptEngine(string path)
        {
            // executes the gruntfile with some little additions :)
            try
            {
                var engine = new ScriptEngine();
                engine.Execute(Resources.Init);
                engine.ExecuteFile(path);
                engine.Execute("module.exports(grunt);");

                // reads the evaluated tasks
                ArrayInstance names = (ArrayInstance)engine.Evaluate("names");
                foreach (var elem in names.ElementValues)
                {
                    _list.Add(elem.ToString());
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parses task names with help of grunt process execution.
        /// </summary>
        private static bool ParseFromProcess(string path)
        {
            _valid = false;
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(path),
                    FileName = "cmd",
                    Arguments = " /c \"grunt --help 2>&1\"",
                };

                System.Diagnostics.Process proc = new System.Diagnostics.Process()
                {
                    StartInfo = procStartInfo,
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += (object sendingProcess, DataReceivedEventArgs outLine) => GruntParser.OutputHandler(outLine.Data);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                proc.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads output stream from process execution and builds up task name list.
        /// </summary>
        private static void OutputHandler(string message)
        {
            if (String.IsNullOrEmpty(message))
            {
                if (_valid)
                {
                    // Stop to read from now on.
                    _valid = false;
                }
                // Skip null strings.
                return;
            }

            message = message.Trim();
            if (message.StartsWith("[4mAvailable tasks[24m"))
            {
                // Start to read from now on.
                _valid = true;
                return;
            }

            if (!_valid || message.EndsWith("*") || message.StartsWith("\""))
            {
                // Skip invalid parts and internal tasks of output.
                return;
            }

            int index = message.IndexOf(' ');
            if (index > 0 && index < message.Length)
            {
                // Cut out the task name.
                _list.Add(message.Substring(0, index));
            }
        }
    }
}
