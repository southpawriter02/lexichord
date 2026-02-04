// =============================================================================
// File: SpacySentenceParser.cs
// Project: Lexichord.Modules.Knowledge
// Description: SpaCy-based sentence parser implementation.
// =============================================================================
// LOGIC: Implements ISentenceParser using SpaCy via Python interop. Provides
//   sentence segmentation, tokenization, dependency parsing, and semantic
//   role labeling. Includes caching for parsed sentences.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: ISentenceParser, ParseOptions, ParseResult, ParsedSentence,
//               Token, DependencyNode, DependencyRelation, SemanticFrame,
//               SemanticArgument, SemanticRole, NamedEntity (all v0.5.6f),
//               IMemoryCache (v0.4.6f), ILogger<T> (v0.0.3b)
// =============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.Parsing;

/// <summary>
/// SpaCy-based sentence parser with Python interop.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses SpaCy for NLP processing. It provides:
/// <list type="bullet">
///   <item>Sentence segmentation via SpaCy's sentence boundary detection.</item>
///   <item>Tokenization with POS tagging and lemmatization.</item>
///   <item>Dependency parsing for grammatical relations.</item>
///   <item>Rule-based semantic role labeling.</item>
///   <item>Named entity recognition.</item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> Parse results are cached using sentence text hash as key.
/// Cache duration is 30 minutes by default.
/// </para>
/// <para>
/// <b>Multi-language:</b> Supports English, German, French, and Spanish.
/// </para>
/// <para>
/// <b>Note:</b> This is a mock implementation for unit testing. The production
/// implementation requires Python installed with SpaCy and language models.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public class SpacySentenceParser : ISentenceParser, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SpacySentenceParser> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private bool _disposed;

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedLanguages => new[] { "en", "de", "fr", "es" };

    /// <summary>
    /// Initializes a new instance of <see cref="SpacySentenceParser"/>.
    /// </summary>
    /// <param name="cache">Memory cache for parse results.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cache"/> or <paramref name="logger"/> is null.
    /// </exception>
    public SpacySentenceParser(IMemoryCache cache, ILogger<SpacySentenceParser> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("SpacySentenceParser initialized. Supported languages: {Languages}",
            string.Join(", ", SupportedLanguages));
    }

    /// <inheritdoc />
    public async Task<ParseResult> ParseAsync(
        string text,
        ParseOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= ParseOptions.Default;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text provided, returning empty result");
            return new ParseResult
            {
                Sentences = Array.Empty<ParsedSentence>(),
                Duration = TimeSpan.Zero,
                Stats = new ParseStats()
            };
        }

        return await Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            var sentences = new List<ParsedSentence>();
            var skippedCount = 0;

            // Segment text into sentences
            var sentenceTexts = SegmentSentences(text);
            _logger.LogDebug("Segmented text into {Count} sentences", sentenceTexts.Count);

            var sentenceIndex = 0;
            var currentOffset = 0;

            foreach (var sentenceText in sentenceTexts)
            {
                ct.ThrowIfCancellationRequested();

                // Skip if exceeds max length
                if (sentenceText.Length > options.MaxSentenceLength)
                {
                    _logger.LogDebug("Skipping sentence {Index} (length {Length} > {Max})",
                        sentenceIndex, sentenceText.Length, options.MaxSentenceLength);
                    skippedCount++;
                    currentOffset += sentenceText.Length + 1;
                    sentenceIndex++;
                    continue;
                }

                // Check cache
                ParsedSentence? parsed = null;
                string? cacheKey = null;

                if (options.UseCache)
                {
                    cacheKey = $"parse:{ComputeHash(sentenceText)}:{options.Language}";
                    if (_cache.TryGetValue(cacheKey, out parsed))
                    {
                        _logger.LogTrace("Cache hit for sentence {Index}", sentenceIndex);
                        sentences.Add(parsed!);
                        currentOffset += sentenceText.Length + 1;
                        sentenceIndex++;
                        continue;
                    }
                }

                // Parse the sentence
                parsed = ParseSentenceInternal(sentenceText, sentenceIndex, currentOffset, options);
                sentences.Add(parsed);

                // Cache the result
                if (options.UseCache && cacheKey != null)
                {
                    _cache.Set(cacheKey, parsed, _cacheExpiration);
                }

                currentOffset += sentenceText.Length + 1;
                sentenceIndex++;
            }

            sw.Stop();

            var stats = new ParseStats
            {
                TotalSentences = sentences.Count,
                TotalTokens = sentences.Sum(s => s.Tokens.Count),
                AverageTokensPerSentence = sentences.Count > 0
                    ? (float)sentences.Average(s => s.Tokens.Count)
                    : 0,
                SkippedSentences = skippedCount,
                TotalSemanticFrames = sentences.Sum(s => s.SemanticFrames?.Count ?? 0),
                TotalNamedEntities = sentences.Sum(s => s.Entities?.Count ?? 0)
            };

            _logger.LogDebug(
                "Parsed {SentenceCount} sentences with {TokenCount} tokens in {Duration}ms",
                stats.TotalSentences, stats.TotalTokens, sw.ElapsedMilliseconds);

            return new ParseResult
            {
                Sentences = sentences,
                Duration = sw.Elapsed,
                Stats = stats
            };
        }, ct);
    }

    /// <inheritdoc />
    public async Task<ParsedSentence> ParseSentenceAsync(
        string sentence,
        ParseOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= ParseOptions.Default;

        if (string.IsNullOrWhiteSpace(sentence))
        {
            return new ParsedSentence
            {
                Text = sentence ?? string.Empty,
                Tokens = Array.Empty<Token>()
            };
        }

        return await Task.Run(() =>
        {
            // Check cache
            if (options.UseCache)
            {
                var cacheKey = $"parse:{ComputeHash(sentence)}:{options.Language}";
                if (_cache.TryGetValue(cacheKey, out ParsedSentence? cached))
                {
                    return cached!;
                }
            }

            return ParseSentenceInternal(sentence, 0, 0, options);
        }, ct);
    }

    /// <summary>
    /// Segments text into sentences using simple rule-based detection.
    /// </summary>
    private List<string> SegmentSentences(string text)
    {
        var sentences = new List<string>();
        var sentenceEnders = new[] { '.', '!', '?' };
        var current = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            current.Append(c);

            if (sentenceEnders.Contains(c))
            {
                // Check for abbreviations (simple heuristic)
                var isAbbreviation = false;
                if (c == '.' && current.Length >= 2)
                {
                    // Check if previous character is uppercase (likely abbreviation)
                    var prev = current.Length >= 2 ? current[current.Length - 2] : ' ';
                    if (char.IsUpper(prev) && current.Length <= 4)
                    {
                        isAbbreviation = true;
                    }
                }

                // Check for next character to confirm sentence end
                var nextIsWhitespaceOrEnd = i + 1 >= text.Length ||
                    char.IsWhiteSpace(text[i + 1]) ||
                    char.IsUpper(text[i + 1]);

                if (!isAbbreviation && nextIsWhitespaceOrEnd)
                {
                    var sentence = current.ToString().Trim();
                    if (!string.IsNullOrEmpty(sentence))
                    {
                        sentences.Add(sentence);
                    }
                    current.Clear();
                }
            }
        }

        // Add remaining text as final sentence
        var remaining = current.ToString().Trim();
        if (!string.IsNullOrEmpty(remaining))
        {
            sentences.Add(remaining);
        }

        return sentences;
    }

    /// <summary>
    /// Parses a single sentence into linguistic structures.
    /// </summary>
    private ParsedSentence ParseSentenceInternal(
        string text,
        int index,
        int startOffset,
        ParseOptions options)
    {
        // Tokenize
        var tokens = Tokenize(text, options);

        // Build dependency tree if requested
        List<DependencyRelation>? dependencies = null;
        DependencyNode? root = null;

        if (options.IncludeDependencies && tokens.Count > 0)
        {
            dependencies = BuildDependencies(tokens);
            root = BuildDependencyTree(tokens, dependencies);
        }

        // Extract semantic frames if requested
        List<SemanticFrame>? frames = null;
        if (options.IncludeSRL && tokens.Count > 0)
        {
            frames = ExtractSemanticFrames(tokens, dependencies);
        }

        // Extract named entities if requested
        List<NamedEntity>? entities = null;
        if (options.IncludeEntities && tokens.Count > 0)
        {
            entities = ExtractNamedEntities(text, tokens);
        }

        return new ParsedSentence
        {
            Text = text,
            Tokens = tokens,
            Dependencies = dependencies,
            DependencyRoot = root,
            SemanticFrames = frames,
            Entities = entities,
            StartOffset = startOffset,
            EndOffset = startOffset + text.Length,
            Index = index
        };
    }

    /// <summary>
    /// Tokenizes text into words with linguistic annotations.
    /// </summary>
    private List<Token> Tokenize(string text, ParseOptions options)
    {
        var tokens = new List<Token>();
        var words = TokenizeSimple(text);

        var charOffset = 0;
        for (int i = 0; i < words.Count; i++)
        {
            var word = words[i];

            // Find word position in text
            var wordStart = text.IndexOf(word, charOffset, StringComparison.Ordinal);
            if (wordStart < 0) wordStart = charOffset;

            var token = new Token
            {
                Text = word,
                Lemma = options.IncludePOS ? GetLemma(word) : null,
                POS = options.IncludePOS ? GetPOSTag(word) : null,
                Tag = options.IncludePOS ? GetFineTag(word) : null,
                Index = i,
                StartChar = wordStart,
                EndChar = wordStart + word.Length,
                IsStopWord = IsStopWord(word),
                IsPunct = IsPunctuation(word)
            };

            tokens.Add(token);
            charOffset = wordStart + word.Length;
        }

        return tokens;
    }

    /// <summary>
    /// Simple word tokenization.
    /// </summary>
    private List<string> TokenizeSimple(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (char.IsPunctuation(c) && c != '-' && c != '\'')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                tokens.Add(c.ToString());
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    /// <summary>
    /// Gets the lemma (base form) of a word.
    /// </summary>
    private string GetLemma(string word)
    {
        // Simple lemmatization rules
        var lower = word.ToLowerInvariant();

        // Common verb endings
        if (lower.EndsWith("ing") && lower.Length > 4)
            return lower[..^3];
        if (lower.EndsWith("ed") && lower.Length > 3)
            return lower[..^2];
        if (lower.EndsWith("s") && !lower.EndsWith("ss") && lower.Length > 2)
            return lower[..^1];

        return lower;
    }

    /// <summary>
    /// Gets the coarse POS tag for a word.
    /// </summary>
    private string GetPOSTag(string word)
    {
        if (IsPunctuation(word)) return "PUNCT";
        if (IsNumber(word)) return "NUM";
        if (IsDeterminer(word)) return "DET";
        if (IsPreposition(word)) return "ADP";
        if (IsConjunction(word)) return "CCONJ";
        if (IsVerb(word)) return "VERB";
        if (IsAdjective(word)) return "ADJ";
        if (IsAdverb(word)) return "ADV";
        if (IsPronoun(word)) return "PRON";
        if (char.IsUpper(word[0])) return "PROPN";
        return "NOUN";
    }

    /// <summary>
    /// Gets the fine-grained POS tag for a word.
    /// </summary>
    private string GetFineTag(string word)
    {
        var pos = GetPOSTag(word);
        var lower = word.ToLowerInvariant();

        return pos switch
        {
            "VERB" when lower.EndsWith("ing") => "VBG",
            "VERB" when lower.EndsWith("ed") => "VBD",
            "VERB" when lower.EndsWith("s") => "VBZ",
            "VERB" => "VB",
            "NOUN" => "NN",
            "PROPN" => "NNP",
            "ADJ" => "JJ",
            "ADV" => "RB",
            "DET" => "DT",
            "ADP" => "IN",
            "PUNCT" => word,
            _ => pos
        };
    }

    /// <summary>
    /// Builds dependency relations between tokens.
    /// </summary>
    private List<DependencyRelation> BuildDependencies(List<Token> tokens)
    {
        var relations = new List<DependencyRelation>();

        // Find the main verb (root)
        var rootIdx = tokens.FindIndex(t => t.POS == "VERB");
        if (rootIdx < 0)
        {
            // No verb, use first noun as root
            rootIdx = tokens.FindIndex(t => t.POS == "NOUN" || t.POS == "PROPN");
        }
        if (rootIdx < 0 && tokens.Count > 0)
        {
            rootIdx = 0;
        }

        if (rootIdx < 0) return relations;

        var root = tokens[rootIdx];

        // Add ROOT relation
        relations.Add(new DependencyRelation
        {
            Head = root,
            Dependent = root,
            Relation = DependencyRelations.ROOT
        });

        // Find subject (noun before verb)
        for (int i = 0; i < rootIdx; i++)
        {
            var token = tokens[i];
            if (token.POS == "NOUN" || token.POS == "PROPN")
            {
                relations.Add(new DependencyRelation
                {
                    Head = root,
                    Dependent = token,
                    Relation = DependencyRelations.NSUBJ
                });
                break;
            }
        }

        // Find direct object (noun after verb)
        for (int i = rootIdx + 1; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.POS == "NOUN" || token.POS == "PROPN")
            {
                relations.Add(new DependencyRelation
                {
                    Head = root,
                    Dependent = token,
                    Relation = DependencyRelations.DOBJ
                });
                break;
            }
        }

        // Add determiners
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.POS == "DET" && i + 1 < tokens.Count)
            {
                var next = tokens[i + 1];
                if (next.POS == "NOUN" || next.POS == "PROPN")
                {
                    relations.Add(new DependencyRelation
                    {
                        Head = next,
                        Dependent = token,
                        Relation = DependencyRelations.DET
                    });
                }
            }
        }

        // Add adjectives as modifiers
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.POS == "ADJ" && i + 1 < tokens.Count)
            {
                var next = tokens[i + 1];
                if (next.POS == "NOUN" || next.POS == "PROPN")
                {
                    relations.Add(new DependencyRelation
                    {
                        Head = next,
                        Dependent = token,
                        Relation = DependencyRelations.AMOD
                    });
                }
            }
        }

        return relations;
    }

    /// <summary>
    /// Builds difficulty tree from tokens and relations.
    /// </summary>
    private DependencyNode? BuildDependencyTree(List<Token> tokens, List<DependencyRelation> relations)
    {
        var rootRelation = relations.FirstOrDefault(r => r.Relation == DependencyRelations.ROOT);
        if (rootRelation == null) return null;

        return BuildNodeRecursive(rootRelation.Dependent, relations);
    }

    private DependencyNode BuildNodeRecursive(Token token, List<DependencyRelation> relations)
    {
        var children = relations
            .Where(r => r.Head == token && r.Dependent != token)
            .Select(r => BuildNodeRecursive(r.Dependent, relations))
            .ToList();

        var relation = relations.FirstOrDefault(r => r.Dependent == token)?.Relation;

        return new DependencyNode
        {
            Token = token,
            Relation = relation,
            Children = children
        };
    }

    /// <summary>
    /// Extracts semantic frames using rule-based heuristics.
    /// </summary>
    private List<SemanticFrame>? ExtractSemanticFrames(
        List<Token> tokens,
        List<DependencyRelation>? dependencies)
    {
        var frames = new List<SemanticFrame>();
        var verbs = tokens.Where(t => t.POS == "VERB" && !t.IsPunct).ToList();

        foreach (var verb in verbs)
        {
            var arguments = new List<SemanticArgument>();

            if (dependencies != null)
            {
                // Find ARG0 (subject)
                var subjectRel = dependencies.FirstOrDefault(d =>
                    d.Head == verb &&
                    (d.Relation == DependencyRelations.NSUBJ ||
                     d.Relation == DependencyRelations.NSUBJPASS));

                if (subjectRel != null)
                {
                    var subjectTokens = GetSpanTokens(subjectRel.Dependent, tokens, dependencies);
                    arguments.Add(new SemanticArgument
                    {
                        Role = SemanticRole.ARG0,
                        Tokens = subjectTokens,
                        Head = subjectRel.Dependent
                    });
                }

                // Find ARG1 (direct object)
                var objectRel = dependencies.FirstOrDefault(d =>
                    d.Head == verb && d.Relation == DependencyRelations.DOBJ);

                if (objectRel != null)
                {
                    var objectTokens = GetSpanTokens(objectRel.Dependent, tokens, dependencies);
                    arguments.Add(new SemanticArgument
                    {
                        Role = SemanticRole.ARG1,
                        Tokens = objectTokens,
                        Head = objectRel.Dependent
                    });
                }

                // Find negation
                var negRel = dependencies.FirstOrDefault(d =>
                    d.Head == verb && d.Relation == DependencyRelations.NEG);

                if (negRel != null)
                {
                    arguments.Add(new SemanticArgument
                    {
                        Role = SemanticRole.ARGM_NEG,
                        Tokens = new[] { negRel.Dependent },
                        Head = negRel.Dependent
                    });
                }
            }

            if (arguments.Count > 0)
            {
                frames.Add(new SemanticFrame
                {
                    Predicate = verb,
                    Arguments = arguments
                });
            }
        }

        return frames.Count > 0 ? frames : null;
    }

    /// <summary>
    /// Gets tokens that belong to a span headed by the given token.
    /// </summary>
    private List<Token> GetSpanTokens(
        Token head,
        List<Token> allTokens,
        List<DependencyRelation> relations)
    {
        var spanTokens = new List<Token> { head };

        // Add determiners and modifiers
        var children = relations.Where(r => r.Head == head && r.Dependent != head);
        foreach (var child in children)
        {
            if (child.Relation == DependencyRelations.DET ||
                child.Relation == DependencyRelations.AMOD ||
                child.Relation == DependencyRelations.COMPOUND)
            {
                spanTokens.Add(child.Dependent);
            }
        }

        return spanTokens.OrderBy(t => t.Index).ToList();
    }

    /// <summary>
    /// Extracts named entities using simple pattern matching.
    /// </summary>
    private List<NamedEntity>? ExtractNamedEntities(string text, List<Token> tokens)
    {
        var entities = new List<NamedEntity>();

        // Find sequences of proper nouns
        var i = 0;
        while (i < tokens.Count)
        {
            if (tokens[i].POS == "PROPN")
            {
                var start = i;
                while (i < tokens.Count && tokens[i].POS == "PROPN")
                {
                    i++;
                }

                var entityTokens = tokens.Skip(start).Take(i - start).ToList();
                var entityText = string.Join(" ", entityTokens.Select(t => t.Text));

                entities.Add(new NamedEntity
                {
                    Text = entityText,
                    Label = GuessEntityLabel(entityText),
                    StartOffset = entityTokens.First().StartChar,
                    EndOffset = entityTokens.Last().EndChar
                });
            }
            else
            {
                i++;
            }
        }

        return entities.Count > 0 ? entities : null;
    }

    /// <summary>
    /// Guesses entity label based on simple heuristics.
    /// </summary>
    private string GuessEntityLabel(string text)
    {
        // Very simple heuristics
        if (text.Contains("Corp") || text.Contains("Inc") || text.Contains("Ltd"))
            return "ORG";
        if (text.All(c => char.IsUpper(c) || c == ' ') && text.Length <= 10)
            return "ORG"; // Might be an acronym
        return "ENTITY";
    }

    #region Helper Methods

    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "must", "shall", "can", "need", "dare",
            "ought", "used", "to", "of", "in", "for", "on", "with", "at", "by",
            "from", "as", "into", "through", "during", "before", "after", "above",
            "below", "between", "under", "again", "further", "then", "once",
            "and", "but", "or", "nor", "so", "yet", "both", "either", "neither",
            "not", "only", "own", "same", "than", "too", "very", "just"
        };
        return stopWords.Contains(word);
    }

    private static bool IsPunctuation(string word)
    {
        return word.Length == 1 && char.IsPunctuation(word[0]);
    }

    private static bool IsNumber(string word)
    {
        return double.TryParse(word, out _);
    }

    private static bool IsDeterminer(string word)
    {
        var determiners = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "this", "that", "these", "those", "my", "your",
            "his", "her", "its", "our", "their", "some", "any", "no", "every",
            "each", "all", "both", "half", "either", "neither", "much", "many",
            "more", "most", "few", "fewer", "little", "less", "other", "another"
        };
        return determiners.Contains(word);
    }

    private static bool IsPreposition(string word)
    {
        var prepositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "in", "on", "at", "to", "for", "with", "by", "from", "of", "about",
            "into", "through", "during", "before", "after", "above", "below",
            "between", "under", "over", "out", "up", "down", "off", "across",
            "along", "around", "behind", "beside", "beyond", "near", "toward",
            "upon", "within", "without", "against", "among", "beneath", "despite"
        };
        return prepositions.Contains(word);
    }

    private static bool IsConjunction(string word)
    {
        var conjunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "or", "but", "nor", "so", "yet", "for", "both", "either",
            "neither", "not", "only", "whether", "while", "although", "because",
            "since", "unless", "until", "when", "where", "whereas", "if", "then"
        };
        return conjunctions.Contains(word);
    }

    private static bool IsVerb(string word)
    {
        var lower = word.ToLowerInvariant();

        // Common verbs
        var commonVerbs = new HashSet<string>
        {
            "is", "are", "was", "were", "be", "been", "being", "have", "has",
            "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "must", "shall", "can", "accept", "accepts",
            "return", "returns", "require", "requires", "use", "uses",
            "provide", "provides", "send", "sends", "receive", "receives"
        };

        if (commonVerbs.Contains(lower)) return true;

        // Verb endings
        if (lower.EndsWith("ing") || lower.EndsWith("ed")) return true;
        if (lower.EndsWith("ify") || lower.EndsWith("ize") || lower.EndsWith("ate")) return true;

        return false;
    }

    private static bool IsAdjective(string word)
    {
        var lower = word.ToLowerInvariant();
        return lower.EndsWith("ful") || lower.EndsWith("less") ||
               lower.EndsWith("ous") || lower.EndsWith("ive") ||
               lower.EndsWith("able") || lower.EndsWith("ible") ||
               lower.EndsWith("al") || lower.EndsWith("ic");
    }

    private static bool IsAdverb(string word)
    {
        return word.ToLowerInvariant().EndsWith("ly");
    }

    private static bool IsPronoun(string word)
    {
        var pronouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "i", "me", "my", "mine", "myself", "you", "your", "yours", "yourself",
            "he", "him", "his", "himself", "she", "her", "hers", "herself",
            "it", "its", "itself", "we", "us", "our", "ours", "ourselves",
            "they", "them", "their", "theirs", "themselves", "who", "whom",
            "whose", "which", "what", "that", "this", "these", "those"
        };
        return pronouns.Contains(word);
    }

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash)[..16];
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
