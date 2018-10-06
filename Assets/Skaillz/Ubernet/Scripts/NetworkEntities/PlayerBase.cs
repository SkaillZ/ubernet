using System.IO;

namespace Skaillz.Ubernet.NetworkEntities
{
    public abstract class PlayerBase : IPlayer
    {
        public int ClientId { get; set; }
        public NetworkEntityManager Manager { get; set; }
        
        public virtual void Serialize(Stream stream)
        {
        }

        public virtual void Deserialize(Stream stream)
        {
        }
        
        public abstract class Synced : PlayerBase, IRegistrationCallbacks
        {
            private SyncedValueSerializer _syncedValueSerializer;
            
            public virtual void OnRegister()
            {
                _syncedValueSerializer = new SyncedValueSerializer(this, Manager.GetSerializer());
            }

            public virtual void OnRemove()
            {
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