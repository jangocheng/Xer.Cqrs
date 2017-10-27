﻿using System;
using Xer.DomainDriven.Repositories;

namespace Xer.Cqrs.EventSourcing.Repositories
{
    public interface IEventSourcedAggregateRepository<TAggregate> : IAggregateRepository<TAggregate> where TAggregate : EventSourcedAggregate
    {
        TAggregate GetById(Guid aggregateId, int version);
    }
}