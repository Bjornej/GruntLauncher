namespace Bjornej.GruntLauncher.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    ///     Helper methods to write to the VS output window
    /// </summary>
    public static class OutputHelpers
    {
        /// <summary>
        ///     Window pane used to show grunt output
        /// </summary>
        private static IVsOutputWindowPane outputWindowPane;

        /// <summary>
        ///     Prints a string to the Output window in a custom pane
        /// </summary>
        /// <param name="msg">The string to print</param>
        /// <param name="focus">Decides if the output pane should be focused</param>
        public static void Output(string msg, bool focus = false)
        {
            if (outputWindowPane == null)
            {
                Init();
            }

            if (focus)
            {
                var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                Window window = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                window.Visible = true;
                window.Activate();
                outputWindowPane.Activate();
            }

            // Output the message
            outputWindowPane.OutputString(msg);
        }

        /// <summary>
        ///     Initializes the output window
        /// </summary>
        private static void Init()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            Window window = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            window.Visible = true;

            // Ensure that the desired pane is visible
            var paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;

            outputWindow.CreatePane(paneGuid, "Grunt execution", 1, 0);
            outputWindow.GetPane(paneGuid, out outputWindowPane);
        }
    }
}
