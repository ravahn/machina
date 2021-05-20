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

using System;
using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Reflection;

namespace Machina
{
    // A small wrapper around COM interop to make it easier to use.
    //  thanks to users on this github issue for the original code: https://github.com/dotnet/runtime/issues/12587
    internal class COMObject : DynamicObject
    {
        private readonly object instance;

        public static COMObject CreateObject(string progID)
        {
            return new COMObject(Activator.CreateInstance(Type.GetTypeFromProgID(progID, true)));
        }

        public COMObject(object instance)
        {
            this.instance = instance;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = instance.GetType().InvokeMember(
                binder.Name,
                BindingFlags.GetProperty,
                Type.DefaultBinder,
                instance,
                Array.Empty<object>(),
                CultureInfo.InvariantCulture
            );

            if (result != null && !result.GetType().IsValueType && result.GetType() != typeof(string))
                result = new COMObject(result);

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _ = instance.GetType().InvokeMember(
                binder.Name,
                BindingFlags.SetProperty,
                Type.DefaultBinder,
                instance,
                new object[] { value },
                CultureInfo.InvariantCulture
            );
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = instance.GetType().InvokeMember(
                binder.Name,
                BindingFlags.InvokeMethod,
                Type.DefaultBinder,
                instance,
                args,
                CultureInfo.InvariantCulture
            );
            return true;
        }

        public IEnumerator GetEnumerator()
        {
            return (instance as IEnumerable) != null
                ? (instance as IEnumerable).GetEnumerator()
                : throw new NotImplementedException("COM object doesnt support IEnumerable.");
        }
    }
}
