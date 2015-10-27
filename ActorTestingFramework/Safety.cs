using System.Diagnostics;

namespace ActorTestingFramework
{
    public static class Safety
    {
        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
        }
    }
}