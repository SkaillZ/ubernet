using System;

namespace Skaillz.Ubernet
{
    public class UbernetException : Exception
    {
        public UbernetException(string message) : base(message)
        {
        }
    }
    
    public class UnknownTypeException : UbernetException
    {
        public UnknownTypeException(string message) : base(message)
        {
        }
    }
    
    public class ConnectionException : UbernetException
    {
        public ConnectionException(string message) : base(message)
        {
        }

        public ConnectionException(string message, DisconnectReason reason) : base(message)
        {
            Reason = reason;
        }

        public DisconnectReason Reason { get; private set; }
    }

    public class ServerFullException : UbernetException
    {
        public ServerFullException(string message) : base(message)
        {
        }
    }
    
    public class GameJoinException : UbernetException
    {
        public GameJoinException(string message) : base(message)
        {
        }
    }
    
    public class GameDoesNotExistException : GameJoinException
    {
        public GameDoesNotExistException() : base("The game does not exist.")
        {
        }
    }
    
    public class NoRandomGameFoundException : GameJoinException
    {
        public NoRandomGameFoundException() : base("No random game found.")
        {
        }
    }
    
    public class GameFullException : GameJoinException
    {
        public GameFullException() : base("The game is full.")
        {
        }
    }
    
    public class UnsupportedGameTypeException : UbernetException
    {
        public UnsupportedGameTypeException(string message) : base(message)
        {
        }
    }
}