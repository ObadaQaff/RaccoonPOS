using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace ROCCOPOSSetup;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            var installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "ROCCOPOS");

            Directory.CreateDirectory(installDir);
            ExtractPayload(installDir);
            CreateDesktopShortcut(installDir);
            LaunchApp(installDir);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ROCCOPOS Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void ExtractPayload(string installDir)
    {
        var payloadName = typeof(Program).Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("ROCCOPOS-Payload.zip", StringComparison.OrdinalIgnoreCase));

        if (payloadName is null)
        {
            throw new InvalidOperationException("The installer payload was not found.");
        }

        using var payloadStream = typeof(Program).Assembly.GetManifestResourceStream(payloadName);
        if (payloadStream is null)
        {
            throw new InvalidOperationException("The installer payload stream could not be opened.");
        }

        using var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.Combine(installDir, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static void CreateDesktopShortcut(string installDir)
    {
        var shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "ROCCOPOS.lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = Path.Combine(installDir, "RaccoonWarehouse.exe");
        shortcut.WorkingDirectory = installDir;
        shortcut.IconLocation = shortcut.TargetPath;
        shortcut.Save();
    }

    private static void LaunchApp(string installDir)
    {
        var appPath = Path.Combine(installDir, "RaccoonWarehouse.exe");
        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException("The main application was not installed.", appPath);
        }

        Process.Start(new ProcessStartInfo(appPath)
        {
            WorkingDirectory = installDir,
            UseShellExecute = true
        });
    }
}
