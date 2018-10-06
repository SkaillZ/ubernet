namespace Skaillz.Ubernet.NetworkEntities
{
    public class DefaultPlayer : PlayerBase.Synced
    {
        public DefaultPlayer()
        {
        }

        public DefaultPlayer(string name)
        {
            Name.Value = name;
        } 
        
        public SyncedString Name { get; set; } = new SyncedString();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }
}