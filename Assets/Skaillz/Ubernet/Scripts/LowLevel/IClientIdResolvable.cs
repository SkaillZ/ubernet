namespace Skaillz.Ubernet
{
    /// <summary>
    /// Used for various objects that represent a client. Can be used as a <see cref="IMessageTarget"/>.
    /// </summary>
    public interface IClientIdResolvable : IMessageTarget
    {
        int ClientId { get; set; }
    }
}