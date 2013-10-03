using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bjornej.GruntLauncher
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidGruntLauncherPkgString)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    public sealed class GruntLauncherPackage : Package
    {


        /// <summary>
        /// List of dynamic commands
        /// </summary>
        private static List<OleMenuCommand> Commands;

        /// <summary>
        /// Base Grunt command
        /// </summary>
        private static OleMenuCommand Base;

        private static System.Diagnostics.Process process;

        private string lastFile;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public GruntLauncherPackage()
        {
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGruntLauncher);
                OleMenuCommand command = new OleMenuCommand(MenuItemCallback, menuCommandID);
                command.Visible = false;
                command.BeforeQueryStatus += SetVisibility;
                Base = command;
                mcs.AddCommand(command);
            }
        }
        #endregion

        /// <summary>
        /// Gets the extensibility object
        /// </summary>
        private static EnvDTE80.DTE2 GetDTE2()
        {
            return GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        /// <summary>
        /// Get the full path of the file clicked upon when opening the contextual menu 
        /// </summary>
        /// <returns>The full path of the current file</returns>
        private string GetSourceFilePath()
        {
            EnvDTE80.DTE2 _applicationObject = GetDTE2();
            UIHierarchy uih = _applicationObject.ToolWindows.SolutionExplorer;
            Array selectedItems = (Array)uih.SelectedItems;

            if (null != selectedItems)
            {
                foreach (UIHierarchyItem selItem in selectedItems)
                {
                    ProjectItem prjItem = selItem.Object as ProjectItem;
                    string filePath;
                    if (prjItem.Properties != null)
                    {
                        filePath = prjItem.Properties.Item("FullPath").Value.ToString();
                    }
                    else
                    {
                        filePath = prjItem.FileNames[1];
                    }
                    return filePath;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the visibility of the command and creates the dynamic list of commands
        /// </summary>
        private void SetVisibility(object sender, EventArgs e)
        {

            //gets the full path of the clicked file
            var path = GetSourceFilePath();

            //optimization to avoid parsing the file again if the clicked file has not changed since last time
            if (path == lastFile)
            {
                return;
            }
            else
            {
                lastFile = path;
            }

            var myCommand = sender as OleMenuCommand;
            //if the currently selected file is a Gruntfile set the command to visible
            myCommand.Visible = (path.ToLower()).IndexOf("gruntfile.js") != -1;
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;


            //delete the old command list
            if (Commands == null)
            {
                Commands = new List<OleMenuCommand>();
            }
            foreach (var cmd in Commands)
            {
                mcs.RemoveCommand(cmd);
            }

            if (myCommand.Visible)
            {

                var list = GruntParser.ReadAllTasks(path);
                if (list.Contains("default")) { list.Remove("default"); }

                //creates the list of commands
                int j = 1;
                foreach (var ele in list)
                {
                    CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGruntLauncher + j);
                    j++;
                    OleMenuCommand command = new OleMenuCommand(MenuItemCallback, menuCommandID);
                    command.Text = "Grunt: " + ele;
                    command.BeforeQueryStatus += (x, y) => { (x as OleMenuCommand).Visible = true; };
                    Commands.Add(command);
                    mcs.AddCommand(command);
                }
            }

        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var text = cmd.Text;
            var task = text.Substring(text.IndexOf(':') + 1).Trim();

            try
            {

                //launches the grunt process and redirects the output to the output window
                System.Diagnostics.ProcessStartInfo procStartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(GetSourceFilePath()),
                    FileName = "cmd"
                };

                ///Horrendous hack due to a bug in node in windows which doesn't redirect correctly error output
                /// https://github.com/gruntjs/grunt/issues/510
                procStartInfo.Arguments = " /c \"grunt --no-color " + task + "  2>&1 > grunt.tmp & type grunt.tmp & del grunt.tmp \" ";


                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;

                Output("Executing " + " grunt " + task + " \r\n\r\n");

                proc.OutputDataReceived += (object sendingProcess, DataReceivedEventArgs outLine)
                     => Output(outLine.Data + "\r\n");
                proc.ErrorDataReceived += (object sendingProcess, DataReceivedEventArgs outLine)
                     => Output(outLine.Data + "\r\n");
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                process = proc;
                proc.WaitForExit();
            }
            catch (Exception ex)
            {

                Output(ex.Message);
            }
        }

        /// <summary>
        /// Prints a string to the Output window in a custom pane
        /// </summary>
        /// <param name="msg">The string to print</param>
        public static void Output(string msg)
        {
            // Get the output window
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var dte = (DTE)GetGlobalService(typeof(DTE));
            Window window = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            window.Visible = true;


            // Ensure that the desired pane is visible
            var paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            IVsOutputWindowPane pane;
            outputWindow.CreatePane(paneGuid, "Grunt execution", 1, 0);
            outputWindow.GetPane(paneGuid, out pane);
            pane.Activate();


            // Output the message
            pane.OutputString(msg);
        }

    }
}
