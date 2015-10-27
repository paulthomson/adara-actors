namespace Example
{
    public class Phoner : IPhoner
    {
        private int id;
        private IAnswerPhone _answerPhone;

        public Phoner(int id)
        {
            this.id = id;
        }

        #region Implementation of IPhoner

        public void SetAnswerPhone(IAnswerPhone answerPhone)
        {
            _answerPhone = answerPhone;
        }

        public void Go()
        {
            _answerPhone.LeaveMessage($"Phoner {id}", "hello");
            _answerPhone.LeaveMessage($"Phoner {id}", "hi again!");
        }

        #endregion
    }
}