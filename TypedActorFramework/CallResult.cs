using System;

namespace TypedActorFramework
{
    public class CallResult<T>
    {
        public T result;
        public Exception exception;

        #region Overrides of Object

        public override string ToString()
        {
            return nameof(CallResult<object>);
        }

        #endregion
    }
}