// -----------------------------------------------------------------------
// <copyright file="WorkflowImportException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Exception thrown when a YAML workflow import fails.
/// </summary>
/// <remarks>
/// Raised by <see cref="IWorkflowDesignerService.ImportFromYaml"/> and
/// <see cref="WorkflowYamlSerializer.Deserialize"/> when the provided
/// YAML string cannot be parsed into a valid <see cref="WorkflowDefinition"/>.
/// </remarks>
public class WorkflowImportException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowImportException"/> class.
    /// </summary>
    public WorkflowImportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowImportException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WorkflowImportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowImportException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WorkflowImportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
