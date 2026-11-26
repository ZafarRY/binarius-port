using System.IO;

namespace Binarius.Nikki.Shims
{
    /// <summary>
    /// Stands in for System.Diagnostics.Process inside the Nikki/Endscript sources.
    /// The original code only uses Process.GetCurrentProcess().MainModule.FileName to find a
    /// folder next to the running exe for temporary files. On Android that folder is read-only,
    /// so the temp folder of the app is used instead.
    /// </summary>
    internal static class Process
    {
        public static CurrentProcess GetCurrentProcess() => new CurrentProcess();
    }

    internal sealed class CurrentProcess
    {
        public CurrentModule MainModule => new CurrentModule();
    }

    internal sealed class CurrentModule
    {
        public string FileName => Path.Combine(Path.GetTempPath(), "binarius.exe");
    }
}