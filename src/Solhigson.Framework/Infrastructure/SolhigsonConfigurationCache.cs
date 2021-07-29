﻿using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonConfigurationCache
    {
        private readonly ConfigurationWrapper _configurationWrapper;

        public SolhigsonConfigurationCache(ConfigurationWrapper configurationWrapper)
        {
            _configurationWrapper = configurationWrapper;
        }
        public string ProtectedFields => _configurationWrapper.GetConfig("Solhigson.Framework", "ProtectedFields", "");

    }
}