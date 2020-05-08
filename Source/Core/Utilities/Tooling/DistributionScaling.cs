// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Type of distribution scaling for QL.
    /// </summary>
    public enum DistributionScaling
    {
        /// <summary>
        /// No scaling.
        /// </summary>
        None = 0,

        /// <summary>
        /// Scale the average based on a lower threshold.
        /// </summary>
        ScaledAverage
    }
}
