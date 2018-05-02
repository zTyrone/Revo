﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Revo.Core.Events;
using Revo.DataAccess.Entities;
using Revo.Domain.Events;

namespace Revo.Infrastructure.EF6.Events.Async
{
    [TablePrefix(NamespacePrefix = "RAE", ColumnPrefix = "EER")]
    [DatabaseEntity]
    public class ExternalEventRecord
    {
        public ExternalEventRecord(Guid id, string eventJson, string eventName, int eventVersion,
            string metadataJson)
        {
            Id = id;
            EventJson = eventJson;
            EventName = eventName;
            EventVersion = eventVersion;
            MetadataJson = metadataJson;
        }

        protected ExternalEventRecord()
        { 
        }

        public Guid Id { get; private set; }
        public string EventName { get; private set; }
        public int EventVersion { get; private set; }
        public string EventJson { get; private set; }
        public string MetadataJson { get; private set; }
    }
}
