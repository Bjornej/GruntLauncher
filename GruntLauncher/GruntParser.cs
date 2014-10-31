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
    private static List<string> list = null;
    private static bool valid = false;

    /// <summary>
    /// Reads all the defined tasks in a Gruntfile whose path is passed as parameter
    /// </summary>
    /// <param name="path">The path of the  file to examine</param>
    /// <returns>A list of tasks</returns>
    public static ICollection<string> ReadAllTasks(string path)
    {
      var list = new List<string>();

      if (path.EndsWith(".ts"))
      {
        path = path.Replace(".ts", ".js").Replace(".coffee", ".js");
      }

      if (!File.Exists(path))
      {
        return list;
      }

      valid = false;
      if (!ParseFromScriptEngine(path))
      {
        ParseFromProcess(path);
      }

      if (list.Count == 0)
      {
        list.Add("Cannot parse Gruntfile");
      }
      return list.Distinct().ToList();
    }

    /// <summary>
    /// Parses task names with help of Jurassic script engine.
    /// </summary>
    private static bool ParseFromScriptEngine(string path)
    {
      // executes the gruntfile with some little additions :)
      list = new List<string>();
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
          list.Add(elem.ToString());
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
      list = new List<string>();
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
          Arguments = " /c \"grunt --help \" ",
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
      if (message == null)
      {
        // Skip null strings.
        return;
      }

      message = message.Trim();
      if (message.StartsWith("[4mAvailable tasks[24m"))
      {
        // Start to read from now on.
        valid = true;
        return;
      }

      if (!valid || message.EndsWith("*") || message.StartsWith("\""))
      {
        // Skip invalid parts and internal tasks of output.
        return;
      }

      if (message.Length == 0)
      {
        // Stop to read from now on.
        valid = false;
        return;
      }

      int index = message.IndexOf(' ');
      if (index > 0 && index < message.Length)
      {
        // Cut out the task name.
        list.Add(message.Substring(0, index));
      }
    }
  }
}
