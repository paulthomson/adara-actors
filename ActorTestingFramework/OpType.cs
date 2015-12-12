namespace ActorTestingFramework
{
    public enum OpType
    {
        INVALID,
        START, END,
        CREATE, JOIN, SEND, RECEIVE,
        WaitForDeadlock,
        Yield
    }
}