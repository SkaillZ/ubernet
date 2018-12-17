namespace Skaillz.Ubernet
{
    public class NetworkEvent
    {
        public int SenderId { get; set; }
        public byte Code { get; set; }
        public object Data { get; set; }
        public IMessageTarget Target { get; set; }
    }
}