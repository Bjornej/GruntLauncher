namespace Bjornej.GruntLauncher
{
    using System;
    using System.Diagnostics;
    using System.Management;

    /// <summary>
    ///     Utility methods to manage the running processes
    /// </summary>
    public static class ProcessHelpers
    {
        /// <summary>
        ///     Kill a process, and all of its children.
        /// </summary>
        /// <param name="pid">Process ID</param>
        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }

            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
