namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the contract for loading style sheets from various sources.
/// </summary>
/// <remarks>
/// LOGIC: IStyleSheetLoader handles YAML parsing and validation.
/// It supports loading from files, streams, and embedded resources.
///
/// Full implementation in v0.2.1c will use YamlDotNet for deserialization.
/// This interface allows the StyleEngine to remain decoupled from YAML details.
///
/// Design Decisions:
/// - Async-first: file I/O is inherently async
/// - Multiple sources: files for user sheets, embedded for defaults
/// - Validation separate: ValidateYaml allows checking before loading
/// </remarks>
public interface IStyleSheetLoader
{
    /// <summary>
    /// Loads a style sheet from a file path.
    /// </summary>
    /// <param name="filePath">Absolute path to the YAML file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded style sheet.</returns>
    /// <exception cref="FileNotFoundException">File does not exist.</exception>
    /// <exception cref="InvalidOperationException">YAML parsing failed.</exception>
    /// <remarks>
    /// LOGIC: Primary method for loading user-provided style sheets.
    /// Validates YAML syntax and schema before returning.
    /// </remarks>
    Task<StyleSheet> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a style sheet from a stream.
    /// </summary>
    /// <param name="stream">Stream containing YAML content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded style sheet.</returns>
    /// <remarks>
    /// LOGIC: Used for loading from embedded resources or network.
    /// Caller is responsible for stream lifecycle.
    /// </remarks>
    Task<StyleSheet> LoadFromStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the embedded default style sheet.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default style sheet.</returns>
    /// <remarks>
    /// LOGIC: Loads lexichord.yaml from embedded resources.
    /// This is the "out of box" experience before user customization.
    /// Always succeeds or throws on assembly corruption.
    /// </remarks>
    Task<StyleSheet> LoadEmbeddedDefaultAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates YAML content without fully loading it.
    /// </summary>
    /// <param name="yamlContent">YAML string to validate.</param>
    /// <returns>Validation result with success/failure and error details.</returns>
    /// <remarks>
    /// LOGIC: Used for syntax checking in editor before save.
    /// Lighter weight than full LoadFromStreamAsync.
    /// </remarks>
    StyleSheetLoadResult ValidateYaml(string yamlContent);
}
