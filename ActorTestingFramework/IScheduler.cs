using System.Collections.Generic;

namespace ActorTestingFramework
{
    public interface IScheduler
    {
        ActorInfo GetNext(List<ActorInfo> actorList, ActorInfo currentActor);
        void NextSchedule();
        void SetSeed(int seed);
        int GetNumSteps();
        int GetStepLimit();
        int GetMaxSteps();
        int GetMaxActors();
        int GetMaxEnabledActors();
    }
}