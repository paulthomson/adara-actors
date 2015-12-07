using System;
using System.Collections.Generic;
using System.Text;
using ActorInterface;

namespace Example
{
    public class AnswerPhone : IAnswerPhone
    {
        private readonly List<Tuple<string, string>> _messages =
            new List<Tuple<string, string>>();
         
        #region Implementation of IAnswerPhone

        public void LeaveMessage(string name, string message)
        {
            _messages.Add(new Tuple<string, string>(name, message));
        }

        public void CheckMessages(IMailbox<string> res)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var m in _messages)
            {
                sb.Append($"From: {m.Item1};\n Message: {m.Item2}\n\n");
            }
            res.Send(sb.ToString());
        }

        public string CheckMessagesSync(int a, out int b, string t)
        {
            b = 3;
            StringBuilder sb = new StringBuilder();
            const int v = 55;
            if (a != v)
            {
                throw new ArgumentException($"{nameof(a)} must be {v}");
            }
            sb.Append(a + ", ");
            sb.Append(t + " ---\n\n");
            foreach (var m in _messages)
            {
                sb.Append($"From: {m.Item1};\n Message: {m.Item2}\n\n");
            }
            return sb.ToString();
        }

        #endregion
    }
}