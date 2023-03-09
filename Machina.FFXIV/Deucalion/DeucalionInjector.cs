using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Machina.FFXIV.Deucalion
{
    public static class DeucalionInjector
    {
        #region native functions
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;
        #endregion

        public static string ExtractLibrary()
        {
            string fileName = Path.Combine(Path.GetTempPath(), "Machina.FFXIV", "deutalion-release.dll");
            if (!File.Exists(fileName))
            {

                if (!Directory.Exists(fileName.Substring(0, fileName.LastIndexOf("\\") + 1)))
                    Directory.CreateDirectory(fileName.Substring(0, fileName.LastIndexOf("\\") + 1));

                string resourceName = $"Machina.FFXIV.Deucalion.Distrib.deucalion-release.dll";
                using (Stream s = typeof(DeucalionInjector).Module.Assembly.GetManifestResourceStream(resourceName))
                {
                    using (BinaryReader sr = new BinaryReader(s))
                    {
                        byte[] fileData = sr.ReadBytes((int)s.Length);
                        File.WriteAllBytes(fileName, fileData);
                    }
                }
            }

            string release_checksum = "1B-9B-7A-E7-5F-C1-A7-05-40-AD-60-C7-62-77-01-36";

            // validate checksum
            byte[] checksum = CalculateChecksum(fileName);
            if (BitConverter.ToString(checksum) != release_checksum)
            {
                Trace.WriteLine($"DeucalionInjector: File checksum is invalid, cannot inject dll at {fileName}", "DEBUG-MACHINA");
                return string.Empty;
            }
            return fileName;
        }

        public static byte[] CalculateChecksum(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }
        public static bool InjectLibrary(int processId, string deucalionPath)
        {
            if (!System.IO.File.Exists(deucalionPath))
            {
                Trace.WriteLine($"DeucalionInjector: Cannot find the Deucalion library at {deucalionPath}.", "DEBUG-MACHINA");
                return false;
            }

            // Get process handle for specified process id
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, 
                false, 
                processId);
            if (procHandle == IntPtr.Zero)
            {
                Trace.WriteLine($"DeucalionInjector: Unable to call OpenProcess with id {processId}.", "DEBUG-MACHINA");
                return false;
            }

            // get local address for kernel32.dll LoadLibraryA method.  This assumes remote process has same address.
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
                return false;

            // Allocate memory in remote process to load the DLL name
            IntPtr allocMemAddress = VirtualAllocEx(
                procHandle, 
                IntPtr.Zero, 
                (uint)((deucalionPath.Length + 1) * Marshal.SizeOf(typeof(char))),
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
                Trace.WriteLine($"DeucalionInjector: Unable to allocate memory in process id {processId}.", "DEBUG-MACHINA");
                return false;
            }

            // Write file name to remote process
            bool result = WriteProcessMemory(procHandle, 
                allocMemAddress, 
                Encoding.Default.GetBytes(deucalionPath), 
                (uint)((deucalionPath.Length + 1) * Marshal.SizeOf(typeof(char))), 
                out UIntPtr bytesWritten);
            if (result == false || bytesWritten == UIntPtr.Zero)
            {
                Trace.WriteLine($"DeucalionInjector: Unable to write filename to memory in process id {processId}.", "DEBUG-MACHINA");
                return false;
            }

            // Create and start remote thread in process to call LoadLibraryA with the dll name as the argument
            IntPtr threadResult = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            if (threadResult== IntPtr.Zero)
            {
                Trace.WriteLine($"DeucalionInjector: Unable to start remote thread in process id {processId}.", "DEBUG-MACHINA");
                return false;
            }

            return true;
        }
    }
}
