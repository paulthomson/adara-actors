﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ActorTestingFramework;

namespace Microsoft.PSharp.TestingServices.Scheduling.POR
{
    /// <summary>
    /// 
    /// </summary>
    public class TidEntryList
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly List<TidEntry> List;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        public TidEntryList(List<TidEntry> list)
        {
            List = list;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetAllEnabledToBeBacktracked()
        {
            foreach (var tidEntry in List)
            {
                if (tidEntry.Enabled)
                {
                    tidEntry.Backtrack = true;
                    Safety.Assert(tidEntry.Enabled);
                }
            }
        }

        public string ShowEnabled()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in List)
            {
                if (!tidEntry.Enabled)
                {
                    continue;
                }
                if (tidEntry.Selected)
                {
                    sb.Append("*");
                }
                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.TargetType);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public string ShowSelected()
        {
            int selectedIndex = TryGetSelected();
            if (selectedIndex < 0)
            {
                return "-";
            }
            TidEntry selected = List[selectedIndex];
            return $"({selected.Id}, {selected.OpType}, {selected.TargetType}, {selected.TargetId})";

        }

        public string ShowBacktrack()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in List)
            {
                if (!tidEntry.Backtrack)
                {
                    continue;
                }
                if (tidEntry.Selected)
                {
                    sb.Append("*");
                }
                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.TargetType);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetFirstBacktrackNotSlept(int startingFrom)
        {
            int size = List.Count;
            int i = startingFrom;
            bool foundSlept = false;
            for (int count = 0; count < size; ++count)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep)
                {
                    return i;
                }
                if (List[i].Sleep)
                {
                    foundSlept = true;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }

            return foundSlept ? DPORStrategy.SLEEP_SET_BLOCKED : -1;
        }

        public List<int> GetAllBacktrackNotSleptNotSelected()
        {
            List<int> res = new List<int>();
            for (int i = 0; i < List.Count; ++i)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep &&
                    !List[i].Selected)
                {
                    Safety.Assert(List[i].Enabled);
                    res.Add(i);
                }
            }
            return res;
        }

        public bool HasBacktrackNotSleptNotSelected()
        {
            foreach (TidEntry t in List)
            {
                if (t.Backtrack &&
                    !t.Sleep &&
                    !t.Selected)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSelectedToSleep()
        {
            List[GetSelected()].Sleep = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool AllDoneOrSlept()
        {
            return GetFirstBacktrackNotSlept(0) < 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int TryGetSelected()
        {
            int res = -1;
            for (int i = 0; i < List.Count; ++i)
            {
                if (List[i].Selected)
                {
                    if (res != -1)
                    {
                        Safety.Assert(false);
                        throw new SchedulingStrategyException("DFS Strategy: More than one selected tid entry!");
                    }
                    res = i;
                }
            }
            

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsNoneSelected()
        {
            return TryGetSelected() < 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetSelected()
        {
            int res = TryGetSelected();
            if (res == -1)
            {
                throw new SchedulingStrategyException("DFS Strategy: No selected tid entry!");
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearSelected()
        {
            List[GetSelected()].Selected = false;
        }

        public void AddFirstEnabledNotSleptToBacktrack(int startingFrom)
        {
            int size = List.Count;
            int i = startingFrom;
            for (int count = 0; count < size; ++count)
            {
                if (List[i].Enabled &&
                    !List[i].Sleep)
                {
                    List[i].Backtrack = true;
                    Safety.Assert(List[i].Enabled);
                    return;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }
        }
    }
}