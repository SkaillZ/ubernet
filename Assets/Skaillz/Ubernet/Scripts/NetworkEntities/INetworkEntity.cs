using System;
using System.Collections.Generic;
using System.IO;

namespace Skaillz.Ubernet.NetworkEntities
{
    /// <summary>
    /// A serializable entity belonging to a player.
    /// </summary>
    ///
    /// Network entities can be Unity MonoBehaviours or custom objects.
    /// 
    public interface INetworkEntity : ICustomSerializable
    {
        /// <summary>
        /// The unique ID of the entity
        /// </summary>
        int Id { get; set; }
        
        /// <summary>
        /// The ID of the owning <see cref="IClient"/>
        /// </summary>
        int OwnerId { get; set; }
        
        /// <summary>
        /// Whether the entity is active. If false, no network updates are sent.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Specifies if the entities' serialization events should be sent reliably or unreliably.
        /// </summary>
        /// This setting does not affect RPCs, which are sent reliably by default.
        ///
        bool Reliable { get; set; }
        
        /// <summary>
        /// The <see cref="NetworkEntityManager"/> that manages the entity
        /// </summary>
        NetworkEntityManager Manager { get; set; }
        
        /// <summary>
        /// A read-only list of components belonging to this entity.
        /// </summary>
        IReadOnlyList<INetworkComponent> Components { get; }

        /// <summary>
        /// Called when a component is added to the entity.
        /// See also: <seealso cref="AddNetworkComponent"/>
        /// </summary>
        IObservable<INetworkComponent> OnComponentAdd { get; }
        
        /// <summary>
        /// Called when a component is removed from the entity.
        /// See also: <seealso cref="RemoveNetworkComponent(Skaillz.Ubernet.NetworkEntities.INetworkComponent)"/>
        /// </summary>
        IObservable<INetworkComponent> OnComponentRemove { get; }

        void AddNetworkComponent(INetworkComponent component);
        bool RemoveNetworkComponent(INetworkComponent component);
        bool RemoveNetworkComponent(short componentId);
        void RemoveAllNetworkComponents();
    }
}