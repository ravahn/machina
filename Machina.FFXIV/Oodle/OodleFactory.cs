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
    public static class OodleFactory
    {
        private static IOodleNative _oodleNative;
        private static readonly object _lock = new object();

        public static void SetImplementation(OodleImplementation implementation, string path)
        {
            lock (_lock)
            {
                // Note: Do not re-initialize if not changing implementation type.
                if (implementation == OodleImplementation.Library)
                {
                    if (!(_oodleNative is OodleNative_Library))
                        _oodleNative?.UnInitialize();
                    else
                        return;
                    _oodleNative = new OodleNative_Library();
                }
                else
                {
                    if (!(_oodleNative is OodleNative_Ffxiv))
                        _oodleNative?.UnInitialize();
                    else
                        return;
                    _oodleNative = new OodleNative_Ffxiv();
                }
                _oodleNative.Initialize(path);
            }
        }

        public static Oodle Create()
        {
            lock (_lock)
            {
                if (_oodleNative is null)
                    return null;
                return new Oodle(_oodleNative);
            }
        }
    }
}
