using System;

namespace RLBot.Exceptions
{
    public class RLException : Exception
    {
        public RLException() { }

        public RLException(string message) : base(message) { }
    }
}
