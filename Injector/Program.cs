using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static CSharp_Native_Injector.WinApi;

class Program 
{
    const string target_process = "wordpad"; // Without .exe
    const string dll_path = @"C:\Users\Salaheddine\Desktop\dll\DLL\bin\Release\net9.0\win-x64\native\DLL.dll"; // Full path of dll
    const string dll_name = "DLL.dll"; // Dll file name

    private static IntPtr GetRemoteModule(Process proc, string name)
    {
        ProcessModuleCollection modules = proc.Modules;

        foreach (ProcessModule module in modules)
        {
            if (name.ToLower() == module.ModuleName.ToLower())
                return module.BaseAddress;
        }

        return IntPtr.Zero;
    }

    public static unsafe void Main() 
    {
        try
        {
            Process Handle = Process.GetProcessesByName(target_process).FirstOrDefault();
            if (Handle.Handle == IntPtr.Zero || Handle.Handle == null) throw new Exception($"Target process is offline.");

            int PID = Handle.Id;

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, PID);
            if (hProcess == IntPtr.Zero || hProcess == null) throw new Exception($"Failed to open process with error code {Marshal.GetLastWin32Error()}");

            IntPtr hKernel32 = GetModuleHandle("kernel32.dll");
            if (hKernel32 == IntPtr.Zero || hKernel32 == null) throw new Exception($"Failed to load kernel32.dll with error code {Marshal.GetLastWin32Error()}");

            IntPtr LoadLibraryAddy = GetProcAddress(hKernel32, "LoadLibraryA");
            if (LoadLibraryAddy == IntPtr.Zero || LoadLibraryAddy == null) throw new Exception($"Failed to load 'LoadLibraryA' with error code {Marshal.GetLastWin32Error()}");

            IntPtr LoadLibraryOffset = LoadLibraryAddy - hKernel32;

            IntPtr RemoteKernel32 = GetRemoteModule(Handle, "kernel32.dll");
            if (RemoteKernel32 == IntPtr.Zero || RemoteKernel32 == null) throw new Exception($"Failed to get remote address of kernel32.dll with error code {Marshal.GetLastWin32Error()}");

            LoadLibraryAddy = RemoteKernel32 + LoadLibraryOffset;

            IntPtr hFile = LoadLibraryEx(dll_path, 0, DONT_RESOLVE_DLL_REFERENCES);

            IntPtr dllMainLocal = GetProcAddress(hFile, "DllMain");
            if (dllMainLocal == IntPtr.Zero || dllMainLocal == null) throw new Exception($"Failed to get address of DllMain function with error code {Marshal.GetLastWin32Error()}");

            IntPtr dllMainOffset = dllMainLocal - hFile;

            nint RemoteDllPath = VirtualAllocEx(hProcess, nint.Zero, (uint)(dll_path.Length + 1), (uint)(AllocationType.Commit | AllocationType.Reserve), PAGE_EXECUTE_READWRITE);
            if (RemoteDllPath == nint.Zero || RemoteDllPath == null) throw new Exception($"Failed to allocate memory on the target process with error code {Marshal.GetLastWin32Error()}");
            if (!WriteProcessMemory(hProcess, RemoteDllPath, (void*)Marshal.StringToHGlobalAnsi(dll_path), (uint)(dll_path.Length + 1), null)) throw new Exception($"Failed to write memory on the target process with error code {Marshal.GetLastWin32Error()}");

            IntPtr hThread = CreateRemoteThread(hProcess, (nint)null, 0, LoadLibraryAddy, RemoteDllPath, 0, (nint)null);
            if (hThread == IntPtr.Zero || hThread == null) throw new Exception($"Failed to create thread on remote process with error code {Marshal.GetLastWin32Error()}");

            WaitForSingleObject(hThread, INFINITE);

            IntPtr RemoteDll = GetRemoteModule(Handle, dll_name);
            if (RemoteDll == IntPtr.Zero || RemoteDll == null) throw new Exception($"Failed to get remote address of {dll_name} with error code {Marshal.GetLastWin32Error()}");

            IntPtr dllMainRemote = RemoteDll + dllMainOffset;

            hThread = CreateRemoteThread(hProcess, (nint)null, 4096, dllMainRemote, 0, 0, (nint)null);
            if (hThread == IntPtr.Zero || hThread == null) throw new Exception($"Failed to create thread (2) on remote process with error code {Marshal.GetLastWin32Error()}");

            WaitForSingleObject(hThread, INFINITE);

            Handle.Dispose();
            VirtualFreeEx(hProcess, RemoteDllPath, (uint)(dll_path.Length + 1), (uint)AllocationType.Release);
            CloseHandle(hThread);
            CloseHandle(hProcess);

            Console.WriteLine("DLL Injected!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}