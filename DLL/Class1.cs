using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace DLL;

// Compile : dotnet publish -r win-x64 -c Release

public class _DLL
{
    [DllImport("user32.dll")]
    public static extern int MessageBox(int hWnd, string text, string caption, uint type);

    private const uint DLL_PROCESS_DETACH = 0,
                           DLL_PROCESS_ATTACH = 1,
                           DLL_THREAD_ATTACH = 2,
                           DLL_THREAD_DETACH = 3;

    [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static bool DllMain(IntPtr hModule, uint ul_reason_for_call, IntPtr lpReserved)
    {
        if (ul_reason_for_call == DLL_PROCESS_ATTACH)
        {
            MessageBox(0, "Injected", "hecker", 0);
        }

        return true;
    }

}
