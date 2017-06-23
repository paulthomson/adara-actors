using System;
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

        private Random random;

//        private Random randHack = new Random(0);

        public Stack(Random random)
        {
            this.random = random;
        }

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
                        machineInfo.currentOpTarget,
                        machineInfo.currentOpSendStepIndex));
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

            if (StackInternal.Count > nextStackPos)
            {
                return top.GetSelected();
            }

            int res = top.TryGetSelected();
            return res >= 0 ? res : top.GetFirstBacktrackNotSlept(startingFrom);

        }

        /// <summary>
        /// 
        /// </summary>
        public void PrepareForNextSchedule()
        {
            if (random != null)
            {
                nextStackPos = 0;
                return;
                List<int> backtrackSteps = new List<int>(StackInternal.Count);
                for (int i = 0; i < StackInternal.Count; ++i)
                {
                    var entries = StackInternal[i];
                    if (entries.HasBacktrackNotSleptNotSelected())
                    {
                        backtrackSteps.Add(i);
                    }
                }

                {
                    if (backtrackSteps.Count == 0)
                    {
                        StackInternal.RemoveRange(1, StackInternal.Count - 1);
                        nextStackPos = 0;
                        return;
                    }

                    int stepChoice = random.Next(backtrackSteps.Count);
                    int stepIndex = backtrackSteps[stepChoice];

                    var allEntries = StackInternal[stepIndex];
                    var backtrackEntries =
                        allEntries.GetAllBacktrackNotSleptNotSelected();

                    var backtrackEntryChoice = random.Next(backtrackEntries.Count);
                    var backtrackEntryIndex = backtrackEntries[backtrackEntryChoice];
//                    Console.Write($"Backtracking to step {stepIndex}.\n");
                    var backtrackEntry = allEntries.List[backtrackEntryIndex];
                    allEntries.SetSelectedToSleep();
                    allEntries.ClearSelected();
                    backtrackEntry.Selected = true;
                    Safety.Assert(backtrackEntry.Enabled);
                    StackInternal.RemoveRange(stepIndex + 1, StackInternal.Count - stepIndex - 1);
                    nextStackPos = 0;
                }

               return;

            }
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

//            if (randHack.Next(1000) == 0)
//            {
//                List<int> backtrackSteps = new List<int>(StackInternal.Count);
//                for (int i = 0; i < StackInternal.Count; ++i)
//                {
//                    var entries = StackInternal[i];
//                    if (entries.HasBacktrackNotSleptNotSelected())
//                    {
//                        backtrackSteps.Add(i);
//                    }
//                }
//
//                if (backtrackSteps.Count > 1)
//                {
//                    int choice = randHack.Next(backtrackSteps.Count - 1);
//                    int backtrackTo = backtrackSteps[choice];
//                    while (StackInternal.Count > backtrackTo + 1)
//                    {
//                        Pop();
//                    }
//                    TidEntryList top = GetTopAsRealTop();
//                    Safety.Assert(top.TryGetSelected() >= 0);
//                    top.SetSelectedToSleep();
//                    top.ClearSelected();
//                }
//            }

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