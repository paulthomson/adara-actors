using System;
using System.Collections.Generic;
using System.Linq;

namespace ActorTestingFramework
{
    public class RandomScheduler : IScheduler
    {
        private readonly Random rand = new Random();

        private static bool IsProgressOp(OpType op)
        {
            switch (op)
            {
                case OpType.INVALID:
                case OpType.START:
                case OpType.END:
                case OpType.CREATE:
                case OpType.JOIN:
                case OpType.WaitForDeadlock:
                case OpType.Yield:
                    return false;
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

            if (IsProgressOp(currentActor.currentOp))
            {
                foreach (var actorInfo in
                    actorList.Where(info => info.currentOp == OpType.Yield && !info.enabled))
                {
                    actorInfo.enabled = true;
                }
            }

            var enabled = actorList.Where(info => info.enabled).ToList();

            if (enabled.Count == 0)
            {
                return null;
            }

            int nextIndex = rand.Next(enabled.Count - 1);

            return enabled[nextIndex];
        }

        public void NextSchedule()
        {
        }

        #endregion
    }
}