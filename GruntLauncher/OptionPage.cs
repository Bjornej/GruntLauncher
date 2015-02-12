using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Bjornej.GruntLauncher
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class OptionPage : DialogPage
    {
        #region Constants

        public const string TaskRegexKey = "TaskRegex";
        public const string StopProcessesOnBuildKey = "StopProcessesOnBuild";

        #endregion

        [Category("Tasks Parser")]
        [DisplayName("Exclusion")]
        [Description("Specify a Regex pattern to exclude some unwanted tasks from list.")]
        public string TaskRegex
        {
            get; set;
        }

        [Category("Build")]
        [DisplayName("Stop processes before building")]
        [Description("Stop running grunt tasks when a (re)build is started. For instance, this can be useful when running 'watch' task from Visual Studio and executing 'npm install' when building.")]
        public bool StopProcessesOnBuild
        {
            get; set;
        }
    }
}
