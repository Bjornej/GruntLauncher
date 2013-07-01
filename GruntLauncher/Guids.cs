// Guids.cs
// MUST match guids.h
using System;

namespace Bjornej.GruntLauncher
{
    static class GuidList
    {
        public const string guidGruntLauncherPkgString = "cced4e72-2f8c-4458-b8df-4934677e4bf3";
        public const string guidGruntLauncherCmdSetString = "59ce41a7-3da6-41c2-bf26-48dac265cbae";

        public static readonly Guid guidGruntLauncherCmdSet = new Guid(guidGruntLauncherCmdSetString);
    };
}