﻿using NUnit.Framework;
using Skaillz.Ubernet.Providers.Photon;

namespace Skaillz.Ubernet.Tests.IT
{
    public class NetworkEntityManagerPhotonIT : NetworkEntityManagerTestBase
    {
        private readonly PhotonSettings _settings = PlaymodeTestUtils.GetPhotonSettings();

        [SetUp]
        public override void BeforeEach()
        {
            base.BeforeEach();
            UsePhoton(_settings);
        }
    }
}