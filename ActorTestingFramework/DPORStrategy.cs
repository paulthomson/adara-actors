using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Scheduling.POR;

namespace ActorTestingFramework
{
    public class DPORStrategy : IScheduler
    {

        private readonly Stack Stack;
        private readonly DPORAlgorithm Dpor;
        private readonly bool UseSleepSets;
        private readonly int StepLimit;

        public DPORStrategy(bool dpor, bool useSleepSets, int stepLimit = -1)
        {
            Stack = new Stack();
            Dpor = dpor ? new DPORAlgorithm() : null;
            UseSleepSets = useSleepSets;
            StepLimit = stepLimit;
            Reset();
        }


        #region Implementation of IScheduler

        public ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor)
        {
            // "Yield" and "Waiting for deadlock" hack.
            if (actorList.TrueForAll(info => !info.enabled))
            {
                if (actorList.Exists(info => info.currentOp == OpType.Yield))
                {
                    foreach (var actorInfo in actorList)
                    {
                        if (actorInfo.currentOp == OpType.Yield)
                        {
                            actorInfo.enabled = true;
                        }
                    }
                }
                else if (actorList.Exists(
                    info => info.currentOp == OpType.WaitForDeadlock))
                {
                    foreach (var actorInfo in actorList)
                    {
                        if (actorInfo.currentOp == OpType.WaitForDeadlock)
                        {
                            actorInfo.enabled = true;
                        }
                    }
                }
            }

            if (Stack.GetNumSteps() >= StepLimit)
            {
                return null;
            }

            bool added = Stack.Push(actorList, currentActor.id.id);

            if (added)
            {
                TidEntryList top = Stack.GetTop();

                if (UseSleepSets)
                {
                    SleepSets.UpdateSleepSets(Stack);
                }

                if (Dpor == null)
                {
                    top.SetAllEnabledToBeBacktracked();
                }
                else
                {
                    top.AddFirstEnabledNotSleptToBacktrack(currentActor.id.id);
                }
            }

            int nextTidIndex = Stack.GetSelectedOrFirstBacktrackNotSlept(currentActor.id.id);

            if (nextTidIndex < 0)
            {
                return null;
            }

            TidEntry nextTidEntry = Stack.GetTop().List[nextTidIndex];

            if (!nextTidEntry.Selected)
            {
                nextTidEntry.Selected = true;
            }

            return actorList[nextTidEntry.Id];
        }

        public bool NextSchedule()
        {
            Dpor?.DoDPOR(Stack);

            Stack.PrepareForNextSchedule();
            return Stack.GetInternalSize() != 0;
        }

        public void SetSeed(int seed)
        {
        }

        public int GetNumSteps()
        {
            return Stack.GetNumSteps();
        }

        public int GetStepLimit()
        {
            return StepLimit;
        }

        public int GetMaxSteps()
        {
            return 0;
        }

        public int GetMaxActors()
        {
            return 0;
        }

        public int GetMaxEnabledActors()
        {
            return 0;
        }

        public void Reset()
        {
            Stack.Clear();
        }

        #endregion
    }
}