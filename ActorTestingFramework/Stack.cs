using System.Collections.Generic;
using System.Linq;
using ActorTestingFramework;

namespace Microsoft.PSharp.TestingServices.Scheduling.POR
{
    /// <summary>
    /// 
    /// </summary>
    public class Stack
    {
        /// <summary>
        /// The actual stack.
        /// </summary>
        public readonly List<TidEntryList> StackInternal = new List<TidEntryList>();

        private int nextStackPos;

        /// <summary>
        /// Push a list of tid entries onto the stack.
        /// </summary>
        /// <param name="machines"></param>
        /// <param name="prevThreadIndex"></param>
        /// <returns></returns>
        public bool Push(List<ActorInfo> machines, int prevThreadIndex)
        {
            List<TidEntry> list = new List<TidEntry>();

            foreach (var machineInfo in machines)
            {
                list.Add(
                    new TidEntry(
                        machineInfo.id.id,
                        machineInfo.enabled,
                        machineInfo.currentOp,
                        machineInfo.currentOpTargetType,
                        machineInfo.currentOpTarget));
            }
            
            if (nextStackPos > StackInternal.Count)
            {
                throw new SchedulingStrategyException("DFS strategy unexpected stack state.");
            }

            bool added = nextStackPos == StackInternal.Count;

            if (added)
            {
                StackInternal.Add(new TidEntryList(list));
            }
            else
            {
                CheckMatches(list);
            }
            ++nextStackPos;

            return added;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetNumSteps()
        {
            return nextStackPos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetInternalSize()
        {
            return StackInternal.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TidEntryList GetTop()
        {
            return StackInternal[nextStackPos - 1];
        }

        public TidEntryList GetSecondFromTop()
        {
            return StackInternal[nextStackPos - 2];
        }

        /// <summary>
        /// Gets the top of stack and also ensures that this is the real top of stack.
        /// </summary>
        /// <returns></returns>
        public TidEntryList GetTopAsRealTop()
        {
            if (nextStackPos != StackInternal.Count)
            {
                throw new SchedulingStrategyException("DFS Strategy: top of stack is not aligned.");
            }
            return GetTop();
        }

        /// <summary>
        /// Get the next thread to schedule: either the preselected thread entry
        /// from the current schedule prefix that we are replaying or the first
        /// suitable thread entry from the real top of the stack.
        /// </summary>
        /// <returns></returns>
        public int GetSelectedOrFirstBacktrackNotSlept(int startingFrom)
        {
            var top = GetTop();

            return StackInternal.Count == nextStackPos
                ? top.GetFirstBacktrackNotSlept(startingFrom)
                : top.GetSelected();
        }

        /// <summary>
        /// 
        /// </summary>
        public void PrepareForNextSchedule()
        {
            // Deadlock / sleep set blocked; no selected tid entry.
            {
                TidEntryList top = GetTopAsRealTop();
                
                if (top.IsNoneSelected())
                {
                    Pop();
                }
            }
            

            // Pop until there are some tid entries that are not done/slept OR stack is empty.
            while (StackInternal.Count > 0)
            {
                TidEntryList top = GetTopAsRealTop();
                top.SetSelectedToSleep();
                top.ClearSelected();

                if (!top.AllDoneOrSlept())
                {
                    break;
                }

                Pop();
            }

            nextStackPos = 0;
        }

        private void Pop()
        {
            if (nextStackPos != StackInternal.Count)
            {
                throw new SchedulingStrategyException("DFS Strategy: top of stack is not aligned.");
            }
            StackInternal.RemoveAt(StackInternal.Count - 1);
            --nextStackPos;
        }

        private void CheckMatches(List<TidEntry> list)
        {
            if (!StackInternal[nextStackPos].List.SequenceEqual(list, TidEntry.ComparerSingleton))
            {
                throw new SchedulingStrategyException("DFS strategy detected nondeterminism.");
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            StackInternal.Clear();
        }
    }
}