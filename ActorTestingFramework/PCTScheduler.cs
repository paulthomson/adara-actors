using System;
using System.Collections.Generic;
using System.Linq;

namespace ActorTestingFramework
{
    public class PCTScheduler : IScheduler
    {
        private readonly List<ActorInfo> actorPriorityList;
        private readonly int numChangePoints;
        private readonly List<int> changePoints; 

        private Random rand;
        private int maxSteps;
        private int numSteps;

        public PCTScheduler(int seed, int numChangePoints)
        {
            rand = new Random(seed);
            this.numChangePoints = numChangePoints;
            changePoints = new List<int>();
            actorPriorityList = new List<ActorInfo>();
        }

        private void InsertActorRandomly(ActorInfo actorInfo)
        {
            int pos = rand.Next(actorPriorityList.Count + 1);
            actorPriorityList.Insert(pos, actorInfo);
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

            // Increment num steps and reduce priority of currentActor if this a change point.
            if (currentActor.currentOp == OpType.SEND)
            {
                ++numSteps;
                if (changePoints.Contains(numSteps))
                {
                    actorPriorityList.Remove(currentActor);
                    actorPriorityList.Add(currentActor);
                    changePoints.Remove(numSteps);
                }
            }

            if (currentActor.currentOp == OpType.Yield)
            {
                currentActor.enabled = false;
            }

            var enabled = actorPriorityList.Where(info => info.enabled).ToList();

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(info => info.currentOp != OpType.SEND).ToList();

            var choices = enabledNotSend.Count > 0 ? enabledNotSend : enabled;

            return choices[0];
        }

        public void NextSchedule()
        {
            actorPriorityList.Clear();
            maxSteps = Math.Max(maxSteps, numSteps);
            numSteps = 0;
            changePoints.Clear();
            for (int i = 0; i < numChangePoints; ++i)
            {
                changePoints.Add(rand.Next(maxSteps) + 1);
            }
        }

        #endregion
    }
}