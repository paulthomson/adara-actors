using System.Collections.Generic;

namespace ActorTestingFramework
{
    public interface IScheduler
    {
        ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor);
        bool NextSchedule();
        void SetSeed(int seed);
        int GetNumSteps();
        int GetStepLimit();
        int GetMaxSteps();
        int GetMaxActors();
        int GetMaxEnabledActors();
        void Reset();
    }
}