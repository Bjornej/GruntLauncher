using Bjornej.GruntLauncher.Helpers;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bjornej.GruntLauncher.Utilities.Bower
{
    public static class Init
    {
        private static bool isParent, isChild;


        public static void Start(DTE2 dte, OleMenuCommandService mcs)
        {
            CommandID cmdBower = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidBowerUpdater);
            OleMenuCommand bower = new OleMenuCommand(UpdateBower, cmdBower);
            bower.BeforeQueryStatus += BowerBeforeQueryStatus;
            mcs.AddCommand(bower);
        }


        private static void BowerBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            string path = SolutionHelpers.GetSourceFilePath();

            isParent = path.EndsWith("bower_components\\", StringComparison.OrdinalIgnoreCase);

            if (isParent)
            {
                button.Text = "Bower: Update all packages";
            }
            else
            {
                isChild = Directory.GetParent(path).Parent.Name.EndsWith("bower_components", StringComparison.OrdinalIgnoreCase);
                button.Text = "Bower: Update " + Directory.GetParent(path).Name;
            }

            button.Visible = isParent || isChild;
        }

        private static void UpdateBower(object sender, EventArgs e)
        {
            string path = SolutionHelpers.GetSourceFilePath();
            OleMenuCommand button = (OleMenuCommand)sender;

            if (isParent)
            {
                button.Text = "Update Bower Packages";
                GruntLauncherPackage.RunProcess(button, " /c \"bower update 2>&1 \" ", true);
            }
            else if (isChild)
            {
                string bowerPackage = new DirectoryInfo(path).Name;
                GruntLauncherPackage.RunProcess(button, " /c \"bower update " + bowerPackage + " 2>&1 \" ", true);
            }
        }
    }
}
