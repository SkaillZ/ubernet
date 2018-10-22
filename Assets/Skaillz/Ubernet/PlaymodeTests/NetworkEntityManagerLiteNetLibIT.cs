using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;

namespace Skaillz.Ubernet.Tests.IT
{
    public class NetworkEntityManagerLiteNetLibIT : NetworkEntityManagerTestBase
    {
        [SetUp]
        public override void BeforeEach()
        {
            base.BeforeEach();
            UseLiteNetLib();
        }
    }
}