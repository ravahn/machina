// Copyright © 2021 Ravahn - All Rights Reserved
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

namespace Machina.FFXIV.Oodle
{
    public enum OodleImplementation
    {
        /// <summary>
        /// Udp Network protocol, loads ffxiv_dx11.exe into process memory and invokes the Oodle library functions
        /// </summary>
        FfxivUdp = 1,

        /// <summary>
        /// Udp network protocol, loads an oodle dll such as oo2net_9_win64.dll and invokes the Oodle library functions
        /// </summary>
        LibraryUdp = 2,

        /// <summary>
        /// Default.  Tcp Network protocol, loads ffxiv_dx11.exe into process memory and invokes the Oodle library functions
        /// </summary>
        FfxivTcp = 3,

        /// <summary>
        /// Tcp network protocol, loads an oodle dll such as oo2net_9_win64.dll and invokes the Oodle library functions
        /// </summary>
        LibraryTcp = 4,

        /// <summary>
        /// Default.  Tcp Network protocol, loads Korean version of ffxiv_dx11.exe into process memory and invokes the Oodle library functions.
        ///   Note: this is the only version that works with the Korean game client.
        /// </summary>
        KoreanFfxivUdp = 5
    }
}
