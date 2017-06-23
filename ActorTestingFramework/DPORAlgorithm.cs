using System;
using System.Collections.Generic;
using System.Linq;
using ActorTestingFramework;

namespace Microsoft.PSharp.TestingServices.Scheduling.POR
{
    /// <summary>
    /// 
    /// </summary>
    public class DPORAlgorithm
    {
        private uint numThreads;
        private uint numSteps;
        
        private uint[] threadIdToLastOpIndex; 
        private uint[] targetIdToLastCreateStartEnd;
        private uint[] targetIdToLastSend;
        private uint[] vcs;

        private readonly List<Race> races;

        // Race replay suffix.
        public readonly List<int> RaceReplaySuffix;

        public int replayRaceIndex;

        private readonly List<int> missingThreadIds;

        /// <summary>
        /// 
        /// </summary>
        public DPORAlgorithm()
        {
            // initial estimates

            numThreads = 4;
            numSteps = 1 << 8;

            threadIdToLastOpIndex = new uint[numThreads];
            targetIdToLastCreateStartEnd = new uint[numThreads];
            targetIdToLastSend = new uint[numThreads];
            vcs = new uint[numSteps * numThreads];
            races = new List<Race>((int) numSteps);
            RaceReplaySuffix = new List<int>();
            missingThreadIds = new List<int>();
            replayRaceIndex = 0;
        }


        private void FromVCSetVC(uint from, uint to)
        {
            uint fromI = (from-1) * numThreads;
            uint toI = (to-1) * numThreads;
            for (uint i = 0; i < numThreads; ++i)
            {
                vcs[toI] = vcs[fromI];
                ++fromI;
                ++toI;
            }
        }

        private void ForVCSetClockToValue(uint vc, uint clock, uint value)
        {
            vcs[(vc - 1) * numThreads + clock] = value;
        }

        private uint ForVCGetClock(uint vc, int clock)
        {
            return vcs[(vc - 1) * numThreads + clock];
        }

        private void ForVCJoinFromVC(uint to, uint from)
        {
            uint fromI = (from - 1) * numThreads;
            uint toI = (to - 1) * numThreads;
            for (uint i = 0; i < numThreads; ++i)
            {
                if (vcs[fromI] > vcs[toI])
                {
                    vcs[toI] = vcs[fromI];
                }
                ++fromI;
                ++toI;
            }
        }

        private void ClearVC(uint vc)
        {
            uint vcI = (vc - 1) * numThreads;
            for (uint i = 0; i < numThreads; ++i)
            {
                vcs[vcI] = 0;
                ++vcI;
            }
        }

        private uint[] GetVC(uint vc)
        {
            uint[] res = new uint[numThreads];
            uint fromI = (vc - 1) * numThreads;
            for (uint i = 0; i < numThreads; ++i)
            {
                res[i] = vcs[fromI];
                ++fromI;
            }
            return res;
        }

        private bool HB(Stack stack, uint vc1, uint vc2)
        {
            // A hb B
            // iff:
            // A's index <= B.VC[A's tid]

            TidEntry aStep = GetSelectedTidEntry(stack, vc1);

            return vc1 <= ForVCGetClock(vc2, aStep.Id);
        }


        private TidEntry GetSelectedTidEntry(Stack stack, uint index)
        {
            var list = GetThreadsAt(stack, index);
            return list.List[list.GetSelected()];
        }

        /// <summary>
        /// Assumes both operations passed in are dependent.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        private bool Reversible(Stack stack, uint index1, uint index2)
        {
            var step1 = GetSelectedTidEntry(stack, index1);
            var step2 = GetSelectedTidEntry(stack, index2);
            return step1.OpType == OpType.SEND &&
                   step2.OpType == OpType.SEND;
        }

        private static TidEntryList GetThreadsAt(Stack stack, uint index)
        {
            return stack.StackInternal[(int)index - 1];
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="rand"></param>
        public void DoDPOR(Stack stack, Random rand)
        {
            UpdateFieldsAndRealocateDatastructuresIfNeeded(stack);
            
            // Indexes start at 1.

            for (uint i = 1; i < numSteps; ++i)
            {
                TidEntry step = GetSelectedTidEntry(stack, i);
                if (threadIdToLastOpIndex[step.Id] > 0)
                {
                    FromVCSetVC(threadIdToLastOpIndex[step.Id], i);
                }
                else
                {
                    ClearVC(i);
                }
                ForVCSetClockToValue(i, (uint) step.Id, i);
                threadIdToLastOpIndex[step.Id] = i;

                int targetId = step.TargetId;
                if (step.OpType == OpType.CREATE && i + 1 < numSteps)
                {
                    targetId = GetThreadsAt(stack, i).List.Count;
                }

                if (targetId < 0)
                {
                    continue;
                }

                uint lastAccessIndex = 0;

                switch (step.OpType)
                {
                    case OpType.START:
                    case OpType.END:
                    case OpType.CREATE:
                    case OpType.JOIN:
                    {
                        lastAccessIndex =
                            targetIdToLastCreateStartEnd[targetId];
                        targetIdToLastCreateStartEnd[targetId] = i;
                        break;
                    }
                    case OpType.SEND:
                    {
                        lastAccessIndex = targetIdToLastSend[targetId];
                        targetIdToLastSend[targetId] = i;
                        break;
                    }
                    case OpType.RECEIVE:
                    {
                        lastAccessIndex = (uint) step.SendStepIndex;
                        break;
                    }
                    case OpType.WaitForDeadlock:
                        for (int j = 0; j < threadIdToLastOpIndex.Length; j++)
                        {
                            if (j == step.Id || threadIdToLastOpIndex[j] == 0)
                            {
                                continue;
                            }
                            ForVCJoinFromVC(i, threadIdToLastOpIndex[j]);
                        }
                        // Continue. No backtrack.
                        continue;

                    case OpType.Yield:
                        // TODO: 
                        break;
                    case OpType.INVALID:
                        throw new ArgumentOutOfRangeException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                    
                if (lastAccessIndex > 0)
                {
                    AddBacktrack(stack, lastAccessIndex, i, step);

                    ForVCJoinFromVC(i, lastAccessIndex);
                }
            }

            if (rand != null)
            {
                DoRandomRaceReverse(stack, rand);
            }

        }

        private void DoRandomRaceReverse(Stack stack, Random rand)
        {
            int raceIndex = rand.Next(races.Count);
            Race race = races[raceIndex];

            Safety.Assert(RaceReplaySuffix.Count == 0);

            // Add to RaceReplaySuffix: all steps between a and b that do not h.a. a.
            for (int i = race.a; i < race.b; ++i)
            {
                if (HB(stack, (uint) race.a, (uint) i))
                {
                    // Skip it.
                    // But track the missing thread id if this is a create operation.
                    if (GetSelectedTidEntry(stack, (uint) i).OpType == OpType.CREATE)
                    {
                        var missingThreadId = GetThreadsAt(stack, (uint) i).List.Count;
                        var index = missingThreadIds.BinarySearch(missingThreadId);
                        // We should not find it.
                        Safety.Assert(index < 0);
                        // Get the next largest index (see BinarySearch).
                        index = ~index;
                        // Insert before the next largest item.
                        missingThreadIds.Insert(index, missingThreadId);
                    }
                }
                else
                {
                    // Add thread id to the RaceReplaySuffix, but adjust
                    // it for missing thread ids.
                    AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (uint) i).Id);
                }
            }

            AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (uint) race.b).Id);
            AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (uint) race.a).Id);

            // Remove steps from a onwards. Indexes start at one so we must subtract 1.
            stack.StackInternal.RemoveRange(race.a - 1, stack.StackInternal.Count - (race.a - 1));

        }

        private void AddThreadIdToRaceReplaySuffix(int threadId)
        {
            // Add thread id to the RaceReplaySuffix, but adjust
            // it for missing thread ids.

            var index = missingThreadIds.BinarySearch(threadId);
            // Make it so index is the number of missing thread ids before and including threadId.
            // e.g. if missingThreadIds = [3,6,9]
            // 3 => index + 1  = 1
            // 4 => ~index     = 1
            if (index >= 0)
            {
                index += 1;
            }
            else
            {
                index = ~index;
            }

            RaceReplaySuffix.Add(threadId - index);
        }

        private void AddBacktrack(Stack stack,
            uint lastAccessIndex,
            uint i,
            TidEntry step)
        {
            var aTidEntries = GetThreadsAt(stack, lastAccessIndex);
            var a = GetSelectedTidEntry(stack, lastAccessIndex);
            if (HB(stack, lastAccessIndex, i) ||
                !Reversible(stack, lastAccessIndex, i)) return;

            races.Add(new Race((int)lastAccessIndex, (int)i));

            // Find candidates:
            // Race between `a` and `b`.
            // Must find first steps after `a` that do not HA `a`
            // (except maybe b.tid) and do not HA each other.
            // candidates = {}
            // if b.tid is enabled before a:
            //   add b.tid to candidates
            // lookingFor = set of enabled threads before a - a.tid - b.tid.
            // let vc = [0,0,...]
            // vc[a.tid] = a;
            // for k = aIndex+1 to bIndex:
            //   if lookingFor does not contain k.tid:
            //     continue
            //   remove k.tid from lookingFor
            //   doesHaAnother = false
            //   foreach t in tids:
            //     if vc[t] hb k:
            //       doesHaAnother = true
            //       break
            //   vc[k.tid] = k
            //   if !doesHaAnother:
            //     add k.tid to candidates
            //   if lookingFor is empty:
            //     break


            var candidateThreadIds = new HashSet<uint>();
            
            if (aTidEntries.List.Count > step.Id && 
                (aTidEntries.List[step.Id].Enabled || aTidEntries.List[step.Id].OpType == OpType.Yield))
            {
                candidateThreadIds.Add((uint) step.Id);
            }
            var lookingFor = new HashSet<uint>();
            for (uint j = 0; j < aTidEntries.List.Count; ++j)
            {
                if (j != a.Id &&
                    j != step.Id &&
                    (aTidEntries.List[(int) j].Enabled || aTidEntries.List[(int)j].OpType == OpType.Yield))
                {
                    lookingFor.Add(j);
                }
            }

            uint[] vc = new uint[numThreads];
            vc[a.Id] = lastAccessIndex;
            if (lookingFor.Count > 0)
            {
                for (uint k = lastAccessIndex + 1; k < i; ++k)
                {
                    var kEntry = GetSelectedTidEntry(stack, k);
                    if (!lookingFor.Contains((uint) kEntry.Id)) continue;

                    lookingFor.Remove((uint) kEntry.Id);
                    bool doesHaAnother = false;
                    for (int t = 0; t < numThreads; ++t)
                    {
                        if (vc[t] > 0 &&
                            vc[t] <= ForVCGetClock(k, t))
                        {
                            doesHaAnother = true;
                            break;
                        }
                    }
                    if (!doesHaAnother)
                    {
                        candidateThreadIds.Add((uint) kEntry.Id);
                    }
                    if (lookingFor.Count == 0)
                    {
                        break;
                    }
                }
            }

            // Make sure at least one candidate is found

            if (candidateThreadIds.Count == 0)
            {
                Safety.Assert(false);
                throw new SchedulingStrategyException("DPOR: There were no candidate backtrack points.");
            }

            // Is one already backtracked?
            foreach (var tid in candidateThreadIds)
            {
                if (aTidEntries.List[(int) tid].Backtrack)
                {
                    return;
                }
            }

            // None are backtracked, so we have to pick one.
            // Try to pick one that is slept first.
            // Start from thread b.tid:
            {
                uint sleptThread = (uint) step.Id;
                for (uint k = 0; k < numThreads; ++k)
                {
                    if (candidateThreadIds.Contains(sleptThread) &&
                        aTidEntries.List[(int)sleptThread].Sleep)
                    {
                        aTidEntries.List[(int)sleptThread].Backtrack = true;
                        return;
                    }
                    ++sleptThread;
                    if (sleptThread >= numThreads)
                    {
                        sleptThread = 0;
                    }
                }
            }

            // None are slept.
            // Avoid picking threads that are disabled (due to yield hack)
            // Start from thread b.tid:
            {
                uint backtrackThread = (uint)step.Id;
                for (uint k = 0; k < numThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread) &&
                        aTidEntries.List[(int) backtrackThread].Enabled)
                    {
                        aTidEntries.List[(int) backtrackThread].Backtrack = true;
                        return;
                    }
                    ++backtrackThread;
                    if (backtrackThread >= numThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            // None are slept and enabled.
            // Start from thread b.tid:
            {
                uint backtrackThread = (uint)step.Id;
                for (uint k = 0; k < numThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread))
                    {
                        aTidEntries.List[(int)backtrackThread].Backtrack = true;
                        return;
                    }
                    ++backtrackThread;
                    if (backtrackThread >= numThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            throw new SchedulingStrategyException("DPOR: Did not manage to add backtrack point.");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stack"></param>
        private void UpdateFieldsAndRealocateDatastructuresIfNeeded(Stack stack)
        {
            numThreads = (uint) stack.GetTopAsRealTop().List.Count;
            numSteps = (uint) stack.StackInternal.Count;

            int temp = threadIdToLastOpIndex.Length;
            while (temp < numThreads)
            {
                temp <<= 1;
            }

            if (threadIdToLastOpIndex.Length < temp)
            {
                threadIdToLastOpIndex = new uint[temp];
                targetIdToLastCreateStartEnd = new uint[temp];
                targetIdToLastSend = new uint[temp];
            }
            else
            {
                Array.Clear(threadIdToLastOpIndex, 0, threadIdToLastOpIndex.Length);
                Array.Clear(targetIdToLastCreateStartEnd, 0, targetIdToLastCreateStartEnd.Length);
                Array.Clear(targetIdToLastSend, 0, targetIdToLastSend.Length);
            }

            uint numClocks = numThreads * numSteps;

            temp = vcs.Length;

            while (temp < numClocks)
            {
                temp <<= 1;
            }

            if (vcs.Length < temp)
            {
                vcs = new uint[temp];
            }

            races.Clear();
            RaceReplaySuffix.Clear();
            missingThreadIds.Clear();
            replayRaceIndex = 0;

        }

    }
}