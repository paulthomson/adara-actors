using System.Collections.Generic;

namespace FailureDetectorExample
{
    public class Node : INode
    {
        private readonly INode self;

        public Node(INode self)
        {
            this.self = self;
        }

        #region Implementation of INode

        public void Ping(IFailureDetector detector)
        {
            detector.Pong(self);
        }

        #endregion
    }
}