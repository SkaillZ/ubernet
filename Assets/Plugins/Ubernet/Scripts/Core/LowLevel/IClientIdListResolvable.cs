namespace Skaillz.Ubernet
{
    public interface IClientIdListResolvable : IMessageTarget
    {
        int[] GetClientIds();
    }
}