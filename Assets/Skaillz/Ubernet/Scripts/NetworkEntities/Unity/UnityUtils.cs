using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;

namespace Skaillz.Ubernet
{
    public static class UnityUtils
    {
        /// <summary>
        /// The singleton <see cref="NetworkEntityManager"/> used by <see cref="GameObjectNetworkEntity"/> by default.
        /// </summary>
        public static NetworkEntityManager EntityManager { get; set; }

        public static NetworkEntityManager SetAsDefaultEntityManager(this NetworkEntityManager entityManager)
        {
            EntityManager = entityManager;
            return entityManager;
        }
    }
}