using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                }
            }
        }

        public string ShowEnabled()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in List.Where(entry => entry.Enabled))
            {
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
            for (int count = 0; count < size; ++count)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep)
                {
                    return i;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }

            return -1;
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