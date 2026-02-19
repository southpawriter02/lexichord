using System.Threading.Tasks;

namespace Lexichord.Abstractions.Services;

/// <summary>
/// Interface for a service that displays message boxes.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Shows a message box with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowMessageAsync(string title, string message);
}
