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

        [Category("General")]
        [DisplayName("Task regex")]
        [Description("Regex to use to hide specific tasks from list.")]
        public string TaskRegex
        {
            get; set;
        }
    }
}
