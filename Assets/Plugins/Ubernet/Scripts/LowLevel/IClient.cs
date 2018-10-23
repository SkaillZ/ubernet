using System;
using System.Collections.Generic;

namespace Skaillz.Ubernet
{
    /// <summary>
    /// Generic client uniquely identified by <see cref="IClient.ClientId"/>.
    /// </summary>
    /// Can be used as a <see cref="MessageTarget"/> when sending events.
    public interface IClient : IClientIdResolvable
    {
    }
}
