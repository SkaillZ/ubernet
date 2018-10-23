using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities
{
    public class NetworkEntity : INetworkEntity
    {
        private readonly Dictionary<short, INetworkComponent> _components = new Dictionary<short, INetworkComponent>();
        private readonly Dictionary<short, byte[]> _componentCaches = new Dictionary<short, byte[]>();
        protected readonly ISubject<INetworkComponent> _componentAddSubject = new Subject<INetworkComponent>();
        protected readonly ISubject<INetworkComponent> _componentRemoveSubject = new Subject<INetworkComponent>();

        protected readonly SerializationHelper _helper = new SerializationHelper();

        public int Id { get; set; }
        public int OwnerId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool Reliable { get; set; } = true;
        public NetworkEntityManager Manager { get; set; }
        
        public IReadOnlyList<INetworkComponent> Components => _components.Values.ToList();

        public IObservable<INetworkComponent> OnComponentAdd => _componentAddSubject.AsObservable();
        public IObservable<INetworkComponent> OnComponentRemove => _componentRemoveSubject.AsObservable();

        public NetworkEntity()
        {
            
        }

        public NetworkEntity(int id, int ownerId = -1)
        {
            Id = id;
            OwnerId = ownerId;
        }
        
        public virtual void AddNetworkComponent(INetworkComponent component)
        {
            RegisterNetworkComponent(component);
            _componentAddSubject.OnNext(component);
        }

        internal virtual void RegisterNetworkComponent(INetworkComponent component)
        {
            component.Entity = this;
            if (_components.ContainsValue(component))
            {
                throw new InvalidOperationException("The component was already registered to this entity.");
            }
            
            if (_components.ContainsKey(component.Id))
            {
                throw new InvalidOperationException($"A component with ID {component.Id} was already registered to this entity.");
            }
            
            _components[component.Id] = component;
            (component as IRegistrationCallbacks)?.OnRegister();
        }

        public virtual bool RemoveNetworkComponent(INetworkComponent component)
        {
            return RemoveNetworkComponent(component.Id);
        }

        public virtual bool RemoveNetworkComponent(short componentId)
        {
            if (!_components.ContainsKey(componentId))
            {
                return false;
            }
            
            var component = _components[componentId];
            component.Entity = null;
            (component as IRegistrationCallbacks)?.OnRemove();
            
            _components.Remove(componentId);
            _componentCaches.Remove(componentId);
            _componentRemoveSubject.OnNext(component);

            return true;
        }

        public virtual void RemoveAllNetworkComponents()
        {
            foreach (short componentId in _components.Keys.ToArray())
            {
                RemoveNetworkComponent(componentId);
            }
        }

        // TODO: refactor
        public virtual void Serialize(Stream stream)
        {
            var componentsToSerialize = new Dictionary<short, byte[]>();
            foreach (var component in _components.Values)
            {
                var id = component.Id;
                
                byte[] bytes;
                using (var memStream = new MemoryStream())
                {
                    component.Serialize(memStream);
                    bytes = memStream.ToArray();
                }

                if (!_componentCaches.ContainsKey(id) || !UbernetUtils.AreArraysEqual(bytes, _componentCaches[id]))
                {
                    componentsToSerialize[id] = bytes;
                    _componentCaches[id] = bytes;
                }
            }
            
            int updatedNum = componentsToSerialize.Count;

            if (updatedNum > 0) // Only streams with content are sent
            {
                _helper.SerializeShort((short) updatedNum, stream);
                foreach (var pair in componentsToSerialize)
                {
                    var id = pair.Key;
                    var bytes = pair.Value;
                    
                    _helper.SerializeShort(id, stream);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public virtual void Deserialize(Stream stream)
        {
            int updatedNum = _helper.DeserializeShort(stream);
            for (int i = 0; i < updatedNum; i++)
            {
                int componentId = _helper.DeserializeShort(stream);
                _components.Values.First(c => c.Id == componentId).Deserialize(stream);
            }
        }

        public override string ToString()
        {
            return $"NetworkEntity #{Id} ({(this.IsLocal() ? "local" : "non-local")}, owner: Client#{OwnerId})";
        }

        public abstract class Synced : NetworkEntity, IRegistrationCallbacks
        {
            private SyncedValueSerializer _serializer;
            
            protected Synced(int id, int ownerId = -1) : base(id, ownerId)
            {
            }
            
            public virtual void OnRegister()
            {
                _serializer = new SyncedValueSerializer(this, this.GetSerializer());
            }

            public virtual void OnRemove()
            {
            }

            public override void Serialize(Stream stream)
            {
                base.Serialize(stream);
                _serializer.Serialize(stream);
            }

            public override void Deserialize(Stream stream)
            {
                base.Deserialize(stream);
                _serializer.Deserialize(stream);
            }
        }
    }
}