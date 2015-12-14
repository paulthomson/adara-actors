using System.Collections.Generic;
using TypedActorInterface;

namespace FailureDetector2
{
    public class FailureDetector : IActorDefinition<IFailureDetector>
    {
        private readonly ITypedActorRuntime TR;

        private List<INode> _nodes;
        private ISet<IClient> _clients;
        private int _attempts;
        private ISet<INode> _alive;
        private ISet<INode> _responses;
        private ITimer _timer;

        // states
        private IFailureDetector _initState;
        private IFailureDetector _sendPingState;
        private IFailureDetector _waitForCancelResponse;


        public FailureDetector(ITypedActorRuntime tr)
        {
            TR = tr;
            _initState = new InitState(this);
            _sendPingState = new SendPing(this);
            _waitForCancelResponse = new WaitForCancelResponse(this);
        }

        

        // This method is called from an event handler. See below.
        private MsgRes HandleInit(List<INode> nodes)
        {
            _nodes = nodes;
            _clients = new HashSet<IClient>();
            _alive = new HashSet<INode>();
            _responses = new HashSet<INode>();
            _alive.UnionWith(_nodes);
            _timer = TR.Create<ITimer>(null);

            TR.PushState(_sendPingState);

            return MsgRes.Handled;
        }



        class InitState : IFailureDetector
        {
            private readonly FailureDetector d;

            public InitState(FailureDetector d)
            {
                this.d = d;
            }

            #region Implementation of IFailureDetector

            public MsgRes Init(List<INode> nodes) { return d.HandleInit(nodes); }
            public MsgRes Pong(INode node)        { return MsgRes.Error;   }

            #endregion
        }



        class SendPing : IFailureDetector
        {
            private readonly FailureDetector d;

            public SendPing(FailureDetector d)
            {
                this.d = d;
            }

            #region Implementation of IFailureDetector

            public MsgRes Init(List<INode> nodes) {           return MsgRes.Error;   }
            public MsgRes Pong(INode node)        { /* ... */ return MsgRes.Handled; }

            #endregion
        }



        class WaitForCancelResponse : IFailureDetector
        {
            private readonly FailureDetector d;

            public WaitForCancelResponse(FailureDetector d)
            {
                this.d = d;
            }

            #region Implementation of IFailureDetector

            public MsgRes Init(List<INode> nodes) { return MsgRes.Error; }
            public MsgRes Pong(INode node)        { return MsgRes.Defer; }

            #endregion
        }

    }
}