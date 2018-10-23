using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using NSubstitute;
using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;
using UniRx;
using UnityEngine;

namespace Skaillz.Ubernet.Tests
{
    public class PhotonMatchmakerTest
    {
        private LoadBalancingClient _photonClient;
        private readonly PhotonSettings _settings = new PhotonSettings
        {
            Region = "foo",
            AppId = "bar",
            AppVersion = "baz"
        };
        
        [SetUp]
        public void BeforeEach()
        {
            _photonClient = Substitute.ForPartsOf<LoadBalancingClient>("", "", "", ConnectionProtocol.Udp);
            _photonClient.When(p => p.ConnectToRegionMaster(Arg.Any<string>())).DoNotCallBase();
        }
        
        [Test]
        public void ConnectToCloudWithSettings_ThrowsException_IfSettingsAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PhotonMatchmaker(_photonClient).ConnectToCloudWithSettings(null));
        }
        
        [Test]
        public void ConnectToCloudWithSettings_ThrowsException_IfFieldsAreMissingInSettings()
        {
            Assert.Throws<ArgumentException>(() => new PhotonMatchmaker(_photonClient).ConnectToCloudWithSettings(new PhotonSettings()));
        }
        
        [Test]
        public void ConnectToCloudWithSettings_SetsPropertiesOnClient_IfSettingsAreValid()
        {
            new PhotonMatchmaker(_photonClient).ConnectToCloudWithSettings(_settings);
            Assert.AreEqual(_settings.AppId, _photonClient.AppId);
            Assert.AreEqual(_settings.AppVersion, _photonClient.AppVersion);
        }
    }
}