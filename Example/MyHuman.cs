using System;

namespace Example
{
    public class MyHuman : IHuman
    {
        #region Implementation of IHuman

        public int Eat(ref int a, double b, object o, IHuman h)
        {
            Console.WriteLine($"I ate {a}, {b}, {o}, {h}");
            a = 2;
            return 1;
        }

        #endregion
    }
}