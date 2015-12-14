using System.Collections.Generic;
using TypedActorInterface;

namespace FailureDetectorExample
{
    public class FailureDetector : IFailureDetector
    {
        private ITypedActorRuntime TR;

        private List<INode> _nodes;
        private ISet<IClient> _clients;
        private int _attempts;
        private ISet<INode> _alive;
        private ISet<INode> _responses;
        private ITimer _timer;

        public FailureDetector(ITypedActorRuntime tr)
        {
            TR = tr;
        }

        private enum States
        {
            Init = 0,
            SendPing,
            WaitForCancelResponse,
            Reset
        }

        #region Implementation of IFailureDetector

        public void Init(List<INode> nodes)
        {
            if (TR.IsInState((int) States.Init))
            {
                _nodes = nodes;
                _clients = new HashSet<IClient>();
                _alive = new HashSet<INode>();
                _responses = new HashSet<INode>();
                _alive.UnionWith(_nodes);
                _timer = TR.Create<ITimer>(null);

                TR.PushState((int) States.SendPing);

                return;
            }

            TR.StateError();
        }

        public void Pong(INode node)
        {
            if (TR.IsInState((int) States.SendPing))
            {
                // ...
                return;
            }

            if (TR.IsInState((int) States.WaitForCancelResponse))
            {
                TR.Defer();
                return;
            }

            if (TR.IsInState((int) States.Reset))
            {
                // ignore
                return;
            }

            TR.StateError();
        }

        #endregion
    }
}