using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;

namespace dxvk_switch
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var appdir = path.Substring(0, path.LastIndexOf('/'));
            var versdir = Path.Join(appdir, "versions");

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dxvk-switch version[/commands]");
                if (!Directory.Exists(versdir))
                {
                    Console.WriteLine("No installed versions found, put DXVK versions into {0}", appdir + "/versions");
                    return;
                }
                Console.WriteLine(" Available versions:");
                var dirs = Directory.GetDirectories(versdir);
                foreach (var d in dirs)
                    Console.WriteLine("  {0}", d.Substring(d.LastIndexOf('/') + 1));
                return;
            }

            var prefix = Environment.GetEnvironmentVariable("WINEPREFIX");
            if (prefix == null)
            {
                Console.WriteLine("Wine prefix is not found, did you install wine (Hint: if you did run 'wine' command at least once)");
                return;
            }
            var win = Path.Join(prefix, "drive_c", "windows");
            var sys32 = Path.Join(win, "system32");
            var sys64 = Path.Join(win, "syswow64");

            var ver = Path.Join(versdir, args[0]);
            var versplit = args[0].Split('/', StringSplitOptions.None);
            if (versplit.Length > 1)
            {
                ver = Path.Join(versdir, versplit[0]);
                if (string.IsNullOrWhiteSpace(versplit[0]))
                {
                    var cmd = versplit[1];
                    if (string.IsNullOrEmpty(cmd)) return;
                    if (cmd.Contains('r'))
                    {
                        Console.WriteLine("DXVKSW: Trying to revert from backup...");
                        Restore(sys32, sys64);
                    }
                    if (cmd.Contains('R'))
                    {
                        Console.WriteLine("DXVKSW: Rebooting wine...");
                        Process.Start("wineboot", "-r").WaitForExit();
                    }
                    if (cmd.Contains('s'))
                    {
                        Console.WriteLine("DXVKSW: Shutting down wine...");
                        Process.Start("wineboot", "-s").WaitForExit();
                    }
                    if (cmd.Contains('u'))
                    {
                        Console.WriteLine("DXVKSW: Updating wineprefix...");
                        Process.Start("wineboot", "-u").WaitForExit();
                    }

                    return;
                }
            }
            var app = args[1];

            if (!Directory.Exists(ver))
            {
                Console.WriteLine("DXVKSW: Version '{0}' does not exists, cannot switch", ver);
                return;
            }

            var verenv = Path.Join(ver, "env");
            var versh = Path.Join(ver, "shell");

            if (File.Exists(verenv))
            {
                Console.WriteLine("DXVKSW: Adding version specific environment variables");
                var envread = File.ReadAllText(verenv);
                var splitlines = envread.Split(new[]{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitlines)
                {
                    var trim = line.Trim();
                    if (trim.StartsWith('#')) continue;
                    var trimsplit = trim.Split(new[]{ ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (trimsplit.Length != 2)
                    {
                        Console.WriteLine("DXVKSW: Invalid environment variable: '{0}'", trim);
                        continue;
                    }
                    Environment.SetEnvironmentVariable(trimsplit[0], trimsplit[1]);
                }
            }

            if (versplit.Length > 1)
            {
                var cmd = versplit[1];
                if (string.IsNullOrEmpty(cmd)) return;
                if (cmd.Contains('r'))
                {
                    Console.WriteLine("DXVKSW: Trying to revert from backup...");
                    Restore(sys32, sys64);
                }
            }

            Backup(sys32, sys64);
            Console.WriteLine("DXVKSW: Switching to DXVK version: {0}", args[0]);
            Switch(ver, sys32, sys64);
            Console.WriteLine("DXVKSW: Sleeping 50ms to ensure switching of versions.");
            System.Threading.Thread.Sleep(50);

            if (versplit.Length > 1)
            {
                var cmd = versplit[1];
                if (string.IsNullOrEmpty(cmd)) return;
                if (cmd.Contains('r'))
                {
                    Console.WriteLine("DXVKSW: Trying to revert from backup...");
                    Restore(sys32, sys64);
                }
                if (cmd.Contains('R'))
                {
                    Console.WriteLine("DXVKSW: Rebooting wine...");
                    Process.Start("wineboot", "-r").WaitForExit();
                }
                if (cmd.Contains('s'))
                {
                    Console.WriteLine("DXVKSW: Shutting down wine...");
                    Process.Start("wineboot", "-s").WaitForExit();
                }
                if (cmd.Contains('u'))
                {
                    Console.WriteLine("DXVKSW: Updating wineprefix...");
                    Process.Start("wineboot", "-u").WaitForExit();
                }
            }

            var arglist = "";
            for (int i = 2; i < args.Length; i++)
            {   
                if (args[i].Contains(' '))
                    args[i] = $"\"{ args[i] }\"";
                arglist += args[i] + " ";
            }
            Console.WriteLine("DXVKSW: Command: {0} {1}", app, arglist);
            var startinfo = new ProcessStartInfo(app, arglist);
            var process = Process.Start(startinfo);
            process.WaitForExit();

            if (versplit.Length > 1)
            {
                var cmd = versplit[1];
                if (string.IsNullOrEmpty(cmd)) return;
                if (cmd.Contains('!'))
                {
                    Console.WriteLine("DXVKSW: No-revert is specified, not restoring original state. Use /r to revert to originals from backup.");
                    return;
                }
            }

            Console.WriteLine("DXVKSW: Done, restoring original state", args[0]);
            Restore(sys32, sys64);
        }

        static string[] Files = {
            "d3d9.dll", "d3d10core.dll", "d3d11.dll", "dxgi.dll"
        };

        public static void Backup(string sys32, string sys64)
        {
            foreach (var f in Files)
            {
                File.Copy(Path.Join(sys32, f), Path.Join(sys32, f + ".switch"), true);
                File.Copy(Path.Join(sys64, f), Path.Join(sys64, f + ".switch"), true);
            }
        }

        public static void Restore(string sys32, string sys64)
        {
            foreach (var f in Files)
            {
                var f32 = Path.Join(sys32, f + ".switch");
                var f64 = Path.Join(sys64, f + ".switch");
                if (File.Exists(f32))
                    File.Move(Path.Join(sys32, f + ".switch"), Path.Join(sys32, f), true);
                if (File.Exists(f64))
                    File.Move(Path.Join(sys64, f + ".switch"), Path.Join(sys64, f), true);
            }
        }

        public static void Switch(string ver, string sys32, string sys64)
        {
            foreach (var f in Files)
            {
                File.Copy(Path.Join(ver, "x64", f), Path.Join(sys32, f), true);
                File.Copy(Path.Join(ver, "x32", f), Path.Join(sys64, f), true);
            }
        }
    }
}
