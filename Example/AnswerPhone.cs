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

        #endregion
    }
}