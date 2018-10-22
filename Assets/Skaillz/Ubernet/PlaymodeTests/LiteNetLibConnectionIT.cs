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
    public class LiteNetLibConnectionIT : ConnectionTestBase
    {
        [SetUp]
        public void BeforeEach()
        {
            UseLiteNetLib();
        }

        [UnityTest]
        public IEnumerator SendsMessageToClient_WhenTwoClientsAreConnected()
        {
            yield return Connect();

            yield return UpdateUntilSubscription(Observable.Interval(TimeSpan.FromMilliseconds(10)));
            
            string value = null;
            _connection2.OnEvent.Subscribe(evt =>
            {
                value = (string) evt.Data;
            });
            
            _connection.SendEvent(50, "foo", MessageTarget.Others);

            yield return UpdateWhile(() => value == null);
            Assert.AreEqual("foo", value);

            yield return Disconnect();
        }
        
        [UnityTest]
        public IEnumerator SendsMessageToServer_WhenTwoClientsAreConnected()
        {
            yield return Connect();

            yield return UpdateUntilSubscription(Observable.Interval(TimeSpan.FromMilliseconds(10)));
            
            string value = null;
            _connection.OnEvent.Subscribe(evt =>
            {
                value = (string) evt.Data;
            });
            
            _connection2.SendEvent(50, "foo", MessageTarget.Others);

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
        
        [UnityTest, Ignore("")]
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
    }
}