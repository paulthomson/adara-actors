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

        public DPORStrategy(bool dpor, bool useSleepSets)
        {
            Stack = new Stack();
            Dpor = dpor ? new DPORAlgorithm() : null;
            UseSleepSets = useSleepSets;
            Reset();
        }


        #region Implementation of IScheduler

        public ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor)
        {
            // "Waiting for deadlock" hack.
            if (actorList.TrueForAll(info => !info.enabled))
            {
                foreach (var actorInfo in actorList)
                {
                    if (actorInfo.waitingForDeadlock)
                    {
                        actorInfo.enabled = true;
                        actorInfo.waitingForDeadlock = false;
                    }
                }
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
            throw new System.NotImplementedException();
        }

        public int GetMaxSteps()
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxActors()
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxEnabledActors()
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            Stack.Clear();
        }

        #endregion
    }
}