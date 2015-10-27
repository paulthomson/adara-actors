using System;
using System.Collections.Generic;
using System.Linq;

namespace ActorTestingFramework
{
    public class RandomScheduler : IScheduler
    {

        private readonly Random rand = new Random();

        #region Implementation of IScheduler

        public ActorInfo GetNext(
            List<ActorInfo> actorList,
            ActorInfo currentActor)
        {
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