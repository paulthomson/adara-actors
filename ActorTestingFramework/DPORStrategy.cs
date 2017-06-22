using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.TestingServices.Scheduling.POR;

namespace ActorTestingFramework
{
    public class DPORStrategy : IScheduler
    {

        private readonly Stack Stack;
        private readonly DPORAlgorithm Dpor;
        private readonly bool UseSleepSets;
        private readonly int StepLimit;
        private Random Rand;

        public const int SLEEP_SET_BLOCKED = -2;

        public DPORStrategy(bool dpor, bool useSleepSets, Random rand, int stepLimit = -1)
        {
            Stack = new Stack(rand);
            Dpor = dpor ? new DPORAlgorithm() : null;
            UseSleepSets = useSleepSets;
            StepLimit = stepLimit;
            Rand = rand;
            Reset();
        }


        #region Implementation of IScheduler

        public NextActorResult GetNext(List<ActorInfo> actorList, ActorInfo currentActor, out ActorInfo nextActor)
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

            if (StepLimit >= 0 && Stack.GetNumSteps() >= StepLimit)
            {
                nextActor = null;
                return NextActorResult.HitStepLimit;
            }

            bool added = Stack.Push(actorList, currentActor.id.id);
            TidEntryList top = Stack.GetTop();

            if (added)
            {
                if (UseSleepSets && Rand == null)
                {
                    SleepSets.UpdateSleepSets(Stack);
                }

                if (Dpor == null)
                {
                    top.SetAllEnabledToBeBacktracked();
                }
                else if (Dpor.RaceReplaySuffix.Count > 0 && Dpor.replayRaceIndex < Dpor.RaceReplaySuffix.Count)
                {
                    // replaying a race
                    var tid = Dpor.RaceReplaySuffix[Dpor.replayRaceIndex];
                    top.List[tid].Backtrack = true;
                    Safety.Assert(top.List[tid].Enabled || top.List[tid].OpType == OpType.Yield);
                    ++Dpor.replayRaceIndex;
                }
                else
                {
                     top.AddFirstEnabledNotSleptToBacktrack(currentActor.id.id);
                }
            }

            int nextTidIndex = Stack.GetSelectedOrFirstBacktrackNotSlept(currentActor.id.id);

            if (nextTidIndex < 0)
            {
                nextActor = null;
                return nextTidIndex == SLEEP_SET_BLOCKED
                    ? NextActorResult.SleepsetBlocked
                    : NextActorResult.Deadlock;
            }

            TidEntry nextTidEntry = Stack.GetTop().List[nextTidIndex];

            if (!nextTidEntry.Selected)
            {
                nextTidEntry.Selected = true;
            }

            nextActor = actorList[nextTidEntry.Id];

            if (!nextActor.enabled &&
                nextActor.currentOp == OpType.Yield)
            {
                nextActor.enabled = true;
            }

            Safety.Assert(nextActor.enabled);
            return NextActorResult.Success;
        }

        public bool NextSchedule()
        {
            Dpor?.DoDPOR(Stack, Rand);

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