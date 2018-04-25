﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Revo.Core.Commands;
using Revo.Core.Events;

namespace Revo.Integrations.Rebus.Events
{
    public class RebusEventCommandHandler
        : ICommandHandler<HandleRebusEventsCommand>
    {
        private readonly IPublishEventBuffer publishEventBuffer;

        public RebusEventCommandHandler(IPublishEventBuffer publishEventBuffer)
        {
            this.publishEventBuffer = publishEventBuffer;
        }

        public Task HandleAsync(HandleRebusEventsCommand command, CancellationToken cancellationToken)
        {
            command.Messages.ForEach(publishEventBuffer.PushEvent);
            return Task.FromResult(0);
        }
    }
}
