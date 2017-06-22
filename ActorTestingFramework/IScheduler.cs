using System.Collections.Generic;

namespace ActorTestingFramework
{
    public interface IScheduler
    {
        NextActorResult GetNext(List<ActorInfo> actorList, ActorInfo currentActor, out ActorInfo nextActor);
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