using Skaillz.Ubernet.DefaultSerializers.Unity;
using Skaillz.Ubernet.Providers.Mock;
using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    public sealed class GameObjectNetworkEntity : GameObjectNetworkEntityBase
    {
        internal static bool AutoRegister = true;
        
        protected override void Awake()
        {
            base.Awake();
            if (UnityUtils.EntityManager == null)
            {
                #if DEBUG
                UnityUtils.EntityManager = NetworkEntityManager.Create(new MockConnection(true).AutoUpdate())
                    .RegisterUnityDefaultTypes()
                    .SetLocalPlayer(new DefaultPlayer("DebugPlayer"));
                
                Debug.LogWarning($"{nameof(UnityUtils)}.{nameof(UnityUtils.EntityManager)} is null. " +
                                  "A mock connection has been created, so you can test your game in the editor. " +
                                 $"Create a {nameof(NetworkEntityManager)} before any network entities are active " +
                                  "if you want to connect over the network. Note that this causes an exception " +
                                  "outside the Editor in a non-debug build.");
                #else
                enabled = false;
                foreach (var comp in GetComponents<MonoNetworkComponent>())
                {
                    comp.enabled = false;
                }
                    
                throw new UbernetException("The NetworkEntity could not be activated because " +
                    $"{nameof(UnityUtils)}.{nameof(UnityUtils.EntityManager)} is null. Please connect to a provider " +
                     " and set it to an entity manager before using entities.");
                #endif
            }

            var manager = UnityUtils.EntityManager;
            if (OwnerId == -1 && AutoRegister && !manager.IsEntityRegistered(this))
            {
                // Register scene objects immediately
                manager.RegisterEntity(this);
            }
        }
    }
}