using System;
using System.Collections;
using System.Text;

namespace Machina
{
    using System;
    using System.Collections;
    using System.Dynamic;
    using System.Reflection;

    // A small wrapper around COM interop to make it easier to use.
    //  thanks to users on this github issue for the original code: https://github.com/dotnet/runtime/issues/12587
    internal class COMObject : DynamicObject
    {
        public readonly object instance;

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
                new object[] { }
            );

            if (result != null && !result.GetType().IsValueType && result.GetType() != typeof(string))
                result = new COMObject(result);

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            instance.GetType().InvokeMember(
                binder.Name,
                BindingFlags.SetProperty,
                Type.DefaultBinder,
                instance,
                new object[] { value }
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
                args
            );
            return true;
        }

        public IEnumerator GetEnumerator()
        {
            if (instance as IEnumerable != null)
                return (instance as IEnumerable).GetEnumerator();

            throw new NotImplementedException("COM object doesnt support IEnumerable.");
        }
    }
}
