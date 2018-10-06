using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities
{
    /// <summary>
    /// Handles sending and executing RPCs on <see cref="INetworkComponent"/>s.
    /// </summary>
    public class RpcHandler : IDisposable
    {
        private readonly INetworkComponent _context;
        
        private Dictionary<short, MethodInfo> _rpcCodeLookup;
        private Dictionary<string, short> _rpcMethodLookup;
        private IDisposable _rpcSubscription;

        /// <summary>
        /// Creates a new instance for the given component, with the given serializer.
        /// All methods with the <see cref="NetworkRpcAttribute"/> are registered.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serializer"></param>
        public RpcHandler(INetworkComponent context, ISerializer serializer)
        {
            _context = context;
            
            if (!serializer.IsTypeRegistered(typeof(RpcCall)))
            {
                serializer.RegisterCustomType(typeof(RpcCall), DefaultTypes.Rpc, new RpcCallSerializer(serializer));
            }
            
            InitializeRpcs();
        }
        
        public void Dispose()
        {
            if (_rpcSubscription == null)
            {
                throw new InvalidOperationException($"The {nameof(SyncedValueSerializer)} instance has not been " +
                    $"initialized or had errors during initialization.");
            }
            
            _rpcSubscription.Dispose();
        }

        /// <summary>
        /// Sends an RPC. Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
        /// </summary>
        /// <param name="name">The name of the method to call.</param>
        /// <param name="target">The message target.</param>
        /// <param name="parameters">The parameters to call the RPC method with.</param>
        public void SendRpc(string name, IMessageTarget target, params object[] parameters)
        {
            SendRpc(name, target, true, parameters);
        }
        
        /// <summary>
        /// Sends an RPC unreliably (meaning that delivery and order of execution are not guaranteed).
        /// Called methods must be decorated with the <see cref="NetworkRpcAttribute"/>.
        /// </summary>
        /// <param name="name">The name of the method to call.</param>
        /// <param name="target">The message target.</param>
        /// <param name="parameters">The parameters to call the RPC method with.</param>
        public void SendRpcUnreliable(string name, IMessageTarget target, params object[] parameters)
        {
            SendRpc(name, target, false, parameters);
        }
        
        private void InitializeRpcs()
        {
            _rpcCodeLookup = new Dictionary<short, MethodInfo>();
            _rpcMethodLookup = new Dictionary<string, short>();
            
            var type = _context.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(NetworkRpcAttribute), false).Length > 0)
                .OrderBy(m => m.Name)
                .ToArray();
            
            for (short i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (_rpcMethodLookup.ContainsKey(method.Name))
                {
                    throw new NotImplementedException($"Detected overloaded RPC method '{method.Name}' " +
                        $"on component type '{type.FullName}'. Overloads are not supported yet, so please make sure " +
                         "that there aren't multiple RPC methods with the same name on the same component.");
                }

                _rpcCodeLookup[i] = method;
                _rpcMethodLookup[method.Name] = i;
            }
            
            _rpcSubscription = _context.Entity.Manager.OnEvent(DefaultEvents.Rpc)
                .Select(evt => (RpcCall) evt.Data)
                .Where(rpc => rpc.EntityId == _context.Entity.Id && rpc.ComponentId == _context.Id)
                .Subscribe(rpc =>
                {
                    Debug.Log("Got RPC: componentID " + rpc.ComponentId + ", entityId: " + rpc.EntityId);
                    var method = _rpcCodeLookup[rpc.RpcCode];
                    method.Invoke(_context, rpc.Params);
                });
        }

        private void SendRpc(string name, IMessageTarget target, bool reliable, object[] parameters)
        {
            if (!_rpcMethodLookup.ContainsKey(name))
            {
                throw new InvalidOperationException($"No RPC method with the name {name} is registered on the component." +
                    $"Check if you misspelled it or forgot to add the {nameof(NetworkRpcAttribute)} to your method.");
            }

            var call = new RpcCall
            {
                EntityId = _context.Entity.Id,
                ComponentId = _context.Id,
                RpcCode = _rpcMethodLookup[name],
                Params = parameters
            };
            Debug.Log("Send RPC: componentID " + call.ComponentId + ", entityId: " + call.EntityId);
            _context.Entity.Manager.SendEvent(DefaultEvents.Rpc, call, target, reliable);
        }

        internal class RpcCall
        {
            public int EntityId { get; set; }
            public short ComponentId { get; set; }
            public short RpcCode { get; set; }
            public object[] Params { get; set; }
        }

        internal class RpcCallSerializer : ICustomTypeSerializer
        {
            private readonly ISerializer _baseSerializer;
            private readonly SerializationHelper _helper;
            
            public RpcCallSerializer(ISerializer baseSerializer)
            {
                _baseSerializer = baseSerializer;
                _helper = new SerializationHelper();
            }
            
            public void Serialize(object value, Stream stream)
            {
                var rpcCall = (RpcCall) value;
                _helper.SerializeInt(rpcCall.EntityId, stream);
                _helper.SerializeShort(rpcCall.ComponentId, stream);
                _helper.SerializeShort(rpcCall.RpcCode, stream);

                _baseSerializer.Serialize(rpcCall.Params, stream);
            }

            public object Deserialize(Stream stream)
            {
                var rpcCall = new RpcCall
                {
                    EntityId = _helper.DeserializeInt(stream),
                    ComponentId = _helper.DeserializeShort(stream),
                    RpcCode = _helper.DeserializeShort(stream),
                    Params = (object[]) _baseSerializer.Deserialize(stream)
                };

                return rpcCall;
            }
        }
    }
}