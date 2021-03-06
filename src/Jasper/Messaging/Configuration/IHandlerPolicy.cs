﻿using Jasper.Configuration;
using Jasper.Messaging.Model;

namespace Jasper.Messaging.Configuration
{
    /// <summary>
    ///     Use to apply your own conventions or policies to message handlers
    /// </summary>
    public interface IHandlerPolicy
    {
        /// <summary>
        ///     Called during bootstrapping to alter how the message handlers are configured
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="rules"></param>
        void Apply(HandlerGraph graph, JasperGenerationRules rules);
    }
}
