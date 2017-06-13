using System.Collections.Generic;
using ActorTestingFramework;

namespace Microsoft.PSharp.TestingServices.Scheduling.POR
{
    /// <summary>
    /// Thread entry stored on the stack of a depth-first search to track which threads existed
    /// and whether they have been executed already, etc.
    /// </summary>
    public class TidEntry
    {
        /// <summary>
        /// The id/index of this thread in the original thread creation order list of threads.
        /// </summary>
        public int Id;

        /// <summary>
        /// Is the thread enabled?
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Is the thread selected for exploration?
        /// </summary>
        public bool Selected;

        /// <summary>
        /// Skip exploring this thread from here.
        /// </summary>
        public bool Sleep;

        /// <summary>
        /// Backtrack to this transition?
        /// </summary>
        public bool Backtrack;

        /// <summary>
        /// Operation type.
        /// </summary>
        public OpType OpType;

        /// <summary>
        /// Target of the operation.
        /// </summary>
        public int TargetId;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enabled"></param>
        /// <param name="opType"></param>
        /// <param name="targetId"></param>
        public TidEntry(int id, bool enabled, OpType opType, int targetId)
        {
            Id = id;
            Enabled = enabled;
            Selected = false;
            Sleep = false;
            Backtrack = false;
            OpType = opType;
            TargetId = targetId;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Comparer ComparerSingleton = new Comparer();

        /// <summary>
        /// 
        /// </summary>
        public class Comparer : IEqualityComparer<TidEntry>
        {
            #region Implementation of IEqualityComparer<in Comparer>

            /// <summary>Determines whether the specified objects are equal.</summary>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            public bool Equals(TidEntry x, TidEntry y)
            {
                return
                    x.Enabled == y.Enabled &&
                    x.Id == y.Id &&
                    x.OpType == y.OpType &&
                    x.TargetId == y.TargetId;
            }

            /// <summary>Returns a hash code for the specified object.</summary>
            /// <returns>A hash code for the specified object.</returns>
            /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
            /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is null.</exception>
            public int GetHashCode(TidEntry obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + obj.Id.GetHashCode();
                    hash = hash * 23 + obj.OpType.GetHashCode();
                    hash = hash * 23 + obj.TargetId.GetHashCode();
                    hash = hash * 23 + obj.Enabled.GetHashCode();
                    return hash;
                }
            }

            #endregion
        }

    }
}