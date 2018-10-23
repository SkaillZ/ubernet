using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;

namespace Skaillz.Ubernet.Tests.IT
{
    public class NetworkEntityManagerLiteNetLibIT : NetworkEntityManagerTestBase
    {
        [SetUp]
        public void BeforeEach()
        {
            UseLiteNetLib();
        }
    }
}