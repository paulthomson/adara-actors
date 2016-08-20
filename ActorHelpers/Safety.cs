using System.Diagnostics;
using JetBrains.Annotations;

namespace ActorHelpers
{
    public static class Safety
    {
        [ContractAnnotation("condition:false => halt")]
        public static void Assert(bool condition)
        {
            Trace.Assert(condition);
        }
    }
}