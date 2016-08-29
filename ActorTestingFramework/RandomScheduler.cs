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
        private int numSteps;

        private int maxSteps;
        private int maxActors;
        private int maxEnabledActors;


        public RandomScheduler(int seed, int stepLimit)
        {
            this.stepLimit = stepLimit;
            rand = new Random(seed);
        }

        public static bool IsProgressOp(OpType op)
        {
            switch (op)
            {
                case OpType.INVALID:
                case OpType.Yield:
                case OpType.WaitForDeadlock:
                case OpType.START:
                case OpType.END:
                    return false;
                case OpType.CREATE:
                case OpType.JOIN:
                case OpType.SEND:
                case OpType.RECEIVE:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        #region Implementation of IScheduler

        public ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor)
        {
            var enabledWithoutYield =
                actorList.Where(
                    info =>
                        info.enabled &&
                        (currentActor != info || info.currentOp != OpType.Yield)).ToList();

            var enabledWithYield = actorList.Where(info => info.enabled).ToList();

            maxActors = Math.Max(maxActors, actorList.Count);
            maxEnabledActors = Math.Max(maxEnabledActors, enabledWithYield.Count);

            var enabled = enabledWithoutYield.Count == 0
                ? enabledWithYield
                : enabledWithoutYield;

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(info => info.currentOp != OpType.SEND).ToList();

            var choices = enabledNotSend.Count > 0 ? new List<ActorInfo> { enabledNotSend[0]}  : enabled;
            
            int nextIndex = choices.Count == 1 ? 0 : rand.Next(choices.Count);

            LOGGER.Trace("Actors: {0}", new ActorList(actorList, choices[nextIndex]));

            if (numSteps >= stepLimit)
            {
                return null;
            }

            ++numSteps;

            return choices[nextIndex];
        }

        public void NextSchedule()
        {
            maxSteps = Math.Max(maxSteps, numSteps);

            numSteps = 0;
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

        #endregion
    }
}