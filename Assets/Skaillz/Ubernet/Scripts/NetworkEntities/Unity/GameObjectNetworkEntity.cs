namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    public sealed class GameObjectNetworkEntity : GameObjectNetworkEntityBase
    {
        protected override void Awake()
        {
            if (UnityUtils.EntityManager == null)
            {
                enabled = false;
                foreach (var comp in GetComponents<MonoNetworkComponent>())
                {
                    comp.enabled = false;
                }
                    
                throw new UbernetException($"The NetworkEntity could not be activated because " +
                                           $"{nameof(UnityUtils)}.{nameof(UnityUtils.EntityManager)} is null. Please connect to a provider " +
                                           $" and set it to an entity manager before using entities.");
            }
            
            base.Awake();
        }

        public override NetworkEntityManager Manager => UnityUtils.EntityManager;
    }
}