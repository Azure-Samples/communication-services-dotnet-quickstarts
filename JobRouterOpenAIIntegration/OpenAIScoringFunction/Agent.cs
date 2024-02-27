// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace OpenAIScoringFunction
{
    public class Agent
    {
        public string Id { get; set; }

        public PerformanceIndicators PerformanceIndicators { get; set; } = new();
    }

    public class PerformanceIndicators
    {
        public decimal CSAT { get; set; }

        public decimal Outcome { get; set; }

        public string AHT { get; set; }
    }
}
