// -----------------------------------------------------------------------
// <copyright file="ConnectionStatus.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Presentation;

/// <summary>
/// Represents the connection status of an LLM provider.
/// </summary>
/// <remarks>
/// <para>
/// This enum is used by the LLM Settings UI to indicate the current connection
/// state of each configured provider. The states progress through a lifecycle:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Unknown"/>: Initial state, no test performed</description></item>
///   <item><description><see cref="Checking"/>: Connection test in progress</description></item>
///   <item><description><see cref="Connected"/>: Test succeeded, provider is accessible</description></item>
///   <item><description><see cref="Failed"/>: Test failed, see status message for details</description></item>
/// </list>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection status has not been checked.
    /// </summary>
    /// <remarks>
    /// This is the default state when a provider is first loaded or after
    /// the API key is modified. The user should perform a connection test
    /// to verify the provider is accessible.
    /// </remarks>
    Unknown = 0,

    /// <summary>
    /// Connection test is currently in progress.
    /// </summary>
    /// <remarks>
    /// The UI should display a loading indicator (spinner) while in this state.
    /// User interactions that would initiate another test should be disabled.
    /// </remarks>
    Checking = 1,

    /// <summary>
    /// Successfully connected to the provider.
    /// </summary>
    /// <remarks>
    /// The API key is valid and the provider API is accessible.
    /// The UI should display a green checkmark or success indicator.
    /// </remarks>
    Connected = 2,

    /// <summary>
    /// Connection test failed.
    /// </summary>
    /// <remarks>
    /// The test could fail for various reasons:
    /// <list type="bullet">
    ///   <item><description>Invalid or expired API key</description></item>
    ///   <item><description>Network connectivity issues</description></item>
    ///   <item><description>Provider API is unavailable</description></item>
    ///   <item><description>Rate limiting</description></item>
    /// </list>
    /// The UI should display a red error indicator and the status message
    /// should contain details about the failure.
    /// </remarks>
    Failed = 3
}
