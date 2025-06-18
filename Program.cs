using System;
using System.Diagnostics;
using System.IO;

class MinShim
{
    static void Main(string[] args)
    {
        // ---- 1. Parse -----------------------------------------------------------------
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ShimBuilder <targetExe> [shimName.exe]");
            Environment.Exit(1);
        }
        string target = Path.GetFullPath(args[0]);
        if (!File.Exists(target))
        {
            Console.WriteLine($"Target '{target}' not found."); Environment.Exit(2);
        }
        string shimExe = args.Length > 1
                         ? args[1]
                         : Path.ChangeExtension(Path.GetFileName(target), ".exe");

        // ---- 2. Emit 10-line stub source ----------------------------------------------
        string stub = @$"
using System;
using System.Diagnostics;

class S
{{
    static int Main(string[] a)
    {{
        var psi = new ProcessStartInfo
        {{
            FileName        = @""{target}"",
            Arguments       = string.Join("" "", a),
            UseShellExecute = false          // stay in this console
        }};

        var p = Process.Start(psi);
        if (p == null) return -1;
        p.WaitForExit();
        return p.ExitCode;
    }}
}}";
        string tmpSrc = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
        File.WriteAllText(tmpSrc, stub);

        // ---- 3. Find csc.exe (any .NET 4.x install) -----------------------------------
        string csc = Environment.ExpandEnvironmentVariables(
                       @"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe");
        if (!File.Exists(csc))
            csc = Environment.ExpandEnvironmentVariables(
                       @"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe");
        if (!File.Exists(csc))
        {
            Console.WriteLine("csc.exe not found; install .NET Framework 4.x"); Environment.Exit(3);
        }

        // ---- 4. Compile stub -----------------------------------------------------------
        var p = Process.Start(new ProcessStartInfo
        {
            FileName  = csc,
            Arguments = $"/nologo /optimize /target:exe /out:\"{shimExe}\" \"{tmpSrc}\"",
            UseShellExecute = false
        });
        p.WaitForExit();
        File.Delete(tmpSrc);

        Console.WriteLine($"[OK] Created shim '{shimExe}' -> '{target}'");
    }
}
