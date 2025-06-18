using System;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

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
            Console.WriteLine(string.Format("Target '{0}' not found.", target));
            Environment.Exit(2);
        }
        string shimExe = args.Length > 1
                         ? args[1]
                         : Path.ChangeExtension(Path.GetFileName(target), ".exe");

        // ---- 2. Emit 10-line stub source ----------------------------------------------
        string quotedTarget = "@\"" + target.Replace("\"", "\"\"") + "\"";
        string stub = $@"
using System;
using System.Diagnostics;
using System.ComponentModel;

class S
{{
    static int Main(string[] a)
    {{
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = {quotedTarget};
        psi.Arguments = string.Join("" "", a);
        psi.UseShellExecute = false;
        Process p = null;
        try
        {{
            p = Process.Start(psi);
            if (p == null)
            {{
                return -1;
            }}
            p.WaitForExit();
            return p.ExitCode;
        }}
        catch (Win32Exception ex)
        {{
            if (ex.NativeErrorCode != 2) // ERROR_FILE_NOT_FOUND
            {{
                throw;
            }}
            Console.Error.WriteLine(""[ERROR] Target executable not found: "" + {quotedTarget});
            return 100;
        }}
        catch (Exception ex)
        {{
            Console.Error.WriteLine(""[ERROR] Failed to start target: "" + {quotedTarget} + "" - "" + ex.Message);
            return 101;
        }}
        finally
        {{
            if (p != null)
            {{
                p.Dispose();
            }}
        }}
    }}
}}
";

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
            Console.WriteLine("csc.exe not found; install .NET Framework 4.x");
            Environment.Exit(3);
        }

        // ---- 4. Compile stub -----------------------------------------------------------
        var p = Process.Start(new ProcessStartInfo
        {
            FileName  = csc,
            Arguments = string.Format("/nologo /optimize /target:exe /out:\"{0}\" \"{1}\"", shimExe, tmpSrc),
            UseShellExecute = false
        });
        p?.WaitForExit();
        File.Delete(tmpSrc);

        Console.WriteLine(string.Format("[OK] Created shim '{0}' -> '{1}'", shimExe, target));
    }
}
