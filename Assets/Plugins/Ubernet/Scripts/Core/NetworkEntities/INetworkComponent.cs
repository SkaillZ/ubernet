namespace Skaillz.Ubernet.NetworkEntities
{
    /// <summary>
    /// Serializable component that belongs to one <see cref="INetworkEntity"/>.
    /// </summary>
    ///
    /// Network components can be Unity MonoBehaviours or custom objects.
    /// 
    public interface INetworkComponent : ICustomSerializable
    {
        /// <summary>
        /// The entity the component belongs to
        /// </summary>
        INetworkEntity Entity { get; set; }
        
        /// <summary>
        /// The ID of the component
        /// </summary>
        /// 
        /// All component's IDs must be unique per entity. The ID is set automatically when added to an entity
        /// and should not be sent manually in most cases.
        short Id { get; set; }
    }
}