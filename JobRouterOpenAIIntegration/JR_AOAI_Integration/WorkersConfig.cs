// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace JR_AOAI_Integration
{
    internal class WorkersConfig
    {
        public IDictionary<string, WorkerConfig> Defaults { get; set; }
    }

    internal class WorkerConfig
    {
        public string QueueId { get; set; }

        public IDictionary<string, object> Labels { get; set; }

        public IDictionary<string, object> Tags { get; set; }
    }
}
