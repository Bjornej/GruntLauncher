namespace Bjornej.GruntLauncher
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    ///     Main class that implements the gruntLauncher packages
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.1", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidGruntLauncherPkgString)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    public sealed class GruntLauncherPackage : Package
    {
        /// <summary>
        /// List of dynamic commands
        /// </summary>
        private static List<OleMenuCommand> commands;

        /// <summary>
        /// Base Grunt command
        /// </summary>
        private static OleMenuCommand baseCommand;

        /// <summary>
        ///     Window pane used to show grunt output
        /// </summary>
        private static IVsOutputWindowPane outputWindowPane;

        /// <summary>
        ///     Dictionary of currently running processes
        /// </summary>
        private static Dictionary<OleMenuCommand, System.Diagnostics.Process> processes;

        /// <summary>
        ///     Last clicked file. Used to avoid reevaluating continuosly the same file
        /// </summary>
        private string lastFile;

        /// <summary>
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require 
        ///     any Visual Studio service because at this point the package object is created but 
        ///     not sited yet inside Visual Studio environment. The place to do all the other 
        ///     initialization is the Initialize method.
        /// </summary>
        public GruntLauncherPackage()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var dte = (DTE)GetGlobalService(typeof(DTE));
            Window window = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            window.Visible = true;

            // Ensure that the desired pane is visible
            var paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;

            outputWindow.CreatePane(paneGuid, "Grunt execution", 1, 0);
            outputWindow.GetPane(paneGuid, out outputWindowPane);
            processes = new Dictionary<OleMenuCommand, System.Diagnostics.Process>();
        }

        /// <summary>
        ///     Prints a string to the Output window in a custom pane
        /// </summary>
        /// <param name="msg">The string to print</param>
        /// <param name="focus">Decides if the output pane should be focused</param>
        public static void Output(string msg, bool focus = false)
        {
            if (focus) 
            {
                var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                var dte = (DTE)GetGlobalService(typeof(DTE));
                Window window = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                window.Visible = true;
                window.Activate();
                outputWindowPane.Activate();
            }

            // Output the message
            outputWindowPane.OutputString(msg);
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
                OleMenuCommand command = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                command.Visible = false;
                command.BeforeQueryStatus += this.SetVisibility;
                baseCommand = command;
                mcs.AddCommand(command);
            }
        }
        #endregion

        /// <summary>
        /// Gets the extensibility object
        /// </summary>
        /// <returns>The estenzibility object</returns>
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
            EnvDTE80.DTE2 applicationObject = GetDTE2();
            UIHierarchy uih = applicationObject.ToolWindows.SolutionExplorer;
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
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void SetVisibility(object sender, EventArgs e)
        {
            // gets the full path of the clicked file
            var path = this.GetSourceFilePath();

            // optimization to avoid parsing the file again if the clicked file has not changed since last time
            if (path == this.lastFile)
            {
                return;
            }
            else
            {
                this.lastFile = path;
            }

            var myCommand = sender as OleMenuCommand;

            // if the currently selected file is a Gruntfile set the command to visible
            myCommand.Visible = path.ToLower().IndexOf("gruntfile.js") != -1;
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            // delete the old command list
            if (commands == null)
            {
                commands = new List<OleMenuCommand>();
            }

            foreach (var cmd in commands)
            {
                mcs.RemoveCommand(cmd);
            }

            if (myCommand.Visible)
            {
                var list = GruntParser.ReadAllTasks(path);
                if (list.Contains("default"))
                {
                    list.Remove("default");
                }

                // creates the list of commands
                int j = 1;
                foreach (var ele in list)
                {
                    CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGruntLauncher + j);
                    j++;
                    OleMenuCommand command = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                    command.Text = "Grunt: " + ele;
                    command.BeforeQueryStatus += (x, y) => { (x as OleMenuCommand).Visible = true; };
                    commands.Add(command);
                    mcs.AddCommand(command);
                }
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var text = cmd.Text;
            var task = text.Substring(text.IndexOf(':') + 1).Trim();

            if (cmd.Checked)
            {
                System.Diagnostics.Process pro;
                processes.TryGetValue(cmd, out pro);
                if (pro != null)
                {
                    Output("Stopping process " + cmd.Text);
                    ProcessUtilities.KillProcessAndChildren(pro.Id);
                    processes.Remove(cmd);
                }
            }

            try
            {
                if (!cmd.Checked)
                {
                    // launches the grunt process and redirects the output to the output window
                    System.Diagnostics.ProcessStartInfo procStartInfo = new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(this.GetSourceFilePath()),
                        FileName = "cmd"
                    };

                    procStartInfo.Arguments = " /c \"grunt --no-color " + task + "  2>&1 \" ";

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo = procStartInfo;
                    proc.EnableRaisingEvents = true;
                    Output("Executing " + " grunt " + task + " \r\n\r\n", true);

                    proc.OutputDataReceived += (object sendingProcess, DataReceivedEventArgs outLine)
                         => Output(outLine.Data + "\r\n");
                    proc.ErrorDataReceived += (object sendingProcess, DataReceivedEventArgs outLine)
                         => Output(outLine.Data + "\r\n");
                    proc.Exited += (x, y) =>
                    {
                        processes.Remove(cmd);
                        cmd.Checked = false;
                    };
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    cmd.Checked = true;
                    processes.Add(cmd, proc);
                }
                else
                {
                    cmd.Checked = false;
                }
            }
            catch (Exception ex)
            {
                Output(ex.Message);
            }
        }
    }
}
