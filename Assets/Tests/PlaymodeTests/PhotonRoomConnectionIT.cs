using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon.LoadBalancing;
using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skaillz.Ubernet.Tests.IT
{
    public class PhotonRoomConnectionIT : ConnectionTestBase
    {
        private readonly PhotonSettings _settings = PlaymodeTestUtils.GetPhotonSettings();

        [SetUp]
        public void BeforeEach()
        {
            UsePhoton(_settings);
        }

        [UnityTest]
        public IEnumerator SendsMessageToOtherClient_WhenTwoClientsAreConnected()
        {
            yield return Connect();
            
            _connection.SendEvent(50, "foo", MessageTarget.Others);

            string value = null;
            
            _connection2.OnEvent
                .Subscribe(evt => value = (string) evt.Data);
            
            yield return UpdateWhile(() => value == null);
            Assert.AreEqual("foo", value);

            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator ReceivesJoinEventWithClient_WhenOtherClientJoinsRoom()
        {
            yield return Connect(0, true);

            IClient receivedClient = null;
            _connection.OnClientJoin.Subscribe(p => receivedClient = p);

            yield return Connect(1, false);
            
            yield return UpdateWhile(() => receivedClient == null);
            
            Assert.AreEqual(_connection2.LocalClient.ClientId, receivedClient.ClientId);

            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator ReceivesLeaveEventWithClient_WhenOtherClientLeavesRoom()
        {
            yield return Connect();

            int secondIdBeforeLeave = _connection2.LocalClient.ClientId;
            _connection2.Disconnect();
            IClient receivedClient = null;
            _connection.OnClientLeave.Subscribe(p => receivedClient = p);

            yield return UpdateWhile(() => receivedClient == null);
            
            Assert.AreEqual(secondIdBeforeLeave, receivedClient.ClientId);

            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator MigratesHost_WhenMasterClientLeavesRoom()
        {
            yield return Connect();

            // _connection should be set as the server
            Assert.AreEqual(_connection2.Server.ClientId, _connection.LocalClient.ClientId, "Wrong initial server");
            _connection.Disconnect();

            yield return UpdateUntilSubscription(_connection2.OnHostMigration);
            Assert.AreEqual(_connection2.Server, _connection2.LocalClient, "Wrong server after leave");

            yield return UpdateUntilSubscription(_connection2.Disconnect());
        }
        
        [UnityTest]
        public IEnumerator MigrateHost_PerformsHostMigration()
        {
            yield return Connect();

            // _connection should be set as the server
            Assert.AreEqual(_connection2.Server.ClientId, _connection.LocalClient.ClientId, "Wrong initial server");
            _connection.MigrateHost(_connection2.LocalClient.ClientId);

            yield return UpdateUntilSubscription(_connection2.OnHostMigration);
            Assert.AreEqual(_connection2.Server, _connection2.LocalClient, "Wrong server after migration");

            yield return Disconnect();
        }
    }
}