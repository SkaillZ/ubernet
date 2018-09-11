namespace Skaillz.Ubernet
{
    public enum DisconnectReason
    {
        Unknown,
        CleanDisconnect,
        Timeout,
        DisconnectedByServer,
        ExceededLimits,
        Exception,
        Unauthorized
    }
}