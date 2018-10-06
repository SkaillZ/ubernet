using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using ExitGames.Client.Photon.LoadBalancing;
using NUnit.Framework;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.Providers;
using Skaillz.Ubernet.Providers.Mock;
using UniRx;

namespace Skaillz.Ubernet.Tests
{
    public static class TestUtils
    {
        public static NetworkEvent CreateNetworkEvent(object data, byte code = 0, int senderId = 0)
        {
            return new NetworkEvent { Data = data, Code = code, SenderId = senderId};
        }

        public static void SetState(this LoadBalancingClient client, ClientState state)
        {
            // ReSharper disable once PossibleNullReferenceException
            client.GetType()
                .GetProperty("State",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
                .GetSetMethod(true)
                .Invoke(client, new object[] { state });
        }
        
        public static void SetDisconnectedCause(this LoadBalancingClient client, DisconnectCause cause)
        {
            // ReSharper disable once PossibleNullReferenceException
            client.GetType()
                .GetProperty("DisconnectedCause",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
                .GetSetMethod(true)
                .Invoke(client, new object[] { cause });
        }

        public static T WaitForValue<T>(this IObservable<T> observable)
        {
            var cts = new CancellationTokenSource(1);
            
            var task = observable
                .ToTask(cts.Token);

            task.Wait(cts.Token);
            return task.Result;
        }

        public static ObservableAssertion<T> GetAssertion<T>(this IObservable<T> observable)
        {
            return new ObservableAssertion<T>(observable);
        }

        public class ObservableAssertion<T>
        {
            private T _lastValue = default(T);
            private bool _called = false;
            
            public ObservableAssertion(IObservable<T> observable)
            {
                observable.Subscribe(val =>
                {
                    _lastValue = val;
                    _called = true;
                });
            }

            public void AssertLastValue(T value)
            {
                Assert.AreEqual(value, _lastValue);
            }

            public void AssertCalled()
            {
                Assert.IsTrue(_called, "The observable was not called");
            }
        }

        public static NetworkEntityManager CreateManagerWithMasterMockService()
        {
            return new NetworkEntityManager(new MockConnection(true, MockConnection.MockNetwork.Default));
        }

        public static NetworkEntityManager CreateManagerWithSlaveMockService()
        {
            return new NetworkEntityManager(new MockConnection(false, MockConnection.MockNetwork.Default));

        }
    }
}