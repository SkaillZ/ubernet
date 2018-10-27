namespace Skaillz.Ubernet
{
    public class Client : IClient
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