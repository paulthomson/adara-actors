using System.Collections.Generic;

namespace ActorTestingFramework
{
    public interface IScheduler
    {
        ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor);
        void NextSchedule();
    }
}