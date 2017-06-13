using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace ActorTestingFramework
{
    public class RandomScheduler : IScheduler
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private readonly int stepLimit;

        private Random rand;
        private readonly int seed;
        private int numSteps;

        private int maxSteps;
        private int maxActors;
        private int maxEnabledActors;


        public RandomScheduler(int seed, int stepLimit)
        {
            this.stepLimit = stepLimit;
            this.seed = seed;
            Reset();
        }

        #region Implementation of IScheduler

        public ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor)
        {
            var enabled = actorList.Where(info => info.enabled).ToList();

            maxActors = Math.Max(maxActors, actorList.Count);
            maxEnabledActors = Math.Max(maxEnabledActors, enabled.Count);

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(
                    info =>
                        info.currentOp != OpType.SEND &&
                        info.currentOp != OpType.Yield).ToList();

            var choices = enabledNotSend.Count > 0 ? new List<ActorInfo> { enabledNotSend[0] }  : enabled;
            
            int nextIndex = choices.Count == 1 ? 0 : rand.Next(choices.Count);

            LOGGER.Trace("Actors: {0}", new ActorList(actorList, choices[nextIndex]));

            if (numSteps >= stepLimit)
            {
                return null;
            }

            if (choices[nextIndex].currentOp == OpType.SEND ||
                choices[nextIndex].currentOp == OpType.Yield)
            {
                ++numSteps;
            }

            return choices[nextIndex];
        }

        public bool NextSchedule()
        {
            if (numSteps != stepLimit)
            {
                maxSteps = Math.Max(maxSteps, numSteps);
            }

            numSteps = 0;
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
            rand = new Random(seed);
        }

        #endregion
    }
}