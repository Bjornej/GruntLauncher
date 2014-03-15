namespace Bjornej.GruntLauncher
{
    using Jurassic;
    using Jurassic.Library;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class GulpParser
    {
        public static ICollection<string> ReadAllTasks(string path)
        {
            var list = new List<string>();

            // executes the gulpfile with some little additions :)
            try
            {
                var engine = new ScriptEngine();
                engine.Execute(Resources.gulpInit);
                engine.ExecuteFile(path);

                // reads the evaluated tasks
                ArrayInstance names = (ArrayInstance)engine.Evaluate("tasks");
                foreach (var elem in names.ElementValues)
                {
                    list.Add((string)elem);
                }
            }
            catch (Exception)
            {
                list.Add("Cannot parse gulpfile");
            }

            return list.Distinct().ToList();
        }
    }
}
