namespace Example
{
    public class Phoner : IPhoner
    {
        private int _id;
        private IAnswerPhone _answerPhone;

        #region Implementation of IPhoner

        public void Init(int id, IAnswerPhone answerPhone)
        {
            _id = id;
            _answerPhone = answerPhone;
        }

        public void Go()
        {
            _answerPhone.LeaveMessage($"Phoner {_id}", "hello");
            _answerPhone.LeaveMessage($"Phoner {_id}", "hi again!");
        }

        #endregion
    }
}