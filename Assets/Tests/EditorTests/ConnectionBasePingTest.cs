using System.Collections;
using NUnit.Framework;
using Skaillz.Ubernet.Tests.IT;
using UniRx;
using UnityEngine.TestTools;

namespace Skaillz.Ubernet.Tests
{
    public class ConnectionBasePingTest : ConnectionTestBase
    {
        [SetUp]
        public void BeforeEach()
        {
            UseMock();
        }

        [UnityTest]
        public IEnumerator PingServer_SendsPingToServer()
        {
            yield return Connect();
            
            bool receivedPing = false;

            _connection.OnEvent(DefaultEvents.Ping).First().Subscribe(_ => receivedPing = true);

            yield return UpdateUntilSubscription(_connection2.PingServer());
            
            Assert.IsTrue(receivedPing);

            yield return Disconnect();
        }
    }
}