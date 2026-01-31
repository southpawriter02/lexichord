// <copyright file="ProfileSelectorWidget.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Avalonia.Controls;

namespace Lexichord.Modules.StatusBar.Views;

/// <summary>
/// Code-behind for the ProfileSelectorWidget.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4d - Minimal code-behind as all logic is in the ViewModel.</para>
/// <para>The context menu binds directly to AvailableProfiles and SelectProfileCommand.</para>
/// </remarks>
public partial class ProfileSelectorWidget : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileSelectorWidget"/> class.
    /// </summary>
    public ProfileSelectorWidget()
    {
        InitializeComponent();
    }
}
