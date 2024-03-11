// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Communication.JobRouter;

namespace JR_AOAI_Integration
{
    internal static class RouterValueExtensions
    {
        internal static RouterValue? ToRouterValue(this object value)
        {
            return value switch
            {
                double dv => new RouterValue(dv),
                int iv => new RouterValue(iv),
                bool bv => new RouterValue(bv),
                null => null,
                _ => new RouterValue(Convert.ToString(value))
            };
        }
    }
}
