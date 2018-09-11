using System.Collections.Generic;
using System.Linq;

namespace Skaillz.Ubernet
{
    /// <summary>
    /// A list of clients other objects that represent a client by resolving to its <see cref="IClient.ClientId"/>.
    /// </summary>
    /// Can be used as a <see cref="MessageTarget"/> when sending events.
    public class ClientList : List<IClientIdResolvable>, IClientIdListResolvable
    {
        public static ClientList FromClients(params IClientIdResolvable[] clients)
        {
            var list = new ClientList();
            list.AddRange(clients);
            return list;
        }
        
        public int[] GetClientIds()
        {
            return this.Select(c => c.ClientId).ToArray();
        }
    }
}