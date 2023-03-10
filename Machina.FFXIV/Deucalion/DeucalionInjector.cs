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
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern uint GetSecurityInfo(IntPtr handle, SE_OBJECT_TYPE ObjectType, SECURITY_INFORMATION SecurityInfo, IntPtr pSidOwner, 
            IntPtr pSidGroup, out IntPtr pDacl, IntPtr pSacl, out IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi,
          SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern uint SetSecurityInfoByHandle(IntPtr handle, SE_OBJECT_TYPE objectType, SECURITY_INFORMATION securityInformation,
          IntPtr psidOwner, IntPtr psidGroup, IntPtr pDacl, IntPtr pSacl);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_TERMINATE = 0x00000001,
            PROCESS_CREATE_THREAD = 0x00000002,
            PROCESS_VM_OPERATION = 0x00000008,
            PROCESS_VM_READ = 0x00000010,
            PROCESS_VM_WRITE = 0x00000020,
            PROCESS_DUP_HANDLE = 0x00000040,
            PROCESS_CREATE_PROCESS = 0x000000080,
            PROCESS_SET_QUOTA = 0x00000100,
            PROCESS_SET_INFORMATION = 0x00000200,
            PROCESS_QUERY_INFORMATION = 0x00000400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000,
            SYNCHRONIZE = 0x00100000,
            WRITE_DAC = 0x00040000,
            READ_CONTROL = 0x00020000,
            DELETE = 0x00010000,
            WRITE_OWNER = 0x00080000
        }

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;


        private enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }
        private enum SECURITY_INFORMATION : uint
        {
            OWNER_SECURITY_INFORMATION = 1,
            GROUP_SECURITY_INFORMATION = 2,
            DACL_SECURITY_INFORMATION = 4,
            SACL_SECURITY_INFORMATION = 8,
            UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
            UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
            PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
            PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000
        }

        private struct SECURITY_DESCRIPTOR
        {
            public byte Revision;
            public byte Sbz1;
            public short Control;
            public IntPtr Owner;
            public IntPtr Group;
            public IntPtr Sacl;
            public IntPtr Dacl;
        }
        #endregion

        public static string ExtractLibrary()
        {
            string fileName = Path.Combine(Path.GetTempPath(), "Machina.FFXIV", "deucalion-release.dll");
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

            UpdateProcessDACL(processId);

            // Get process handle for specified process id
            IntPtr procHandle = OpenProcess((uint)(ProcessAccessFlags.PROCESS_CREATE_THREAD | ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessAccessFlags.PROCESS_VM_OPERATION | ProcessAccessFlags.PROCESS_VM_WRITE | ProcessAccessFlags.PROCESS_VM_READ), 
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
            if (threadResult == IntPtr.Zero)
            {
                Trace.WriteLine($"DeucalionInjector: Unable to start remote thread in process id {processId}.", "DEBUG-MACHINA");
                return false;
            }

            Trace.WriteLine($"DeucalionInjector: Successfully injected Deucalion dll into process id {processId}.", "DEBUG-MACHINA");

            return true;
        }

        private static unsafe bool UpdateProcessDACL(int processId)
        {
            IntPtr pSecurityDescriptor = IntPtr.Zero;
            IntPtr procHandle = IntPtr.Zero;

            try
            {

                // Get process handle for specified process id
                procHandle = OpenProcess((uint)(ProcessAccessFlags.PROCESS_QUERY_LIMITED_INFORMATION | ProcessAccessFlags.WRITE_DAC | ProcessAccessFlags.READ_CONTROL),
                    false,
                    processId);
                if (procHandle == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to call limited OpenProcess on process id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                uint result = GetSecurityInfo(Process.GetCurrentProcess().Handle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, IntPtr.Zero, IntPtr.Zero, out IntPtr dacl, IntPtr.Zero, out pSecurityDescriptor);
                if (result != 0 || pSecurityDescriptor == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to query security info from process {processId}.", "DEBUG-MACHINA");
                    return false;
                }


                //IntPtr dacl = ((SECURITY_DESCRIPTOR*)pSecurityDescriptor)->Dacl;
                if (dacl == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: DACL struct is null for process id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                result = SetSecurityInfoByHandle(procHandle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION, IntPtr.Zero, IntPtr.Zero, dacl, IntPtr.Zero);
                if (result != 0)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to query security info from process {processId}.", "DEBUG-MACHINA");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"DeucalionInjector: Exception while updating Process DACL for process {processId}.  {ex}", "DEBUG-MACHINA");
                return false;
            }
            finally
            {
                if (pSecurityDescriptor != IntPtr.Zero)
                    _ = LocalFree(pSecurityDescriptor);

                if (procHandle != IntPtr.Zero)
                    _ = CloseHandle(procHandle);
            }

            return true;
        }
    }
}
