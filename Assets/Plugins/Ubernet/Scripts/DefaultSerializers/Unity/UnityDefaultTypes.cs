namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    /// <summary>
    /// Default types used by the entity manager. Custom events should not be sent with any of those codes.
    /// </summary>
    public static class UnityDefaultTypes
    {
        /// <summary>
        /// Used for sending RPCs.
        /// </summary>
        public const byte Vector2 = 40;
        public const byte Vector3 = 41;
        public const byte Quaternion = 42;
        public const byte Color = 43;
    }
}