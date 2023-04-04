// Copyright © 2023 Ravahn - All Rights Reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see<http://www.gnu.org/licenses/>.
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
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [Flags]
        private enum ProcessAccess : uint
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

        private enum MemoryProtection : uint
        {
            PAGE_READWRITE = 4,
            MEM_COMMIT = 0x00001000,
            MEM_RESERVE = 0x00002000,
        }


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

        #endregion

        private static readonly string _resourceFileName = "deucalion-0.9.3.dll";
        public static string ExtractLibrary()
        {
            string fileName = Path.Combine(Path.GetTempPath(), "Machina.FFXIV", _resourceFileName);
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception)
                {
                    // do nothing - file may be locked by ffxiv process.
                }
            }

            if (!File.Exists(fileName))
            {

                if (!Directory.Exists(fileName.Substring(0, fileName.LastIndexOf("\\", StringComparison.Ordinal) + 1)))
                    _ = Directory.CreateDirectory(fileName.Substring(0, fileName.LastIndexOf("\\", StringComparison.Ordinal) + 1));

                string resourceName = $"Machina.FFXIV.Deucalion.Distrib.{_resourceFileName}";
                using (Stream s = typeof(DeucalionInjector).Module.Assembly.GetManifestResourceStream(resourceName))
                {
                    using (BinaryReader sr = new BinaryReader(s))
                    {
                        byte[] fileData = sr.ReadBytes((int)s.Length);
                        File.WriteAllBytes(fileName, fileData);
                    }
                }
            }

            string release_checksum = "16-99-AB-21-7A-1C-BB-8D-E8-7A-37-08-3F-A1-EA-A8-17-60-BE-A4-03-B5-B5-A8-CC-BD-E2-2A-C0-0C-C8-BC";

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
            using (SHA256 hashAlgo = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    return hashAlgo.ComputeHash(stream);
                }
            }
        }
        public static bool InjectLibrary(int processId, string deucalionPath)
        {
            if (!File.Exists(deucalionPath))
            {
                Trace.WriteLine($"DeucalionInjector: Cannot find the Deucalion library at {deucalionPath}.", "DEBUG-MACHINA");
                return false;
            }

            // Note: if this method does not work, do not stop processing.  If user runs with elevated permissions, injection will still work.
            _ = UpdateProcessDACL(processId);

            IntPtr procHandle = IntPtr.Zero;
            IntPtr threadHandle = IntPtr.Zero;
            try
            {
                // Get process handle for specified process id
                procHandle = OpenProcess((uint)(ProcessAccess.PROCESS_CREATE_THREAD | ProcessAccess.PROCESS_QUERY_INFORMATION | ProcessAccess.PROCESS_VM_OPERATION | ProcessAccess.PROCESS_VM_WRITE | ProcessAccess.PROCESS_VM_READ),
                    false,
                    processId);
                if (procHandle == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to call OpenProcess with id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                // get local address for kernel32.dll LoadLibraryA method.  This assumes remote process has same address.
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                    return false;

                byte[] filenameBytes = Encoding.Unicode.GetBytes(deucalionPath);

                // Allocate memory in remote process to load the DLL name
                IntPtr allocMemAddress = VirtualAllocEx(
                    procHandle,
                    IntPtr.Zero,
                    (uint)(filenameBytes.Length + 2),
                    (uint)(MemoryProtection.MEM_COMMIT | MemoryProtection.MEM_RESERVE), (uint)MemoryProtection.PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to allocate memory in process id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                // Write file name to remote process
                bool result = WriteProcessMemory(procHandle,
                    allocMemAddress,
                    filenameBytes,
                    (uint)(filenameBytes.Length + 2),
                    out UIntPtr bytesWritten);
                if (result == false || bytesWritten == UIntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to write filename to memory in process id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                // Create and start remote thread in process to call LoadLibraryA with the dll name as the argument
                threadHandle = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
                if (threadHandle == IntPtr.Zero)
                {
                    Trace.WriteLine($"DeucalionInjector: Unable to start remote thread in process id {processId}.", "DEBUG-MACHINA");
                    return false;
                }

                Trace.WriteLine($"DeucalionInjector: Successfully injected Deucalion dll into process id {processId}.", "DEBUG-MACHINA");

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"DeucalionInjector: Unexpected error in Injectlibrary for {processId}: {ex}", "DEBUG-MACHINA");
                return false;
            }
            finally
            {
                if (procHandle != IntPtr.Zero)
                    _ = CloseHandle(procHandle);

                if (threadHandle != IntPtr.Zero)
                    _ = CloseHandle(threadHandle);
            }
        }

        private static unsafe bool UpdateProcessDACL(int processId)
        {
            IntPtr pSecurityDescriptor = IntPtr.Zero;
            IntPtr procHandle = IntPtr.Zero;

            try
            {

                // Get process handle for specified process id
                procHandle = OpenProcess((uint)(ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION | ProcessAccess.WRITE_DAC | ProcessAccess.READ_CONTROL),
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
