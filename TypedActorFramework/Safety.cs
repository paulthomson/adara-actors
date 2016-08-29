using System.Diagnostics;
using JetBrains.Annotations;

namespace TypedActorFramework
{
    internal static class Safety
    {
        [ContractAnnotation("condition:false => halt")]
        public static void Assert(bool condition, string message)
        {
            Trace.Assert(condition, message);
        }

        [ContractAnnotation("condition:false => halt")]
        public static void Assert(bool condition)
        {
            Trace.Assert(condition);
        }
    }
}