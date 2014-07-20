namespace Bjornej.GruntLauncher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Jurassic;
    using Jurassic.Library;

    /// <summary>
    /// Parses a Gruntfile to extract all the contained tasks by executing it and reading them trough a javascript hack. 
    /// See the Init file in the resources. 
    /// </summary>
    public static class GruntParser
    {
        /// <summary>
        /// Reads all the defined tasks in a Gruntfile whose path is passed as parameter
        /// </summary>
        /// <param name="path">The path of the  file to examine</param>
        /// <returns>A list of tasks</returns>
        public static ICollection<string> ReadAllTasks(string path)
        {
            var list = new List<string>();

            if (path.EndsWith(".ts")) {
                path = path.Replace(".ts", ".js").Replace(".coffee",".js");
            }

            if (!File.Exists(path)) {
                return list;
            }

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
                    list.Add(elem.ToString());
                }
            }
            catch (Exception e)
            {
                list.Add("Cannot parse Gruntfile");
            }

            return list.Distinct().ToList();
        }
    }
}
