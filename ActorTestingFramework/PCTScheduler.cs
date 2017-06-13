using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace ActorTestingFramework
{
    public class PCTScheduler : IScheduler
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private readonly List<ActorInfo> actorPriorityList;
        private readonly int numChangePoints;
        private readonly List<int> changePoints; 
        private readonly List<ActorInfo> badActors;
        private readonly int stepLimit;

        private Random rand;
        public int maxSteps;
        public int numSteps;
        public bool expectBadActor;
        public int maxActors;
        public int maxEnabledActors;

        public PCTScheduler(int seed, int numChangePoints, int stepLimit)
        {
            rand = new Random(seed);
            this.numChangePoints = numChangePoints;
            changePoints = new List<int>();
            actorPriorityList = new List<ActorInfo>();
            badActors = new List<ActorInfo>();
            this.stepLimit = stepLimit;
        }

        private void InsertActorRandomly(ActorInfo actorInfo)
        {
            int pos = rand.Next(actorPriorityList.Count + 1);
            actorPriorityList.Insert(pos, actorInfo);

            if (expectBadActor)
            {
                if (actorInfo.name != null && actorInfo.name.StartsWith("bad"))
                {
                    badActors.Add(actorInfo);
                }
            }
        }

        #region Implementation of IScheduler

        public ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor)
        {
            // Add new actors to the priority list.
            for (int i = actorPriorityList.Count;
                i < actorList.Count;
                ++i)
            {
                InsertActorRandomly(actorList[i]);
            }

            var enabled = actorPriorityList.Where(info => info.enabled).ToList();

            maxActors = Math.Max(maxActors, actorList.Count);
            maxEnabledActors = Math.Max(maxEnabledActors, enabled.Count);

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(info => info.currentOp != OpType.SEND && info.currentOp != OpType.Yield).ToList();

            var choices = enabledNotSend.Count > 0 ? enabledNotSend : enabled;

            LOGGER.Trace("Actors: {0}", new ActorList(actorList, choices[0]));

            // Increment num steps and reduce priority of next actor if this is a change point.
            if (choices[0].currentOp == OpType.SEND || choices[0].currentOp == OpType.Yield)
            {
                if (numSteps >= stepLimit)
                {
                    return null;
                }
                ++numSteps;

                if (changePoints.Contains(numSteps))
                {
                    if (badActors.Count > 0)
                    {
                        LOGGER.Info("ChangePoint {0}: boosting bad actor", numSteps);
                        // move bad actor to the highest priority.
                        actorPriorityList.Remove(badActors[0]);
                        actorPriorityList.Insert(0, badActors[0]);
                        badActors.RemoveAt(0);
                    }
                    else
                    {
                        LOGGER.Info("ChangePoint {0}: lowering current actor", numSteps);
                        // move current actor to the lowest priority.
                        actorPriorityList.Remove(choices[0]);
                        actorPriorityList.Add(choices[0]);
                    }
                    changePoints.Remove(numSteps);
                }
            }

            if (choices[0].currentOp == OpType.Yield)
            {
                LOGGER.Info("Step {0}: lowering current actor because of yield.", numSteps);
                actorPriorityList.Remove(choices[0]);
                actorPriorityList.Add(choices[0]);
            }

            return choices[0];
        }

        public bool NextSchedule()
        {
            actorPriorityList.Clear();
            if (numSteps != stepLimit)
            {
                maxSteps = Math.Max(maxSteps, numSteps);
            }
            numSteps = 0;
            badActors.Clear();
            changePoints.Clear();
            for (int i = 0; i < numChangePoints; ++i)
            {
                changePoints.Add(rand.Next(maxSteps) + 1);
            }
            return true;
        }

        public void SetSeed(int seed)
        {
            rand = new Random(seed);
        }

        public int GetNumSteps()
        {
            return numSteps;
        }

        public int GetStepLimit()
        {
            return stepLimit;
        }

        public int GetMaxSteps()
        {
            return maxSteps;
        }

        public int GetMaxActors()
        {
            return maxActors;
        }

        public int GetMaxEnabledActors()
        {
            return maxEnabledActors;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}