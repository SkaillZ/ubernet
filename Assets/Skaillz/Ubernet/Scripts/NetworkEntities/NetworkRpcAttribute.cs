using System;

namespace Skaillz.Ubernet.NetworkEntities
{
    /// <summary>
    /// Methods with this attribute can be called via RPC.
    /// </summary>
    ///
    /// See <see cref="RpcHandler"/>.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class NetworkRpcAttribute : Attribute
    {
    }
}