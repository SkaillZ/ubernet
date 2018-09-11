namespace Skaillz.Ubernet.NetworkEntities
{
    /// <summary>
    /// Default types used by the entity manager. Custom events should not be sent with any of those codes.
    /// </summary>
    public class DefaultTypes
    {
        /// <summary>
        /// Used for sending RPCs.
        /// </summary>
        public const byte Rpc = 30;
    }
}