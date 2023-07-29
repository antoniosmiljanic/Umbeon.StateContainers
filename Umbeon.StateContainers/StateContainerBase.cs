using System;
using System.Runtime.CompilerServices;

namespace Umbeon.StateContainers
{
    public abstract class StateContainerBase
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        protected sealed class StateContainerFieldAttribute : Attribute
        {
        }

        public void NotifyValueChanged(string fieldName, [CallerMemberName] string caller = null)
        {
            throw new NotImplementedException();
        }
    }
}