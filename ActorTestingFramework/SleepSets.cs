using System;
using Microsoft.PSharp.TestingServices.Scheduling.POR;

namespace ActorTestingFramework
{
    public static class SleepSets
    {
        public static void UpdateSleepSets(Stack stack)
        {
            if (stack.GetNumSteps() <= 1)
            {
                return;
            }

            TidEntryList prevTop = stack.GetSecondFromTop();
            TidEntry prevSelected = prevTop.List[prevTop.GetSelected()];

            TidEntryList currTop = stack.GetTop();

            // For each thread on the top of stack (except previously selected thread and new threads):
            //   if thread was slept previously 
            //   and thread's op was independent with selected op then:
            //     the thread is still slept.
            //   else: not slept.

            for (int i = 0; i < prevTop.List.Count; i++)
            {
                if (i == prevSelected.Id)
                {
                    continue;
                }
                if (prevTop.List[i].Sleep && !IsDependent(prevTop.List[i], prevSelected))
                {
                    currTop.List[i].Sleep = true;
                }
            }
            
        }

        public static bool IsDependent(TidEntry a, TidEntry b)
        {
            // This method will not detect the dependency between 
            // Create and Start (because Create's target id is always -1), 
            // but this is probably fine because we will never be checking that;
            // we only check enabled ops against other enabled ops.
            // Similarly, we assume that Send and Receive will always be independent
            // because the Send would need to enable the Receive to be dependent.
            // Receives are independent as they will always be from different threads,
            // but they should always have different target ids anyway.
            

            if (
                a.TargetId != b.TargetId || 
                a.TargetType != b.TargetType || 
                a.TargetId == -1 || 
                b.TargetId == -1)
            {
                return false;
            }

            // Same target:

            if (a.TargetType == TargetType.Queue)
            {
                return a.OpType == OpType.SEND && b.OpType == OpType.SEND;
            }
            

            return true;
        }
    }
}