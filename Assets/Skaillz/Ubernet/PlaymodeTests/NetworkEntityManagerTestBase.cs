using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.Providers;
using Skaillz.Ubernet.Providers.Mock;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skaillz.Ubernet.Tests.IT
{
    public abstract class NetworkEntityManagerTestBase : ConnectionTestBase
    {
        protected NetworkEntityManager _manager;
        protected NetworkEntityManager _manager2;

        [SetUp]
        public virtual void BeforeEach()
        {
            _manager = new NetworkEntityManager();
            _manager2 = new NetworkEntityManager();
        }

        [UnityTest]
        public IEnumerator CreatesEntity_OnOtherClient()
        {
            yield return Connect();

            var entity = new NetworkEntity(1, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);

            yield return UpdateUntilSubscription(_manager2.OnEntityCreated);
            Assert.AreEqual(_manager2.Entities.First().Id, 1);
            
            yield return Disconnect();
        }

        [UnityTest]
        public IEnumerator DestroysEntity_OnOtherClient()
        {
            yield return Connect();

            var entity = new NetworkEntity(1, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);
            _manager.DestroyEntity(entity.Id);

            yield return UpdateUntilSubscription(_manager2.OnEntityDestroyed);
            Assert.AreEqual(_manager2.Entities.Count, 0);
            
            yield return Disconnect();
        }

        [UnityTest]
        public IEnumerator CreatesComponentOnEntity_OnOtherClient()
        {
            yield return Connect();

            var entity = new NetworkEntity(1, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);

            yield return UpdateUntilSubscription(_manager2.OnEntityCreated);

            var comp = new TestNetworkComponent {Id = 2};
            entity.AddNetworkComponent(comp);

            yield return UpdateUntilSubscription(_manager2.Entities[0].OnComponentAdd);

            Assert.AreEqual(2, _manager2.Entities[0].Components[0].Id);
            
            yield return Disconnect();
        }

        [UnityTest]
        public IEnumerator DestroysComponentOnEntity_OnOtherClient()
        {
            yield return Connect();

            var entity = new NetworkEntity(1, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);

            yield return UpdateUntilSubscription(_manager2.OnEntityCreated);

            var comp = new TestNetworkComponent {Id = 2};
            entity.AddNetworkComponent(comp);
            entity.RemoveNetworkComponent(comp);

            yield return UpdateUntilSubscription(_manager2.Entities[0].OnComponentRemove);

            Assert.AreEqual(0, _manager2.Entities[0].Components.Count);
            
            yield return Disconnect();
        }

        [UnityTest]
        public IEnumerator UpdatesComponentOnEntity_OnOtherClient()
        {
            yield return Connect();

            var entity = new NetworkEntity(1, _manager.Connection.LocalClient.ClientId);
            _manager.InstantiateEntity(entity);

            yield return UpdateUntilSubscription(_manager2.OnEntityCreated);

            var comp = new TestNetworkComponent {Id = 2, Data = 0};
            entity.AddNetworkComponent(comp);
            yield return UpdateUntilSubscription(_manager2.Entities[0].OnComponentAdd);
            comp.Data = 1;
            yield return UpdateUntilSubscription(_manager2.OnEntityUpdated);

            Assert.AreEqual(1, ((TestNetworkComponent) _manager2.Entities[0].Components[0]).Data);
            
            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator JoinRoom_AddsOtherPlayersToList_AfterJoining()
        {
            yield return Connect();
            _manager.SetLocalPlayer(new TestPlayer());
            yield return UpdateUntilSubscription(_manager.Initialize());
            
            _manager2.SetLocalPlayer(new TestPlayer());
            yield return UpdateUntilSubscription(_manager2.Initialize());

            Assert.AreEqual(2, _manager2.Players.Count);
            
            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator JoinRoom_DeserializesOtherPlayers_AfterJoining()
        {
            yield return Connect();
            
            _manager.SetLocalPlayer(new TestPlayer());
            _manager.GetLocalPlayer<TestPlayer>().Data = 1;
            yield return UpdateUntilSubscription(_manager.Initialize());
            
            _manager2.SetLocalPlayer(new TestPlayer());
            yield return UpdateUntilSubscription(_manager2.Initialize());

            var remotePlayer = (TestPlayer) _manager2.GetPlayer(_manager.LocalPlayer.ClientId);
            Assert.AreEqual(1, remotePlayer.Data);
            
            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator LeaveRoom_RemovesPlayerOnOtherClient()
        {
            yield return ConnectAndInitialize();

            bool left = false;
            _manager2.OnPlayerLeft.Subscribe(_ => left = true);
            _manager.Connection.Disconnect();
            
            while (!left)
            {
                yield return DoUpdate();
            }

            Assert.AreEqual(1, _manager2.Players.Count);
            
            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator UpdatesPlayer_OnOtherClient()
        {
            yield return ConnectAndInitialize();
            
            var remotePlayer = (TestPlayer) _manager2.GetPlayer(_manager.LocalPlayer.ClientId);

            Assert.AreEqual(0, remotePlayer.Data);
            _manager.GetLocalPlayer<TestPlayer>().Data = 1;

            yield return UpdateUntilSubscription(_manager2.OnPlayerUpdated);
            
            Assert.AreEqual(1, remotePlayer.Data);

            yield return Disconnect();
        }
        
        protected override IEnumerator DoUpdate(bool returned = false)
        {
            _manager.Update();
            _manager2.Update();

            yield return base.DoUpdate(returned);
        }

        protected override IEnumerator Connect()
        {
            yield return base.Connect();

            _manager = new NetworkEntityManager(_connection);
            _manager2 = new NetworkEntityManager(_connection2);
        }

        protected IEnumerator Initialize()
        {
            yield return UpdateUntilSubscription(Observable.WhenAll(
                _manager.SetLocalPlayer(new TestPlayer()).Initialize(),
                _manager2.SetLocalPlayer(new TestPlayer()).Initialize()
           ));
        }
        
        protected IEnumerator ConnectAndInitialize()
        {
            yield return Connect();
            yield return Initialize();
        }

        private abstract class TestSerializable : ICustomSerializable
        {
            public byte Data { get; set; }

            public void Serialize(Stream stream)
            {
                stream.WriteByte(Data);
            }

            public void Deserialize(Stream stream)
            {
                Data = (byte) stream.ReadByte();
            }
        }

        private class TestNetworkComponent : TestSerializable, INetworkComponent
        {
            public INetworkEntity Entity { get; set; }
            public short Id { get; set; }
        }

        private class TestPlayer : TestSerializable, IPlayer
        {
            public int ClientId { get; set; }
            public NetworkEntityManager Manager { get; set; }
        }
    }
}