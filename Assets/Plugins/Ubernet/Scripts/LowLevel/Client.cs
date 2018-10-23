using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using InternalPhotonPlayerType = ExitGames.Client.Photon.LoadBalancing.Player;

namespace Skaillz.Ubernet
{
    internal class Client : IClient
    {
        public int ClientId { get; set; }

        public Client(int id)
        {
            ClientId = id;
        }

        protected bool Equals(Client other)
        {
            return ClientId == other.ClientId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Client) obj);
        }

        public override int GetHashCode()
        {
            return ClientId;
        }

        public override string ToString()
        {
            return $"{nameof(ClientId)}: {ClientId}";
        }
    }
}