# LCS-DS-v0.18.2a-SEC: Design Specification — Command Parser & Analyzer

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.2a-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.2-SEC                          |
| **Release Version**   | v0.18.2a                                     |
| **Component Name**    | Command Parser & Analyzer                    |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Command Parser & Analyzer** (v0.18.2a). This component is the entry point for the Command Sandboxing system. It is responsible for taking raw command text from a user, parsing it into a structured and normalized format, and performing initial analysis to extract key components like executables, arguments, and shell metacharacters. Its accuracy is fundamental to the success of all subsequent security checks.

---

## 3. Detailed Design

### 3.1. Objective

Provide a high-performance, multi-shell command parser that can reliably deconstruct command strings into a normalized `ParsedCommand` object, which can then be safely analyzed by the Risk Classification Engine.

### 3.2. Scope

-   Define an `ICommandParser` interface for parsing and validation logic.
-   Implement parsers for major shell syntaxes: **bash**, **PowerShell**, and **cmd.exe**. An auto-detect mechanism will be included but will default to `bash` on non-Windows systems and `PowerShell` on Windows.
-   Normalize the parsed output into a common `ParsedCommand` record, abstracting away shell-specific syntax differences.
-   Extract key components: executable, arguments, flags, redirections (`>`, `>>`, `|`), environment variable references, and command substitution (`$()`, `` ` ``).
-   Handle complex shell grammar, including quoting (single and double), escape sequences, and pipe chains.
-   Ensure high performance to avoid being a bottleneck in command processing.

### 3.3. Detailed Architecture

The `CommandParser` will use a factory pattern to select the appropriate parser based on the specified `ShellType`. Each shell-specific parser will be a dedicated class that implements a common internal parsing interface. The core of each parser will be a hand-written lexer/parser combination for maximum performance and control over security-sensitive logic. Using a third-party parser generator like ANTLR is avoided here to prevent supply chain risks and to have fine-grained control over the parsing logic.

```mermaid
graph TD
    A[ICommandParser.ParseAsync(commandText, shellType)] --> B{CommandParserFactory};
    B -- shellType --> C{Selects appropriate parser};
    C --> D1[BashParser];
    C --> D2[PowerShellParser];
    C --> D3[CmdParser];
    
    subgraph ParserLogic
        D1 --> E{Lexer (Tokenizes input)};
        E --> F{Parser (Builds Abstract Syntax Tree)};
        F --> G{Transformer (Converts AST to ParsedCommand)};
    end

    G --> H[ParsedCommand Object];
    A --> H;

```

#### 3.3.1. Parsing Strategy

1.  **Lexer (Tokenizer)**: The input command string is broken down into a stream of tokens (e.g., `EXECUTABLE`, `ARGUMENT`, `PIPE`, `REDIRECT_OUT`, `STRING_LITERAL`). The lexer will be stateful to handle quoting and escape sequences correctly.
2.  **Parser (AST Builder)**: The token stream is consumed by a recursive descent parser that builds an Abstract Syntax Tree (AST) representing the command's structure. This tree will capture the relationships between commands in a pipeline, redirections, and arguments.
3.  **Transformer**: The AST is traversed and transformed into the final, normalized `ParsedCommand` record. This step resolves shell-specific constructs into a common format. For example, both `$(whoami)` (bash) and `(whoami)` (PowerShell) would be transformed into a `CommandSubstitution` node in the `ParsedCommand` object's representation.

This multi-stage process allows for clear separation of concerns and makes the system more extensible for supporting new shell types in the future.

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Analyzes and parses commands from various shell syntaxes into a structured, normalized representation.
/// </summary>
public interface ICommandParser
{
    /// <summary>
    /// Parses a raw command string into a structured ParsedCommand object.
    /// </summary>
    /// <param name="commandText">The raw command text.</param>
    /// <param name="shellType">The shell syntax to use. Defaults to AutoDetect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A structured representation of the command.</returns>
    /// <exception cref="CommandParseException">Thrown if the command syntax is invalid.</exception>
    Task<ParsedCommand> ParseAsync(string commandText, ShellType shellType = ShellType.AutoDetect, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quickly validates the syntax of a command without performing a full parse.
    /// </summary>
    /// <param name="commandText">The command string to validate.</param>
    /// <param name="shellType">The shell syntax to use.</param>
    /// <returns>A result indicating if the syntax is valid, with error details if not.</returns>
    Task<CommandValidationResult> ValidateAsync(string commandText, ShellType shellType = ShellType.AutoDetect);
}

/// <summary>
/// A normalized, structured representation of a single command or a pipeline of commands.
/// </summary>
public record ParsedCommand(
    IReadOnlyList<CommandSegment> Segments,
    string RawText,
    ShellType DetectedShellType);

/// <summary>
/// A single command unit within a pipeline.
/// </summary>
public record CommandSegment(
    string Executable,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<EnvironmentVariableReference> EnvironmentVariables,
    IReadOnlyList<Redirection> Redirections);

/// <summary>
/// The shell syntax type.
/// </summary>
public enum ShellType
{
    AutoDetect,
    Bash,
    PowerShell,
    Cmd
}

/// <summary>
/// Represents a redirection of an I/O stream (e.g., stdout, stderr).
/// </summary>
public record Redirection(
    StreamType SourceStream, // e.g., Stdout
    RedirectionType Type,   // e.g., Overwrite, Append
    string Target);         // e.g., "output.txt"

/// <summary>
/// Represents a reference to an environment variable within the command.
/// </summary>
public record EnvironmentVariableReference(string Name, int StartIndex, int Length);

// Other enums like StreamType, RedirectionType, etc.

public record CommandValidationResult(bool IsValid, string? ErrorMessage);
public class CommandParseException : Exception { /* ... */ }
```

### 3.5. Error Handling

-   **Invalid Syntax**: If the parser encounters a syntax error (e.g., an unclosed quote), it will immediately stop and throw a `CommandParseException`. The exception will contain a detailed message including the position of the error.
-   **Unsupported Syntax**: If the parser encounters a valid but unsupported shell feature (e.g., a complex `zsh`-specific glob pattern), it will log a warning and parse the feature as a literal string to ensure fail-safe behavior.
-   **Auto-Detection Failure**: If `ShellType.AutoDetect` is used and the shell cannot be determined, the system will default to a "basic" parser that performs simple whitespace splitting and treats all metacharacters as literals. This ensures that even unknown commands can be processed, albeit with a higher-than-normal risk score from the classification engine.

### 3.6. Security Considerations

-   **Parser Security**: The parser itself must be secure against attacks. A malicious command string should never be able to cause a stack overflow, infinite loop, or unhandled exception within the parser. All loops and recursion in the parser must have depth limits.
-   **Normalization is Key**: The primary security function of the parser is to normalize commands. This prevents attackers from using shell-specific obfuscation techniques (e.g., complex quoting, command substitution) to hide the true intent of a command from the `RiskClassificationEngine`.
-   **No Execution**: The parser must **never** execute any part of the command string. Features like command substitution (`$()`) must be parsed into the AST as such, but not executed.

### 3.7. Performance Considerations

-   **Throughput**: The parser is on the critical path for every command. The target performance is to parse a typical command in under 5ms.
-   **Lexer Optimization**: The lexer will be the most performance-critical part. It will be implemented as a simple, highly optimized state machine that reads the input string character by character without backtracking.
-   **Object Allocation**: The parser will be designed to minimize memory allocations. Tokens and AST nodes can be represented by structs where possible, and string allocations will be kept to a minimum.

### 3.8. Testing Strategy

-   **Fuzz Testing**: The parser will be subjected to fuzz testing, where a wide range of random, malformed, and malicious inputs are fed to it to ensure it does not crash and handles all inputs gracefully.
-   **Unit Tests**: A comprehensive suite of unit tests will cover:
    -   All supported shell syntaxes (bash, PowerShell, cmd).
    -   Complex quoting and escape sequences.
    -   All redirection and pipe operators.
    -   Command substitution patterns.
    -   Various forms of environment variable references.
    -   Known shell obfuscation techniques.
-   **Benchmark Tests**: Performance tests will measure the parser's throughput and latency under load, ensuring it meets the <5ms target for typical commands.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `ICommandParser`         | The core interface for the parsing service.                              |
| `CommandParser`          | The default implementation using the factory pattern.                    |
| `BashParser`             | The dedicated parser for bash/sh/zsh syntax.                             |
| `PowerShellParser`       | The dedicated parser for PowerShell syntax.                              |
| `ParsedCommand`          | The C# record for the normalized output.                                 |
| Fuzz & Unit Test Suite   | A comprehensive set of tests to ensure correctness and security.         |
| Performance Benchmarks   | Tests to validate the performance targets.                               |

---

## 5. Acceptance Criteria

-   [ ] The parser successfully handles all major shell syntaxes (bash, PowerShell, cmd.exe) for common command patterns.
-   [ ] The parser correctly detects and reports malformed command syntax with clear, actionable error messages.
-   [ ] The parser accurately extracts command components (executable, arguments, flags, redirections) with 100% accuracy for a corpus of 500+ test commands.
-   [ ] Performance benchmarks show an average processing time of <5ms per command for typical commands.
-   [ ] The parser correctly identifies all forms of environment variable references for each shell type.
-   [ ] The parser correctly handles complex nested quoting, escape sequences, and metacharacters.
-   [ ] Pipe chains and command substitution patterns are correctly identified and represented in the `ParsedCommand` structure.
-   [ ] The unit test suite achieves >95% code coverage, with a specific focus on security-related edge cases and malformed inputs.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`Lexichord.Abstractions`**: For common types if any are needed.
-   No external parsing libraries (e.g., ANTLR) will be used to minimize dependencies and security risks.

### 6.2. Integration Points
-   **`RiskClassificationEngine` (v0.18.2b)**: This is the primary consumer of the `ParsedCommand` object. The accuracy of the parser directly impacts the effectiveness of the risk engine.
-   **`CommandSandbox` (Core Orchestrator)**: The main sandbox service will be the first entry point, calling `ICommandParser` before any other processing step.
