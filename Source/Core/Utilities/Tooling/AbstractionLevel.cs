// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Determine the level of abstraction used during exploration.
    /// </summary>
    public enum AbstractionLevel
    {
        /// <summary>
        /// Basic P# hash: state, inbox and next-action.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Only hash the machine inboxes during exploration.
        /// </summary>
        InboxOnly,

        /// <summary>
        /// Use the customized state abstraction during exploration.
        /// </summary>
        Custom,

        /// <summary>
        /// Use the default P# hash and any additional custom hash provided.
        /// </summary>
        Full
    }
}
