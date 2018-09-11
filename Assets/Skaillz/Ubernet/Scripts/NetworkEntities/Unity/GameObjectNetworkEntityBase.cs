
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Linq;
#endif

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    [DisallowMultipleComponent]
    public abstract class GameObjectNetworkEntityBase : MonoBehaviour, INetworkEntity, IRegistrationCallbacks
    {
        private NetworkEntity _entity;
        [SerializeField] private int _id = 1;
        [SerializeField] private bool _reliable = true;

        protected virtual void Awake()
        {
            _entity = new NetworkEntity(_id);
        }
        
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int OwnerId
        {
            get { return _entity.OwnerId; }
            set { _entity.OwnerId = value; }
        }
        
        public virtual NetworkEntityManager Manager
        {
            get { return _entity.Manager; }
            set { _entity.Manager = value; }
        }

        public bool IsActive => enabled;

        public bool Reliable
        {
            get => _reliable;
            set => _reliable = value;
        }

        public IReadOnlyList<INetworkComponent> Components => _entity.Components;
        public IObservable<INetworkComponent> OnComponentAdd => _entity.OnComponentAdd;
        public IObservable<INetworkComponent> OnComponentRemove => _entity.OnComponentRemove;

        public void AddNetworkComponent(INetworkComponent component)
        {
            _entity.AddNetworkComponent(component);
        }

        public bool RemoveNetworkComponent(INetworkComponent component)
        {
            return _entity.RemoveNetworkComponent(component);
        }

        public bool RemoveNetworkComponent(short componentId)
        {
            return _entity.RemoveNetworkComponent(componentId);
        }

        public void RemoveAllNetworkComponents()
        {
            _entity.RemoveAllNetworkComponents();
        }

        public void Serialize(Stream stream)
        {
            _entity.Serialize(stream);
        }

        public void Deserialize(Stream stream)
        {
            _entity.Deserialize(stream);
        }
        
        public void OnRegister()
        {
            foreach (var component in GetComponents<MonoNetworkComponent>())
            {
                _entity.RegisterNetworkComponent(component);
            }
        }

        public void OnRemove()
        {
            Destroy(gameObject);
        }

        public override string ToString()
        {
            return $"MonoNetworkEntity {_entity}";
        }
        
#if UNITY_EDITOR
        private void Reset()
        {
            // Auto-assign an ID that is valid in the scene
            int maxId = Resources.FindObjectsOfTypeAll<GameObjectNetworkEntityBase>()
                .Select(entity => entity.Id)
                .Max();

            checked
            {
                Id = Math.Max(NetworkEntityManager.MinSafeEntityId, maxId + 1);
            }
        }

        private void OnValidate()
        {
            if (_id < NetworkEntityManager.MinSafeEntityId)
            {
                _id = NetworkEntityManager.MinSafeEntityId;
            }
        }

        // TODO: implement for non-mono components
        internal void ReassignComponentIds()
        {
            var components = GetComponents<MonoNetworkComponent>();
            if (components.Length > short.MaxValue)
            {
                throw new OverflowException("Too many components on this entity!");
            }
            
            for (short i = 1; i < components.Length; i++)
            {
                var component = components[i];
                component.Id = i;
            }
        }
#endif
    }
}