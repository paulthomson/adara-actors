using System;

namespace Example
{
    public class MyHuman : IHuman
    {
        #region Implementation of IHuman

        public void Eat(int a, double b, object o, IHuman h)
        {
            Console.WriteLine($"I ate {a}, {b}, {o}, {h}");
        }

        #endregion
    }
}