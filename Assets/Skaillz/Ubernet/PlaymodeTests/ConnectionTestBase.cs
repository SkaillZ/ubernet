using System;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using ExitGames.Client.Photon.LoadBalancing;
using NUnit.Framework;
using Skaillz.Ubernet.Providers.LiteNetLib;
using Skaillz.Ubernet.Providers.Mock;
using Skaillz.Ubernet.Providers.Photon;
using UniRx;
using UnityEngine;

namespace Skaillz.Ubernet.Tests.IT
{
    public class ConnectionTestBase
    {
        protected enum Providers
        {
            Mock,
            Photon,
            LiteNetLib
        }

        protected Providers Provider;
        
        protected IConnection _connection;
        protected IConnection _connection2;
        protected LoadBalancingClient _client;
        protected LoadBalancingClient _client2;
        private PhotonSettings _settings;
        
        protected object LastResult;

        protected void UsePhoton(PhotonSettings settings)
        {
            _settings = settings;
            Provider = Providers.Photon;
            
            _client = new LoadBalancingClient();

            // Second client for two-client tests
            _client2 = new LoadBalancingClient();
        }

        protected void UseMock()
        {
            Provider = Providers.Mock;
        }
        
        protected void UseLiteNetLib()
        {
            Provider = Providers.LiteNetLib;
        }
        
        protected virtual IEnumerator Connect()
        {
            if (Provider == Providers.Photon)
            {
                var matchmaker = new PhotonMatchmaker(_client);
                var matchmaker2 = new PhotonMatchmaker(_client2);

                yield return UpdateUntilSubscription(
                    Observable.WhenAll(
                        matchmaker.ConnectToCloudWithSettings(_settings),
                        matchmaker2.ConnectToCloudWithSettings(_settings)
                    )
                );

                yield return UpdateUntilSubscription(
                    matchmaker.CreateGame(PhotonCreateGameOptions.FromRoomName(PlaymodeTestUtils.RoomName))
                );
                var game1 = (IGame) LastResult;

                yield return UpdateUntilSubscription(
                    matchmaker2.FindGame(PhotonGameQuery.FromRoomName(PlaymodeTestUtils.RoomName))
                );
                var game2 = (IGame) LastResult;

                yield return UpdateUntilSubscription(
                    Observable.WhenAll(
                        game1.ConnectWithPhoton(),
                        game2.ConnectWithPhoton()
                    )
                );

                var connections = (IConnection[]) LastResult;
                _connection = connections[0];
                _connection2 = connections[1];
            }
            else if (Provider == Providers.Mock)
            {
                _connection = new MockConnection(true, MockConnection.MockNetwork.Default);
                _connection2 = new MockConnection(false, MockConnection.MockNetwork.Default);
            }
            else if (Provider == Providers.LiteNetLib)
            {
                _connection = LiteNetLibConnection.CreateServer(5000, 2, "0.1", false);
                _connection2 = LiteNetLibConnection.CreateClient("localhost", 5000, 2, "0.1", false);
            }
        }

        protected virtual IEnumerator Connect(int num, bool create)
        {
            if (Provider == Providers.Photon)
            {
                if (num < 0 || num > 1)
                {
                    throw new ArgumentException("Invalid service number", nameof(num));
                }

                var matchmaker = new PhotonMatchmaker(num == 0 ? _client : _client2);

                yield return UpdateUntilSubscription(
                    matchmaker.ConnectToCloudWithSettings(_settings)
                );

                if (create)
                {
                    yield return UpdateUntilSubscription(
                        matchmaker.CreateGame(PhotonCreateGameOptions.FromRoomName(PlaymodeTestUtils.RoomName))
                    );
                }
                else
                {
                    yield return UpdateUntilSubscription(
                        matchmaker.FindGame(PhotonGameQuery.FromRoomName(PlaymodeTestUtils.RoomName))
                    );
                }

                var game = (IGame) LastResult;

                yield return UpdateUntilSubscription(game.ConnectWithPhoton());
                var connection = (IConnection) LastResult;

                if (num == 0)
                {
                    _connection = connection;
                }
                else
                {
                    _connection2 = connection;
                }
            }
            else if (Provider == Providers.Mock)
            {
                if (num == 0)
                {
                    _connection = new MockConnection(true, MockConnection.MockNetwork.Default);
                }
                else
                {
                    _connection2 = new MockConnection(false, MockConnection.MockNetwork.Default);
                }
            }
            else if (Provider == Providers.LiteNetLib)
            {
                IConnection connection;
                if (create)
                {
                    connection = LiteNetLibConnection.CreateServer(5000, 2, "0.1", false);
                }
                else
                {
                    connection = LiteNetLibConnection.CreateClient("localhost", 5000, 2, "0.1", false);
                }

                if (num == 0)
                {
                    _connection = connection;
                }
                else
                {
                    _connection2 = connection;
                }
            }
        }

        protected virtual IEnumerator Disconnect()
        {
            yield return UpdateUntilSubscription(
                Observable.WhenAll(_connection.Disconnect(), _connection2.Disconnect())
            );
        }

        protected virtual IEnumerator UpdateUntilSubscription<T>(IObservable<T> observable)
        {
            bool returned = false;
            observable.Subscribe(result =>
                {
                    LastResult = result;
                    returned = true;
                },
                err => {
                    Assert.Fail(err.ToString());
                    returned = true;
                });
            while (!returned)
            {
                yield return DoUpdate();
            }
        }

        protected virtual IEnumerator DoUpdate(bool returned = false)
        {
            if (Provider == Providers.Photon && _client != null)
            {
                _client.Service();
                _client2.Service();
            }
            else
            {
                _connection?.Update();
                _connection2?.Update();
            }

            if (!returned)
            {
                if (_connection is MockConnection)
                    yield return null;
                else
                    yield return new WaitForSeconds(0.05f);
            }
        }

        protected IEnumerator UpdateWhile(Func<bool> condition)
        {
            while (condition())
            {
                _connection?.Update();
                _connection2?.Update();
                
                if (_connection is MockConnection)
                    yield return null;
                else
                    yield return new WaitForSeconds(0.05f);
            }
        }
    }
}