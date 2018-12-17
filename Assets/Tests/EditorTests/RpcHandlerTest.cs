using System;
using System.IO;
using JetBrains.Annotations;
using NUnit.Framework;
using Skaillz.Ubernet.NetworkEntities;
using UniRx;

namespace Skaillz.Ubernet.Tests
{
    public class RpcHandlerTest
    {
        private NetworkEntityManager _manager;
        private NetworkEntityManager _manager2;

        [SetUp]
        public void BeforeEach()
        {
            _manager = TestUtils.CreateManagerWithMasterMockService();
            _manager2 = TestUtils.CreateManagerWithSlaveMockService();

            _manager.SetLocalPlayer(new DefaultPlayer());
            _manager2.SetLocalPlayer(new DefaultPlayer());
        }
        
        [TearDown]
        public void AfterEach()
        {
            _manager.Connection.Disconnect().Subscribe();
            _manager2.Connection.Disconnect().Subscribe();
        }

        [Test]
        public void CallsRpc_OnOtherClient()
        {
            var entity1 = new NetworkEntity(0, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity1);

            var comp1 = new TestComponent
            {
                Id = 1
            };
            entity1.AddNetworkComponent(comp1);
            
            UpdateBoth();
            
            comp1.SendRpc();
            UpdateBoth();

            Assert.AreEqual(new TestComponent {A = 1, B = 2, C = "foo"}, _manager2.Entities[0].Components[0]);
        }

        [Test]
        public void ThrowsException_OnInitialize_IfRpcMethodIsOverloaded()
        {
            var entity = new NetworkEntity(0, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);
            var comp = new TestComponentWithOverload
            {
                Id = 1
            };
            Assert.Throws<NotImplementedException>(() => entity.AddNetworkComponent(comp));
        }

        private void UpdateBoth()
        {
            _manager.Update();
            _manager.Connection.Update();
            
            _manager2.Update();
            _manager2.Connection.Update();
        }

        public class TestComponent : INetworkComponent, IRegistrationCallbacks
        {
            private RpcHandler _rpcHandler;

            public INetworkEntity Entity { get; set; }
            public short Id { get; set; }

            public int A { get; set; }
            public short B { get; set; }
            public string C { get; set; }

            public void OnRegister()
            {
                _rpcHandler = new RpcHandler(this, Entity.GetSerializer());
            }

            public void OnRemove()
            {
                _rpcHandler.Dispose();
            }

            public void SendRpc()
            {
                _rpcHandler.SendRpc(nameof(MyRpc), MessageTarget.Others, 1, (short) 2, "foo");
            }

            [NetworkRpc]
            public void MyRpc(int a, short b, string c)
            {
                A = a;
                B = b;
                C = c;
            }

            public void Serialize(Stream stream)
            {
            }

            public void Deserialize(Stream stream)
            {
            }

            protected bool Equals(TestComponent other)
            {
                return A == other.A && B == other.B && string.Equals(C, other.C);
            }

#pragma warning disable 659
            public override bool Equals(object obj)
#pragma warning restore 659
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestComponent) obj);
            }

            public override string ToString()
            {
                return $"{nameof(A)}: {A}, {nameof(B)}: {B}, {nameof(C)}: {C}";
            }
        }

        public class TestComponentWithOverload : TestComponent
        {
            [NetworkRpc, UsedImplicitly]
            public void MyRpc()
            {
            }
        }
    }
}