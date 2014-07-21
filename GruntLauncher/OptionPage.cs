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

        [Category("Tasks Parser")]
        [DisplayName("Exclusion")]
        [Description("Specify a Regex pattern to exclude some unwanted tasks from list.")]
        public string TaskRegex
        {
            get; set;
        }
    }
}
