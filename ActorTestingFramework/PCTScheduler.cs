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

        private Random rand;
        public int maxSteps;
        private int numSteps;
        public bool expectBadActor;


        public PCTScheduler(int seed, int numChangePoints)
        {
            rand = new Random(seed);
            this.numChangePoints = numChangePoints;
            changePoints = new List<int>();
            actorPriorityList = new List<ActorInfo>();
            badActors = new List<ActorInfo>();
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

        public void SetSeed(int seed)
        {
            rand = new Random(seed);
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

            // Increment num steps and reduce priority of currentActor if this is a change point.
            if (currentActor.currentOp == OpType.SEND)
            {
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
                        actorPriorityList.Remove(currentActor);
                        actorPriorityList.Add(currentActor);
                    }
                    changePoints.Remove(numSteps);
                }
            }

            var enabledWithoutYield =
                actorPriorityList.Where(
                    info =>
                        info.enabled &&
                        (currentActor != info || info.currentOp != OpType.Yield)).ToList();

            var enabledWithYield = actorPriorityList.Where(info => info.enabled).ToList();

            var enabled = enabledWithoutYield.Count == 0
                ? enabledWithYield
                : enabledWithoutYield;

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(info => info.currentOp != OpType.SEND).ToList();

            var choices = enabledNotSend.Count > 0 ? enabledNotSend : enabled;

            LOGGER.Trace("Actors: {0}", new ActorList(actorList, choices[0]));

            return choices[0];
        }

        public void NextSchedule()
        {
            actorPriorityList.Clear();
            maxSteps = Math.Max(maxSteps, numSteps);
            numSteps = 0;
            badActors.Clear();
            changePoints.Clear();
            for (int i = 0; i < numChangePoints; ++i)
            {
                changePoints.Add(rand.Next(maxSteps) + 1);
            }
        }

        #endregion
    }
}