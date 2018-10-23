using System.Collections;
using System.Collections.Generic;

namespace Skaillz.Ubernet
{
    public interface IMessageTarget
    {
    }

    public class MessageTarget : IMessageTarget
    {
        public static IMessageTarget Others { get; } = new MessageTarget();
        public static IMessageTarget AllPlayers { get; } = new MessageTarget();
        public static IMessageTarget Server { get; } = new MessageTarget();

        private MessageTarget() {}
    }
}