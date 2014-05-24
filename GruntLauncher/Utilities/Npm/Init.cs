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

namespace Bjornej.GruntLauncher.Utilities.Npm
{
    public static class Init
    {
        private static string packageFile;

        public static void Start(DTE2 dte, OleMenuCommandService mcs)
        {
            CommandID cmdNpm = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidNpmUpdater);
            OleMenuCommand npm = new OleMenuCommand(UpdateNpm, cmdNpm);
            npm.BeforeQueryStatus += NpmBeforeQueryStatus;
            mcs.AddCommand(npm);
        }

        private static void NpmBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            packageFile = SolutionHelpers.GetSourceFilePath();
            bool isPackage = Path.GetFileName(packageFile).Equals("package.json", StringComparison.OrdinalIgnoreCase);
            button.Visible = isPackage;
        }

        private static void UpdateNpm(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            string rootDir = new DirectoryInfo(packageFile).Name;
            GruntLauncherPackage.RunProcess(button, " /c \"npm install 2>&1 \" ", false);
        }
    }
}
