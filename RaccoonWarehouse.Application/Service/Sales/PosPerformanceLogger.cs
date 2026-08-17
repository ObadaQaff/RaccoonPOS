using System;
using System.IO;

namespace RaccoonWarehouse.Application.Service.Sales
{
    public static class PosPerformanceLogger
    {
        private static readonly object Sync = new();

        public static string LogFilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ROCCOPOS",
            "Logs",
            "pos-performance.log");

        public static void Write(string operation, long stepMilliseconds, long totalMilliseconds)
        {
            try
            {
                lock (Sync)
                {
                    var directory = Path.GetDirectoryName(LogFilePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    File.AppendAllText(
                        LogFilePath,
                        $"{DateTimeOffset.Now:O}\t{operation}\tstep_ms={stepMilliseconds}\ttotal_ms={totalMilliseconds}{Environment.NewLine}");
                }
            }
            catch
            {
                // Performance diagnostics must never interrupt invoice creation.
            }
        }
    }
}
