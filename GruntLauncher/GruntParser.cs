using Jurassic;
using Jurassic.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bjornej.GruntLauncher
{

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

            //executes the gruntfile with some little additions :)
            var engine = new ScriptEngine();
            engine.Execute(Resources.Init);
            engine.ExecuteFile(path);
            engine.Execute("module.exports(grunt);");

            //reads the evaluated tasks
            ArrayInstance names = (ArrayInstance)engine.Evaluate("names");
            foreach (var elem in names.ElementValues) {
                list.Add((string)elem);
            }


            return list;
        }


    }
}
