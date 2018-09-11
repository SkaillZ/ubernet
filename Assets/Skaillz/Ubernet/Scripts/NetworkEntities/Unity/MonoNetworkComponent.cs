using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    [RequireComponent(typeof(GameObjectNetworkEntityBase))]
    public abstract class MonoNetworkComponent : MonoBehaviour, INetworkComponent, IRegistrationCallbacks
    {
        [SerializeField] [HideInInspector] protected short _id;

        /// <summary>
        /// Whether the component should be destroyed when unregistered on its entity
        /// </summary>
        public virtual bool DestroyAutomatically => true;
        
        public INetworkEntity Entity { get; set; }

        public short Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public abstract void Serialize(Stream stream);
        public abstract void Deserialize(Stream stream);
        
        public virtual void OnRegister()
        {
        }

        public virtual void OnRemove()
        {
            if (DestroyAutomatically)
            {
                Destroy(this);
            }
        }
        
#if UNITY_EDITOR
        private void Reset()
        {
            GetComponent<GameObjectNetworkEntityBase>().ReassignComponentIds();
        }
        
        private const string ShowComponentDebugInfoPrefKey = "Ubernet.ShowComponentDebugInfo";
        public static bool ShowDebugInfo { get; set; }

        private void OnValidate()
        {
            ShowDebugInfo = EditorPrefs.GetBool(ShowComponentDebugInfoPrefKey);
        }
        
        [ContextMenu("Toggle NetworkComponent Debug Info")]
        public void ToggleDebugInfo()
        {
            ShowDebugInfo = !ShowDebugInfo;
            EditorPrefs.SetBool(ShowComponentDebugInfoPrefKey, ShowDebugInfo);
        }
#endif
        
        /// <summary>
        /// Base class for MonoBehaviour-based network components that provides RPC functionality
        /// </summary>
        public abstract class Rpc : MonoNetworkComponent
        {
            private RpcHandler _rpcHandler;

            public override void OnRegister()
            {
                _rpcHandler = new RpcHandler(this, Entity.GetSerializer());
            }

            public override void OnRemove()
            {
                _rpcHandler.Dispose();
            }

            public override void Serialize(Stream stream)
            {
            }

            public override void Deserialize(Stream stream)
            {
            }
            
            /// <summary>
            /// Sends an RPC to all other clients. Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
            /// </summary>
            /// <param name="methodName">The name of the method to call.</param>
            /// <param name="parameters">The parameters to call the RPC method with.</param>
            public virtual void SendRpc(string methodName, params object[] parameters)
            {
                _rpcHandler.SendRpc(methodName, MessageTarget.Others, parameters);
            }
            
            /// <summary>
            /// Sends an RPC. Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
            /// </summary>
            /// <param name="methodName">The name of the method to call.</param>
            /// <param name="target">The message target.</param>
            /// <param name="parameters">The parameters to call the RPC method with.</param>
            public virtual void SendRpc(string methodName, IMessageTarget target, params object[] parameters)
            {
                _rpcHandler.SendRpc(methodName, target, parameters);
            }
            
            /// <summary>
            /// Sends an RPC to all other players unreliably (meaning that delivery and order of execution are not guaranteed).
            /// Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
            /// </summary>
            /// <param name="methodName">The name of the method to call.</param>
            /// <param name="parameters">The parameters to call the RPC method with.</param>
            public virtual void SendRpcUnreliable(string methodName, params object[] parameters)
            {
                _rpcHandler.SendRpcUnreliable(methodName, MessageTarget.Others, parameters);
            }
            
            /// <summary>
            /// Sends an RPC unreliably (meaning that delivery and order of execution are not guaranteed).
            /// Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
            /// </summary>
            /// <param name="methodName">The name of the method to call.</param>
            /// <param name="target">The message target.</param>
            /// <param name="parameters">The parameters to call the RPC method with.</param>
            public virtual void SendRpcUnreliable(string methodName, IMessageTarget target, params object[] parameters)
            {
                _rpcHandler.SendRpcUnreliable(methodName, target, parameters);
            }
        }

        /// <summary>
        /// Base class for MonoBehaviour-based network components that provides RPC functionality and supports
        /// <see cref="SyncedValue"/>s.
        /// </summary>
        public abstract class Synced : Rpc
        {
            private SyncedValueSerializer _syncedValueSerializer;
            
            public override void OnRegister()
            {
                base.OnRegister();
                _syncedValueSerializer = new SyncedValueSerializer(this, Entity.GetSerializer());
            }
            
            public override void Serialize(Stream stream)
            {
                _syncedValueSerializer.Serialize(stream);
            }

            public override void Deserialize(Stream stream)
            {
                _syncedValueSerializer.Deserialize(stream);
            }
        }
    }
}