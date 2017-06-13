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
        // List of prior sends that have not yet been received.
        private List<uint>[] targetIdToListOfSends;
        private uint[] vcs;

        /// <summary>
        /// 
        /// </summary>
        public DPORAlgorithm()
        {
            // inital estimates

            numThreads = 4;
            numSteps = 1 << 8;

            threadIdToLastOpIndex = new uint[numThreads];
            targetIdToLastCreateStartEnd = new uint[numThreads];
            targetIdToLastSend = new uint[numThreads];
            targetIdToListOfSends = new List<uint>[numThreads];
            for (int i = 0; i < numThreads; ++i)
            {
                targetIdToListOfSends[i] = new List<uint>();
            }
            vcs = new uint[numSteps * numThreads];
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
        public void DoDPOR(Stack stack)
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
                        targetIdToListOfSends[targetId].Add(i);
                        targetIdToLastSend[targetId] = i;
                        break;
                    }
                    case OpType.RECEIVE:
                    {
                        var listOfSends = targetIdToListOfSends[targetId];
                        lastAccessIndex = listOfSends[0];
                        listOfSends.RemoveAt(0);
                        break;
                    }
                    case OpType.WaitForDeadlock:
                        // TODO: 
                        break;
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

        }

        private void AddBacktrack(Stack stack,
            uint lastAccessIndex,
            uint i,
            TidEntry step)
        {
            var a = GetSelectedTidEntry(stack, lastAccessIndex);
            if (HB(stack, lastAccessIndex, i) ||
                !Reversible(stack, lastAccessIndex, i)) return;

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
            
            var beforeA = GetThreadsAt(stack, lastAccessIndex - 1);
            if (beforeA.List.Count > step.Id && beforeA.List[step.Id].Enabled)
            {
                candidateThreadIds.Add((uint) step.Id);
            }
            var lookingFor = new HashSet<uint>();
            for (uint j = 0; j < beforeA.List.Count; ++j)
            {
                if (j != a.Id &&
                    j != step.Id &&
                    beforeA.List[(int) j].Enabled)
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

            // Make sure at least one candidate is backtracked

            if (candidateThreadIds.Count == 0)
            {
                throw new SchedulingStrategyException("DPOR: There were no candidate backtrack points.");
            }

            // Is one already backtracked?
            foreach (var tid in candidateThreadIds)
            {
                if (beforeA.List[(int) tid].Backtrack)
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
                        beforeA.List[(int)sleptThread].Sleep)
                    {
                        beforeA.List[(int)sleptThread].Backtrack = true;
                        return;
                    }
                    ++sleptThread;
                    if (sleptThread >= numThreads)
                    {
                        sleptThread = 0;
                    }
                }
            }

            // None are slept. So just pick one.
            // Start from thread b.tid:
            {
                uint backtrackThread = (uint)step.Id;
                for (uint k = 0; k < numThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread))
                    {
                        beforeA.List[(int)backtrackThread].Backtrack = true;
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
                targetIdToListOfSends = new List<uint>[temp];
                for (int i = 0; i < temp; ++i)
                {
                    targetIdToListOfSends[i] = new List<uint>();
                }
            }
            else
            {
                Array.Clear(threadIdToLastOpIndex, 0, threadIdToLastOpIndex.Length);
                Array.Clear(targetIdToLastCreateStartEnd, 0, targetIdToLastCreateStartEnd.Length);
                Array.Clear(targetIdToLastSend, 0, targetIdToLastSend.Length);
                foreach (List<uint> t in targetIdToListOfSends)
                {
                    t.Clear();
                }
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

            
        }

    }
}