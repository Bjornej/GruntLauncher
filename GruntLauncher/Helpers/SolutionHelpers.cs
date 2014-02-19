namespace Bjornej.GruntLauncher.Helpers
{
    using System;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using EnvDTE80;
    using System.IO;

    /// <summary>
    ///     Static helper methods to interact with the visual studio open solution
    /// </summary>
    public static class SolutionHelpers
    {
        /// <summary>
        /// Gets the extensibility object
        /// </summary>
        /// <returns>The estenzibility object</returns>
        public static EnvDTE80.DTE2 GetDTE2()
        {
            return Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        /// <summary>
        /// Get the full path of the file clicked upon when opening the contextual menu 
        /// </summary>
        /// <returns>The full path of the current file</returns>
        public static string GetSourceFilePath()
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

        public static string GetRootFolder(DTE2 dte)
        {
            Project project = GetActiveProject(dte);

            if (project == null)
                return null;

            string path = project.Properties.Item("FullPath").Value.ToString();
            
            if (Directory.Exists(path))
                return path;

            return Path.GetDirectoryName(path);
        }

        private static Project GetActiveProject(DTE2 dte)
        {
            try
            {
                Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
