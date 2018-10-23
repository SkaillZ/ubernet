namespace Skaillz.Ubernet.NetworkEntities
{
    public interface IPlayer : ICustomSerializable, IClientIdResolvable
    {
        NetworkEntityManager Manager { get; set; }
    }
}