﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xer.Cqrs.Registrations.CommandHandlers
{
    public class CompositeCommandHandlerProvider : ICommandHandlerProvider
    {
        private readonly IEnumerable<ICommandHandlerProvider> _providers;

        public CompositeCommandHandlerProvider(IEnumerable<ICommandHandlerProvider> providers)
        {
            _providers = providers;
        }

        /// <summary>
        /// Get the registered command handler delegate to handle the command of the specified type.
        /// </summary>
        /// <param name="commandType">Type of command to be handled.</param>
        /// <returns>Instance of invokeable CommandAsyncHandlerDelegate.</returns>
        public CommandAsyncHandlerDelegate GetCommandHandler(Type commandType)
        {
            foreach(ICommandHandlerProvider provider in _providers)
            {
                CommandAsyncHandlerDelegate handlerDelegate = provider.GetCommandHandler(commandType);
                if(handlerDelegate != null)
                {
                    return handlerDelegate;
                }
            }

            throw new HandlerNotFoundException($"No command handler is registered to handle command of type: { commandType.Name }");
        }
    }
}
