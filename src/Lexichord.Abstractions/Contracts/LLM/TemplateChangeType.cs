// -----------------------------------------------------------------------
// <copyright file="TemplateChangeType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Defines the type of change that occurred to a prompt template.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration is used in <see cref="TemplateChangedEventArgs"/> to indicate
/// what kind of modification was made to a template in the repository.
/// </para>
/// <para>
/// Template changes can occur due to:
/// </para>
/// <list type="bullet">
///   <item><description>Hot-reload detecting a new or modified template file</description></item>
///   <item><description>Hot-reload detecting a deleted template file</description></item>
///   <item><description>Manual repository reload via <see cref="IPromptTemplateRepository.ReloadTemplatesAsync"/></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// repository.TemplateChanged += (sender, args) =>
/// {
///     switch (args.ChangeType)
///     {
///         case TemplateChangeType.Added:
///             Console.WriteLine($"New template added: {args.TemplateId}");
///             break;
///         case TemplateChangeType.Updated:
///             Console.WriteLine($"Template updated: {args.TemplateId}");
///             break;
///         case TemplateChangeType.Removed:
///             Console.WriteLine($"Template removed: {args.TemplateId}");
///             break;
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="TemplateChangedEventArgs"/>
/// <seealso cref="IPromptTemplateRepository.TemplateChanged"/>
public enum TemplateChangeType
{
    /// <summary>
    /// A new template was added to the repository.
    /// </summary>
    /// <remarks>
    /// This occurs when:
    /// <list type="bullet">
    ///   <item><description>A new template file is created in a watched directory</description></item>
    ///   <item><description>A template is loaded during repository initialization</description></item>
    ///   <item><description>A reload discovers a previously unknown template</description></item>
    /// </list>
    /// The <see cref="TemplateChangedEventArgs.Template"/> property will contain
    /// the newly added template.
    /// </remarks>
    Added,

    /// <summary>
    /// An existing template was modified and reloaded.
    /// </summary>
    /// <remarks>
    /// This occurs when:
    /// <list type="bullet">
    ///   <item><description>A template file is modified in a watched directory</description></item>
    ///   <item><description>A higher-priority template overrides a lower-priority one</description></item>
    ///   <item><description>A reload detects changes to an existing template</description></item>
    /// </list>
    /// The <see cref="TemplateChangedEventArgs.Template"/> property will contain
    /// the updated template with new content.
    /// </remarks>
    Updated,

    /// <summary>
    /// A template was removed from the repository.
    /// </summary>
    /// <remarks>
    /// This occurs when:
    /// <list type="bullet">
    ///   <item><description>A template file is deleted from a watched directory</description></item>
    ///   <item><description>A reload no longer finds a previously loaded template</description></item>
    /// </list>
    /// The <see cref="TemplateChangedEventArgs.Template"/> property will be <c>null</c>
    /// since the template no longer exists.
    /// </remarks>
    Removed
}
