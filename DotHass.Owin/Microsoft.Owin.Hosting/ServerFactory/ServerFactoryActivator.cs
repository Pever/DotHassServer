// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Owin.Hosting.ServerFactory
{
    /// <summary>
    /// Used to instantiate the server factory.
    /// </summary>
    public class ServerFactoryActivator : IServerFactoryActivator
    {
        private readonly IServiceProvider _services;

        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        public ServerFactoryActivator(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Instantiate an instance of the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object Activate(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
        }
    }
}