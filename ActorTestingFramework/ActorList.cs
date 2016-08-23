using System.Collections.Generic;
using System.Text;

namespace ActorTestingFramework
{
    public class ActorList
    {
        private readonly List<ActorInfo> actorList;
        private readonly ActorInfo selected;

        public ActorList(List<ActorInfo> actorList, ActorInfo selected)
        {
            this.actorList = actorList;
            this.selected = selected;
        }

        #region Overrides of Object

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n\n");
            foreach (var actorInfo in actorList)
            {
                string prefix = "";
                if (actorInfo.enabled)
                {
                    prefix = "   ";
                }
                if (actorInfo == selected)
                {
                    prefix = ">  ";
                }
                sb.Append(prefix + actorInfo + "(" + actorInfo.currentOp + ")\n");
            }
            sb.Append("\n");
            return sb.ToString();

        }

        #endregion
    }
}