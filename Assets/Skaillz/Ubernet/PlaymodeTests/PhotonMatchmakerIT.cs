using System;
using System.Collections;
using ExitGames.Client.Photon.LoadBalancing;
using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skaillz.Ubernet.Tests.IT
{
    public class PhotonMatchmakerIT
    {
        private PhotonMatchmaker _service;
        private PhotonMatchmaker _service2;
        
        private LoadBalancingClient _client;
        private LoadBalancingClient _client2;

        private readonly PhotonSettings _settings = PlaymodeTestUtils.GetPhotonSettings();

        [SetUp]
        public void BeforeEach()
        {
            _client = new LoadBalancingClient();
            _service = new PhotonMatchmaker(_client);
            
            // Second client for two-client tests
            _client2 = new LoadBalancingClient();
            _service2 = new PhotonMatchmaker(_client2);
        }

        [UnityTest]
        public IEnumerator Connect_ConnectsToPhoton()
        {
            yield return UpdateUntilSubscription(_service.ConnectToCloudWithSettings(_settings));

            Assert.IsTrue(_client.IsConnectedAndReady, "did not connect");

            yield return UpdateUntilSubscription(_service.Disconnect());
        }

        [UnityTest]
        public IEnumerator Disconnect_DisconnectsFromPhoton()
        {
            yield return UpdateUntilSubscription(_service.ConnectToCloudWithSettings(_settings));
            yield return UpdateUntilSubscription(_service.Disconnect());
            
            Assert.AreEqual(ClientState.Disconnected, _client.State);
        }

        [UnityTest]
        public IEnumerator CreateGame_CreatesPhotonRoomAndJoins()
        {
            yield return UpdateUntilSubscription(_service.ConnectToCloudWithSettings(_settings));

            yield return UpdateUntilSubscription(_service.CreateGame(new PhotonCreateGameOptions()));
            Assert.AreEqual(ClientState.Joined, _client.State);
            
            yield return UpdateUntilSubscription(_service.Disconnect());
        }
        
        
        [UnityTest]
        public IEnumerator JoinGame_Throws_IfGameDoesNotExist()
        {
            yield return UpdateUntilSubscription(_service.ConnectToCloudWithSettings(_settings));

            yield return UpdateUntilError(_service.FindGame(new PhotonGameQuery
            {
                RoomName = "foo"
            }));
            
            Debug.Log(_lastError);
            Assert.That(_lastError, Is.TypeOf<GameDoesNotExistException>());
            
            yield return UpdateUntilSubscription(_service.Disconnect());
        }

        [UnityTest]
        public IEnumerator JoinsRoom_WhenTwoClientsAreConnected()
        {
            yield return UpdateUntilSubscription(
                Observable.WhenAll(_service.ConnectToCloudWithSettings(_settings), _service2.ConnectToCloudWithSettings(_settings)));
            
            yield return UpdateUntilSubscription(_service.CreateGame(new PhotonCreateGameOptions
            {
                RoomName = "foo"
            }));
            
            yield return UpdateUntilSubscription(_service2.FindGame(new PhotonGameQuery
            {
                RoomName = "foo"
            }));
            
            Assert.AreEqual(ClientState.Joined, _client2.State);
            
            yield return UpdateUntilSubscription(_service2.Disconnect());
            yield return UpdateUntilSubscription(_service.Disconnect());
        }

        private object _lastValue;
        private Exception _lastError;
        
        private IEnumerator UpdateUntilSubscription<T>(IObservable<T> observable)
        {
            bool returned = false;
            observable.Subscribe(v =>
            {
                _lastValue = v;
                returned = true;
            }, err => Assert.Fail("error: " + err));
            while (!returned)
            {
                _service.Update();
                _service2.Update();
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        private IEnumerator UpdateUntilError<T>(IObservable<T> observable)
        {
            _lastError = null;
            bool returned = false;
            observable.Subscribe(v => Assert.Fail($"Observable resolved with value: {v}"),
                err =>
                {
                    _lastError = err;
                    returned = true;
                });
            while (!returned)
            {
                _service.Update();
                _service2.Update();
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        private IEnumerator UpdateWhile(Func<bool> condition)
        {
            while (condition())
            {
                _service.Update();
                _service2.Update();
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}