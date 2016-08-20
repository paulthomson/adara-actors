using System.Collections.Generic;
using System.Text;

namespace ActorTestingFramework
{
    public class ActorList
    {
        private readonly List<ActorInfo> actorList;
        private readonly int selectedId;

        public ActorList(List<ActorInfo> actorList, int selectedId)
        {
            this.actorList = actorList;
            this.selectedId = selectedId;
        }

        #region Overrides of Object

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var actorInfo in actorList)
            {
                sb.Append(actorInfo.id.id + "(" + actorInfo.currentOp +
                          (actorInfo.id.id == selectedId ? "*) " : ") "));
            }
            return sb.ToString();

        }

        #endregion
    }
}