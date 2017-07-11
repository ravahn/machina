// Machina ~ IPHelper.cs
// 
// Copyright © 2007 - 2017 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Machina.Models
{
    public static class IPHelper
    {
        #region Public Methods

        public static TCPTable GetExtendedTCPTable(bool sorted)
        {
            var tcpRows = new List<TCPRow>();
            var tcpTable = IntPtr.Zero;
            var tcpTableLength = 0;
            if (UnsafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, 2, UnsafeNativeMethods.TCP_TABLE_CLASS.OWNER_PID_ALL) == 0)
            {
                return new TCPTable(tcpRows);
            }
            try
            {
                tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                if (UnsafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, 2, UnsafeNativeMethods.TCP_TABLE_CLASS.OWNER_PID_ALL) == 0)
                {
                    var table = (UnsafeNativeMethods.TCPTable) Marshal.PtrToStructure(tcpTable, typeof(UnsafeNativeMethods.TCPTable));
                    var rowPtr = (IntPtr) ((long) tcpTable + Marshal.SizeOf(table.Length));
                    for (var i = 0; i < table.Length; ++i)
                    {
                        tcpRows.Add(new TCPRow((UnsafeNativeMethods.TCPRow) Marshal.PtrToStructure(rowPtr, typeof(UnsafeNativeMethods.TCPRow))));
                        rowPtr = (IntPtr) ((long) rowPtr + Marshal.SizeOf(typeof(UnsafeNativeMethods.TCPRow)));
                    }
                }
            }
            finally
            {
                if (tcpTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tcpTable);
                }
            }
            return new TCPTable(tcpRows);
        }

        #endregion
    }
}
