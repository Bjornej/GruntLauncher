namespace Bjornej.GruntLauncher
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Bjornej.GruntLauncher.Helpers;
    using Microsoft.VisualStudio.Shell;
    using EnvDTE80;
    using EnvDTE;
    using System.Text.RegularExpressions;
    using System.Text;
    

    /// <summary>
    ///     Main class that implements the gruntLauncher packages
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.1", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidGruntLauncherPkgString)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [ProvideOptionPage(typeof(OptionPage), "Grunt Launcher", "General", 0, 0, true)]
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
        ///     Dictionary of currently running processes
        /// </summary>
        private static Dictionary<OleMenuCommand, System.Diagnostics.Process> processes;

        /// <summary>
        ///     Last clicked file. Used to avoid reevaluating continuosly the same file
        /// </summary>
        private string lastFile;

        /// <summary>
        /// The DTE object of Visual Studio
        /// </summary>
        private static DTE2 dte;
        
        /// <summary>
        /// Returns the instance of the OptionPage.
        /// </summary>
        private OptionPage Options
        {
            get { return (OptionPage) GetDialogPage(typeof (OptionPage)); }
        }
        

        /// <summary>
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require 
        ///     any Visual Studio service because at this point the package object is created but 
        ///     not sited yet inside Visual Studio environment. The place to do all the other 
        ///     initialization is the Initialize method.
        /// </summary>
        public GruntLauncherPackage()
        {
            processes = new Dictionary<OleMenuCommand, System.Diagnostics.Process>();
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
            dte = GetService(typeof(DTE)) as DTE2;

            DTE env = (DTE)GetService(typeof(DTE));

            env.Events.BuildEvents.OnBuildBegin += OnOnBuildBegin;


            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID cmdGrunt = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGruntLauncher);
                OleMenuCommand gruntCommand = new OleMenuCommand(this.GruntCallback, cmdGrunt);
                gruntCommand.Visible = false;
                gruntCommand.BeforeQueryStatus += GruntBeforeQueryStatus;
                baseCommand = gruntCommand;
                mcs.AddCommand(gruntCommand);

                CommandID gulpMenuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGulpLauncher);
                OleMenuCommand gulpCommand = new OleMenuCommand(this.GulpCallback, gulpMenuCommandID);
                gulpCommand.Visible = false;
                gulpCommand.BeforeQueryStatus += GulpBeforeQueryStatus;
                mcs.AddCommand(gulpCommand);

                CommandID cmdBower = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidBowerUpdater);
                OleMenuCommand bower = new OleMenuCommand(this.UpdateBower, cmdBower);
                bower.BeforeQueryStatus += BowerBeforeQueryStatus;
                mcs.AddCommand(bower);

                CommandID cmdNpm = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidNpmUpdater);
                OleMenuCommand npm = new OleMenuCommand(this.UpdateNpm, cmdNpm);
                npm.BeforeQueryStatus += NpmBeforeQueryStatus;
                mcs.AddCommand(npm);

                CommandID cmdBowerInstall = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidBowerInstaller);
                OleMenuCommand bow = new OleMenuCommand(this.InstallBower, cmdBowerInstall);
                bow.BeforeQueryStatus += BowerInstallBeforeQueryStatus;
                mcs.AddCommand(bow);
            }
        }

        private void OnOnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (!Options.StopProcessesOnBuild)
                return;

            foreach (var process in processes)
                ProcessHelpers.KillProcessAndChildren(process.Value.Id);

            processes = new Dictionary<OleMenuCommand, System.Diagnostics.Process>();
        }

        private void BowerInstallBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            packageFile = SolutionHelpers.GetSourceFilePath();
            bool isPackage = Path.GetFileName(packageFile).Equals("bower.json", StringComparison.OrdinalIgnoreCase);
            button.Visible = isPackage;
        }

        private void InstallBower(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            string rootDir = new DirectoryInfo(packageFile).Name;
            RunProcess(button, " /c \"bower install 2>&1 \" ", false);
        }

        #endregion

        #region NPM

        string packageFile;

        private void NpmBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            packageFile = SolutionHelpers.GetSourceFilePath();
            bool isPackage = Path.GetFileName(packageFile).Equals("package.json", StringComparison.OrdinalIgnoreCase);
            button.Visible = isPackage;
        }

        private void UpdateNpm(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            string rootDir = new DirectoryInfo(packageFile).Name;
            RunProcess(button, " /c \"npm install 2>&1 \" ", false);
        }

        #endregion

        #region Bower

        private bool isParent, isChild;

        private void BowerBeforeQueryStatus(object sender, EventArgs e)
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

        private void UpdateBower(object sender, EventArgs e)
        {
            string path = SolutionHelpers.GetSourceFilePath();
            OleMenuCommand button = (OleMenuCommand)sender;

            if (isParent)
            {
                button.Text = "Update Bower Packages";
                RunProcess(button, " /c \"bower update 2>&1 \" ", true);
            }
            else if (isChild)
            {
                string bowerPackage = new DirectoryInfo(path).Name;
                RunProcess(button, " /c \"bower update " + bowerPackage + " 2>&1 \" ", true);
            }
        }

        #endregion

        #region Grunt

        /// <summary>
        ///     Determines if the current file is a gruntfile
        /// </summary>
        /// <returns>Boolean that indicates if the clicked file was a gruntfile</returns>
        private bool IsGruntFile()
        {
            // gets the full path of the clicked file
            var path = SolutionHelpers.GetSourceFilePath();

            return ((path.ToLower().IndexOf("gruntfile.js") != -1) ||(path.ToLower().IndexOf("gruntfile.ts") != -1) || (path.ToLower().IndexOf("gruntfile.coffee") != -1));
        }

        /// <summary>
        /// Sets the visibility of the command and creates the dynamic list of commands
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void GruntBeforeQueryStatus(object sender, EventArgs e)
        {
            // gets the full path of the clicked file
            var path = SolutionHelpers.GetSourceFilePath();

            var myCommand = sender as OleMenuCommand;

            // if the currently selected file is a Gruntfile set the command to visible
            myCommand.Visible = this.IsGruntFile();


            if (!this.IsGruntFile() && !this.IsGulpFile())
            {
                this.lastFile = path;
            }

            if (!this.IsNewFile())
            {
                return;
            }




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
                this.lastFile = path;
                var list = GruntParser.ReadAllTasks(path);

                myCommand.Text = "Grunt";
                myCommand.Enabled = true;

                if (list.Count == 0) {
                    myCommand.Enabled = false;
                    myCommand.Text = "Gruntfile.js not found";
                }

                if (list.Contains("default"))
                {
                    list.Remove("default");
                }

                string n = Options.TaskRegex;

                Regex a = null;

                if (!string.IsNullOrEmpty(n)) {
                    try {
                        a = new Regex(n);
                    }
                    catch (Exception)
                    {
                       // invalid regex -> ignore
                    }
                    
                }

                // creates the list of commands
                int j = 1;
                foreach (var ele in list)
                {
                    if (a != null)
                    {                       
                        if (a.Match(ele).Success)
                        {
                            continue;
                        }
                    }

                    CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGruntLauncher + j);
                    j++;
                    OleMenuCommand command = new OleMenuCommand(this.GruntCallback, menuCommandID);
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
        private void GruntCallback(object sender, EventArgs e)
        {
          var cmd = (OleMenuCommand)sender;
          var text = cmd.Text;
          var task = text.Substring(text.IndexOf(':') + 1).Trim();
          if (task == "Grunt") { task = ""; }

          // if the command is checked it means that there is a running grunt task associated
          // so we kill it
          if (cmd.Checked)
          {
            System.Diagnostics.Process pro;
            processes.TryGetValue(cmd, out pro);
            if (pro != null)
            {
              OutputHelpers.Output("Stopping process " + cmd.Text);
              ProcessHelpers.KillProcessAndChildren(pro.Id);
              processes.Remove(cmd);
            }
          }

          if (!cmd.Checked)
          {
            // launches the grunt process and redirects the output to the output window
            RunProcess(cmd, " /c \"grunt --no-color " + task + "  2>&1 \" ", false);
          }
          else
          {
            cmd.Checked = false;
          }
        }

        #endregion

        #region Gulp

        private bool IsGulpFile()
        {
          // gets the full path of the clicked file
          var path = SolutionHelpers.GetSourceFilePath();

          return ((path.ToLower().IndexOf("gulpfile.js") != -1) || (path.ToLower().IndexOf("gulpfile.ts") != -1) || (path.ToLower().IndexOf("gulpfile.coffee") != -1));
        }

        private void GulpBeforeQueryStatus(object sender, EventArgs e)
        {
            // gets the full path of the clicked file
            var path = SolutionHelpers.GetSourceFilePath();

            var myCommand = sender as OleMenuCommand;
            myCommand.Visible = this.IsGulpFile();


            if (!this.IsNewFile())
            {
                return;
            }

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
                this.lastFile = path;

                var list = GulpParser.ReadAllTasks(path);

                myCommand.Text = "Gulp";
                myCommand.Enabled = true;

                if (list.Count == 0)
                {
                    myCommand.Enabled = false;
                    myCommand.Text = "Gulpfile.js not found";
                }

                if (list.Contains("default"))
                {
                    list.Remove("default");
                }

                string n = Options.TaskRegex;

                Regex a = null;

                if (!string.IsNullOrEmpty(n))
                {
                    try
                    {
                        a = new Regex(n);
                    }
                    catch (Exception)
                    {
                        // invalid regex -> ignore
                    }

                }

                // creates the list of commands
                int j = 1;
                foreach (var ele in list)
                {
                    if (a != null)
                    {
                        if (a.Match(ele).Success)
                        {
                            continue;
                        }
                    }


                    CommandID menuCommandID = new CommandID(GuidList.guidGruntLauncherCmdSet, (int)PkgCmdIDList.cmdidGulpLauncher + j);
                    j++;
                    OleMenuCommand command = new OleMenuCommand(this.GulpCallback, menuCommandID);
                    command.Text = "Gulp: " + ele;
                    command.BeforeQueryStatus += (x, y) => { (x as OleMenuCommand).Visible = true; };
                    commands.Add(command);
                    mcs.AddCommand(command);
                }
            }
        }


        private void GulpCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var text = cmd.Text;
            var task = text.Substring(text.IndexOf(':') + 1).Trim();

            if (task == "Gulp") { task = ""; }

            // if the command is checked it means that there is a running grunt task associated
            // so we kill it
            if (cmd.Checked)
            {
                System.Diagnostics.Process pro;
                processes.TryGetValue(cmd, out pro);
                if (pro != null)
                {
                    OutputHelpers.Output("Stopping process " + cmd.Text);
                    ProcessHelpers.KillProcessAndChildren(pro.Id);
                    processes.Remove(cmd);
                }
            }

            if (!cmd.Checked)
            {
                // launches the grunt process and redirects the output to the output window
                RunProcess(cmd, " /c \"gulp --no-color " + task + "  2>&1 \" ", false);
            }
            else
            {
                cmd.Checked = false;
            }
        }

        #endregion

        /// <summary>
        ///     Determines if the solution explorer context menu has been opened on a new file since
        ///     last time
        /// </summary>
        /// <returns>Boolean that indicates if a new file was clicked</returns>
        private bool IsNewFile()
        {
          // gets the full path of the clicked file
          var path = SolutionHelpers.GetSourceFilePath();

          // optimization to avoid parsing the file again if the clicked file has not changed since last time
          if (path == this.lastFile)
          {
            return false;
          }

          return true;
        }

        private static void RunProcess(OleMenuCommand cmd, string argument, bool fromRoot)
        {
            dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = fromRoot ? SolutionHelpers.GetRootFolder(dte) : Path.GetDirectoryName(SolutionHelpers.GetSourceFilePath()),
                    FileName = "cmd",
                    Arguments = argument,
                };

                System.Diagnostics.Process proc = new System.Diagnostics.Process()
                {
                    StartInfo = procStartInfo,
                    EnableRaisingEvents = true
                };

                string command = cmd.Text;
                OutputHelpers.Output("Executing " + command + " \r\n\r\n", true);

                proc.OutputDataReceived += (object sendingProcess, DataReceivedEventArgs outLine) => OutputHelpers.Output(outLine.Data + "\r\n");
                proc.ErrorDataReceived += (object sendingProcess, DataReceivedEventArgs outLine) => OutputHelpers.Output(outLine.Data + "\r\n");
                proc.Exited += (x, y) =>
                {
                    processes.Remove(cmd);
                    cmd.Checked = false;
                    dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
                    OutputHelpers.Output("Execution of " + command + " completed.\r\n\r\n", true);
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                cmd.Checked = true;

                processes.Add(cmd, proc);
            }
            catch (Exception ex)
            {
                OutputHelpers.Output(ex.Message);
            }
        }
    }
}
