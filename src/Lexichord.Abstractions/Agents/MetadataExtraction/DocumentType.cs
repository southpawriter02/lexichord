// -----------------------------------------------------------------------
// <copyright file="DocumentType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines document type classifications for metadata extraction (v0.7.6b).
//   The MetadataExtractor uses these types to categorize documents based on
//   their structure, content patterns, and purpose.
//
//   Classification is performed by the LLM during metadata extraction and
//   considers factors such as:
//   - Document structure (headings, lists, code blocks)
//   - Writing style (formal, informal, technical)
//   - Content patterns (instructions, narrative, reference)
//   - Domain indicators (academic citations, legal clauses)
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.MetadataExtraction;

/// <summary>
/// Classification of document types for metadata extraction.
/// </summary>
/// <remarks>
/// <para>
/// Document types are inferred by the <see cref="IMetadataExtractor"/> during metadata
/// extraction based on content analysis. The classification helps with:
/// </para>
/// <list type="bullet">
///   <item><description>Organizing documents in workspaces</description></item>
///   <item><description>Filtering search results by document type</description></item>
///   <item><description>Applying type-specific processing rules</description></item>
///   <item><description>Generating appropriate summaries and descriptions</description></item>
/// </list>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
public enum DocumentType
{
    /// <summary>
    /// Document type could not be determined or does not fit other categories.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used as a fallback when the LLM cannot confidently classify
    /// the document or when the content is too ambiguous.
    /// </remarks>
    Unknown = 0,

    /// <summary>
    /// Article or blog post format with narrative structure.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Clear introduction, body, and conclusion
    /// - Informative or opinion-based content
    /// - Often includes author attribution
    /// - May have publication date
    /// </remarks>
    Article = 1,

    /// <summary>
    /// Step-by-step tutorial content with learning objectives.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Sequential numbered steps
    /// - Learning outcomes or prerequisites
    /// - Code examples or practical exercises
    /// - Progressive complexity
    /// </remarks>
    Tutorial = 2,

    /// <summary>
    /// How-to guide with practical instructions for specific tasks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Task-focused structure
    /// - Numbered or bulleted steps
    /// - Expected outcomes or results
    /// - Troubleshooting sections
    /// </remarks>
    HowTo = 3,

    /// <summary>
    /// Reference documentation or API documentation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Structured format (tables, parameter lists)
    /// - Technical terminology
    /// - Cross-references between sections
    /// - Version or compatibility information
    /// </remarks>
    Reference = 4,

    /// <summary>
    /// API documentation with endpoint definitions and schemas.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - HTTP method and endpoint patterns
    /// - Request/response examples
    /// - Authentication requirements
    /// - Schema or type definitions
    /// </remarks>
    APIDocumentation = 5,

    /// <summary>
    /// Technical specification or design document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Formal structure with numbered sections
    /// - Requirements or acceptance criteria
    /// - Diagrams or architectural descriptions
    /// - Version history or change log
    /// </remarks>
    Specification = 6,

    /// <summary>
    /// Report or analysis document with findings and conclusions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Executive summary
    /// - Data analysis or findings
    /// - Recommendations or action items
    /// - Charts, graphs, or statistics
    /// </remarks>
    Report = 7,

    /// <summary>
    /// Whitepaper with in-depth technical or business analysis.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Problem statement and solution proposal
    /// - Technical depth with supporting evidence
    /// - Industry context or market analysis
    /// - References or citations
    /// </remarks>
    Whitepaper = 8,

    /// <summary>
    /// Proposal document for project, budget, or initiative.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Objective or goal statement
    /// - Scope and deliverables
    /// - Timeline and milestones
    /// - Budget or resource requirements
    /// </remarks>
    Proposal = 9,

    /// <summary>
    /// Meeting notes or minutes with discussion points.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Date, attendees, and location
    /// - Agenda items or discussion topics
    /// - Action items with assignees
    /// - Decisions or resolutions
    /// </remarks>
    Meeting = 10,

    /// <summary>
    /// Personal notes or informal documentation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Informal structure
    /// - Personal observations or reminders
    /// - Mixed content types
    /// - May lack formal organization
    /// </remarks>
    Notes = 11,

    /// <summary>
    /// README file for project or repository documentation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Project title and description
    /// - Installation or setup instructions
    /// - Usage examples
    /// - Contributing guidelines or license
    /// </remarks>
    Readme = 12,

    /// <summary>
    /// Changelog documenting version history and changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Characterized by:
    /// - Version numbers or dates
    /// - Added, changed, removed, fixed sections
    /// - Breaking changes or migration notes
    /// - Chronological ordering
    /// </remarks>
    Changelog = 13,

    /// <summary>
    /// Document type that doesn't fit standard categories.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for documents that have identifiable characteristics
    /// but don't match predefined types. Different from Unknown in that
    /// the content is clear but the type is non-standard.
    /// </remarks>
    Other = 14
}
