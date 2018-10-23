using System.Collections;
using NSubstitute;
using NUnit.Framework;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.Tests.IT;
using UnityEngine.TestTools;

namespace Skaillz.Ubernet.Tests
{
    public class NetworkEntityManagerTest : NetworkEntityManagerTestBase
    {
        [SetUp]
        public void BeforeEach()
        {
            UseMock();
        }
        
        [UnityTest]
        public IEnumerator Initialize_Throws_IfSetPlayerWasNotCalled()
        {
            yield return Connect();

            Assert.Throws<PlayerNotSetException>(() => _manager.Initialize());
        }
    }
}