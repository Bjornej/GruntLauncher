using Bjornej.GruntLauncher.Helpers;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bjornej.GruntLauncher.Utilities.Gulp
{
    public static class Init
    {

        public static void Start(DTE2 dte, OleMenuCommandService mcs)
        {
            CommandID gulpMenuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGulpLauncher);
            OleMenuCommand gulpCommand = new OleMenuCommand(GulpCallback, gulpMenuCommandID);
            gulpCommand.Visible = false;
            gulpCommand.BeforeQueryStatus += GulpBeforeQueryStatus;
            mcs.AddCommand(gulpCommand);
        }


        private static void GulpBeforeQueryStatus(object sender, EventArgs e)
        {
            // gets the full path of the clicked file
            var path = SolutionHelpers.GetSourceFilePath();

            var myCommand = sender as OleMenuCommand;
            myCommand.Visible = IsGulpFile();


            if (!GruntLauncherPackage.IsNewFile())
            {
                return;
            }

            OleMenuCommandService mcs = GruntLauncherPackage.MenuService;

            // delete the old command list
            if (GruntLauncherPackage.commands == null)
            {
                GruntLauncherPackage.commands = new List<OleMenuCommand>();
            }

            foreach (var cmd in GruntLauncherPackage.commands)
            {
                mcs.RemoveCommand(cmd);
            }

            if (myCommand.Visible)
            {
                GruntLauncherPackage.lastFile = path;

                var list = GulpParser.ReadAllTasks(path);
                if (list.Contains("default"))
                {
                    list.Remove("default");
                }

                // creates the list of commands
                int j = 1;
                foreach (var ele in list)
                {
                    CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGulpLauncher + j);
                    j++;
                    OleMenuCommand command = new OleMenuCommand(GulpCallback, menuCommandID);
                    command.Text = "Gulp: " + ele;
                    command.BeforeQueryStatus += (x, y) => { (x as OleMenuCommand).Visible = true; };
                    GruntLauncherPackage.commands.Add(command);
                    mcs.AddCommand(command);
                }
            }
        }


        private static void GulpCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var text = cmd.Text;
            var task = text.Substring(text.IndexOf(':') + 1).Trim();

            // if the command is checked it means that there is a running grunt task associated
            // so we kill it
            if (cmd.Checked)
            {
                System.Diagnostics.Process pro;
                GruntLauncherPackage.processes.TryGetValue(cmd, out pro);
                if (pro != null)
                {
                    OutputHelpers.Output("Stopping process " + cmd.Text);
                    ProcessHelpers.KillProcessAndChildren(pro.Id);
                    GruntLauncherPackage.processes.Remove(cmd);
                }
            }

            if (!cmd.Checked)
            {
                // launches the grunt process and redirects the output to the output window
                GruntLauncherPackage.RunProcess(cmd, " /c \"gulp --no-color " + task + "  2>&1 \" ", false);
            }
            else
            {
                cmd.Checked = false;
            }
        }



        public static bool IsGulpFile()
        {
            // gets the full path of the clicked file
            var path = SolutionHelpers.GetSourceFilePath();

            return path.ToLower().IndexOf("gulpfile.js") != -1;
        }
    }
}
