using System;

namespace Skaillz.Ubernet.NetworkEntities
{
    public class PlayerNotSetException : UbernetException
    {
        public PlayerNotSetException(string message): base(message)
        {
        }
    }
}