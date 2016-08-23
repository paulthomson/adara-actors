﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace ActorTestingFramework
{
    public class RandomScheduler : IScheduler
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private Random rand;

        public RandomScheduler(int seed)
        {
            rand = new Random(seed);
        }

        public void SetSeed(int seed)
        {
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
            if (currentActor.currentOp == OpType.Yield)
            {
                currentActor.enabled = false;
            }

            var enabled = actorList.Where(info => info.enabled).ToList();

            if (enabled.Count == 0)
            {
                return null;
            }

            var enabledNotSend =
                enabled.Where(info => info.currentOp != OpType.SEND).ToList();

            var choices = enabledNotSend.Count > 0 ? enabledNotSend : enabled;

            int nextIndex = rand.Next(choices.Count);

            if (IsProgressOp(choices[nextIndex].currentOp))
            {
                foreach (var actorInfo in
                    actorList.Where(info => info.currentOp == OpType.Yield && !info.enabled))
                {
                    actorInfo.enabled = true;
                }
            }

            LOGGER.Trace("Actors: {0}", new ActorList(actorList, choices[nextIndex]));

            return choices[nextIndex];
        }

        public void NextSchedule()
        {
        }

        #endregion
    }
}