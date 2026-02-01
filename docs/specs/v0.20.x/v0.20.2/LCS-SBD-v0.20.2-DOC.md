# Scope Breakdown Document: v0.20.2-DOC (Help System)

## Document Control

| Field | Value |
|-------|-------|
| Document ID | LCS-SBD-v0.20.2-DOC |
| Version | 1.0 |
| Release Version | v0.20.2 |
| Component | Help System (DOC) |
| Status | Draft |
| Created | 2025-02-01 |
| Last Updated | 2025-02-01 |
| Owner | Product Team |
| Stakeholders | UX/Design, Backend, Frontend, QA, DevOps |

---

## Executive Summary

### Vision

v0.20.2-DOC introduces a **comprehensive, intelligent Help System** that transforms user self-service from passive documentation into an active, guided learning experience. This release implements:

- **Searchable Help Center** with full-text indexing and AI-powered relevance ranking
- **Interactive Tutorials** with step-by-step guidance and hands-on exercises
- **Guided Tours** with contextual overlays and feature discovery
- **Integrated Knowledge Base** with intelligent article recommendations
- **Video Help Integration** with transcripts and searchable content
- **Help Analytics & Feedback** to continuously improve help content

### Business Objectives

1. **Reduce Support Load**: Enable users to self-serve 40% of common questions
2. **Accelerate Onboarding**: New users complete core workflows in 50% less time
3. **Improve NPS**: Increase customer satisfaction through accessible help (target +15 points)
4. **Content Discoverability**: Help users find relevant features they don't know about
5. **Data-Driven Improvement**: Use analytics to identify help gaps and content optimization opportunities

### Total Effort

- **Total Duration**: 62 hours
- **Team Size**: 3-4 engineers (1 backend, 1 frontend, 1 full-stack, 1 QA)
- **Sprint Allocation**: 2 sprints (2 weeks)

---

## Detailed Sub-Parts

### v0.20.2a: Searchable Help Center (12 hours)

#### Overview
Implements a full-text searchable help system with advanced filtering, faceted search, and relevance-based ranking. Users can quickly find answers using keyword search, category filters, and tag-based discovery.

#### Key Features

1. **Full-Text Search Engine**
   - Elasticsearch-powered indexing for sub-100ms queries
   - Support for partial matches, fuzzy matching, and phrase search
   - Automatic stemming and synonym expansion
   - Search history and saved searches per user
   - Search suggestions/autocomplete

2. **Advanced Filtering**
   - Filter by category (Getting Started, Features, Troubleshooting, Integration, etc.)
   - Filter by product area (Workspace, Library, Analytics, Collaboration, etc.)
   - Filter by skill level (Beginner, Intermediate, Advanced)
   - Filter by article type (How-To, FAQ, Troubleshooting, Video)
   - Filter by last updated date

3. **Search Results Ranking**
   - Title matches rank highest
   - Content matches with proximity scoring
   - Popularity/view count weighting
   - User engagement metrics (helpful votes, completion rate)
   - Recency boost for recently updated articles
   - Personalization based on user's workspace context

4. **Search Analytics**
   - Track search queries and result clicks
   - Identify zero-result searches (content gaps)
   - Monitor search performance metrics
   - A/B test search result rankings

#### Acceptance Criteria

- [ ] Users can search by keyword and receive results in < 100ms
- [ ] Search results display with title, excerpt (150 chars), category, and helpful metadata
- [ ] Users can apply multiple filters simultaneously (AND/OR logic properly handled)
- [ ] Autocomplete suggests relevant search terms as user types
- [ ] Search supports quoted phrases ("exact match"), Boolean operators (AND, OR, NOT)
- [ ] Results are ranked by relevance with explanation (why this result is relevant)
- [ ] Users can save searches and receive notifications for new matching articles
- [ ] Search analytics track all queries and clicks
- [ ] Zero-result searches are logged for content gap analysis
- [ ] Search handles special characters, Unicode, and multiple languages gracefully
- [ ] Mobile search is responsive and touch-optimized
- [ ] Search integrates with help center UI and guided tours

#### Technical Requirements

- **Search Backend**: Elasticsearch 8.x or PostgreSQL full-text search with trigram indexes
- **Query Performance**: < 100ms for 95th percentile (including network latency)
- **Indexing**: Real-time incremental indexing with 30-second maximum delay
- **Capacity**: Support 10,000+ help articles with concurrent search from 1,000+ users
- **Data Storage**: Store search queries, user sessions, and click events in PostgreSQL
- **API Endpoints**:
  - `GET /api/help/search` - Execute search query with filters
  - `GET /api/help/search/suggestions` - Get autocomplete suggestions
  - `POST /api/help/search/analytics` - Log search event
  - `GET /api/help/search/trending` - Get trending searches
  - `GET /api/help/articles` - Browse articles by category

---

### v0.20.2b: Interactive Tutorials (12 hours)

#### Overview
Implements step-by-step interactive tutorials that guide users through workflows with real-time validation, code examples, and hands-on exercises. Tutorials adapt based on user progress and learning style.

#### Key Features

1. **Tutorial Authoring**
   - WYSIWYG editor for creating tutorials (markdown + rich text)
   - Support for multiple content types: text, images, code blocks, embedded videos
   - Branching logic for conditional steps based on user choices
   - Tagging and categorization (Skill Level, Product Area, Use Case)
   - Version control and publication workflow
   - A/B testing support (multiple tutorial variants)

2. **Tutorial Player**
   - Step-by-step guided experience with progress tracking
   - Sidebar navigation showing tutorial outline
   - Previous/Next buttons with keyboard shortcuts
   - Estimated time to complete calculation
   - Collapsible hints and detailed explanations
   - Copy-to-clipboard for code samples
   - Dark mode support

3. **Interactive Elements**
   - Input validation with helpful error messages
   - Drag-and-drop exercises
   - Code editor with syntax highlighting (embedded Monaco Editor)
   - Interactive components (toggles, dropdowns, buttons)
   - Form submission with validation
   - Quiz questions with immediate feedback
   - Real-time API calls with result display

4. **Progress Tracking**
   - Save tutorial progress with auto-save every 30 seconds
   - Resume from last completed step
   - Completion badges and achievements
   - Learning path progression (sequential tutorial requirements)
   - Time spent tracking per tutorial

5. **Analytics & Optimization**
   - Track which steps are skipped/completed
   - Identify drop-off points
   - Measure time spent per step
   - Monitor code example usage
   - A/B test tutorial content effectiveness
   - Cohort analysis for different user types

#### Acceptance Criteria

- [ ] Users can view tutorial outline with all steps listed
- [ ] Tutorial loads in < 500ms and scrolls smoothly at 60 FPS
- [ ] Users can complete tutorial steps with step validation feedback
- [ ] Progress is saved automatically and persists across sessions
- [ ] Users can resume tutorials from last completed step
- [ ] Users can navigate forward/backward through steps
- [ ] Code examples are copy-able with syntax highlighting
- [ ] Interactive elements (forms, dropdowns, buttons) function correctly
- [ ] Quiz questions provide immediate pass/fail feedback
- [ ] Tutorials display completion time estimate and actual time spent
- [ ] Users receive completion badge upon tutorial completion
- [ ] Tutorial analytics track completion rate, drop-off points, and time per step
- [ ] Mobile experience is responsive with touch-optimized controls
- [ ] Tutorial content renders consistently across browsers
- [ ] Users can access tutorials from help center and contextual help triggers

#### Technical Requirements

- **Storage**: PostgreSQL tutorials table with markdown/HTML content
- **State Management**: Track user progress in user_progress table with auto-save
- **Frontend Library**: React component library for tutorial player
- **Code Editor**: Monaco Editor for interactive code examples (sandboxed execution optional)
- **Validation Engine**: Custom validator for step completion checks
- **Analytics**: MediatR events for tutorial interactions
- **API Endpoints**:
  - `GET /api/tutorials` - List available tutorials
  - `GET /api/tutorials/{id}` - Get tutorial details
  - `POST /api/tutorials/{id}/start` - Initialize tutorial session
  - `POST /api/tutorials/{id}/progress` - Save step progress
  - `POST /api/tutorials/{id}/complete` - Mark tutorial as complete
  - `GET /api/tutorials/{id}/analytics` - Get tutorial performance data

---

### v0.20.2c: Guided Tours (10 hours)

#### Overview
Implements contextual, interactive guided tours that overlay the application UI to introduce features, workflows, and best practices. Tours can be triggered automatically, manually, or contextually based on user actions.

#### Key Features

1. **Tour Definition & Management**
   - Visual tour builder with point-and-click element selection
   - Tour sequencing with branching logic
   - Multiple trigger types: automatic (first login), manual (help menu), contextual (feature unlock)
   - Tour targeting by user segment, role, or feature flag
   - Draft/publish workflow with preview mode
   - Tour scheduling and expiration dates
   - A/B testing for tour effectiveness

2. **Tour Execution**
   - Interactive overlay highlighting UI elements
   - Animated spotlight effect drawing attention to features
   - Step-by-step guidance with next/previous navigation
   - Contextual tooltips with position auto-adjustment
   - Skip tour and do-not-show-again options
   - Tour progress saving and resumption
   - Keyboard shortcuts (ESC to dismiss, arrow keys to navigate)

3. **Tour Triggers**
   - **First-Time User Tours**: Automatically shown on first login or feature unlock
   - **Feature Introduction**: Triggered when new features are released
   - **Contextual Tours**: Triggered by specific user actions (e.g., clicking a feature)
   - **On-Demand Tours**: Accessible from help menu or sidebar
   - **Smart Suppression**: Don't show tours to users who already know the feature

4. **Element Targeting**
   - CSS selector-based element targeting with fallback DOM path
   - Responsive element detection (handles dynamic content)
   - Z-index management to ensure tour overlay is visible
   - Scroll-into-view when targeting off-screen elements
   - Modal dialog detection and handling

5. **Analytics & Optimization**
   - Track tour views, completions, and skips
   - Identify most effective tours (by conversion to action)
   - Measure time spent per tour step
   - A/B test tour copy and visual design
   - Monitor technical issues (element not found, etc.)

#### Acceptance Criteria

- [ ] Tours load and display overlay without layout shifts (CLS < 0.1)
- [ ] Users can see highlighted UI elements with descriptive tooltips
- [ ] Users can navigate through tour steps with Next/Previous buttons
- [ ] Users can skip tours and suppress future reminders
- [ ] Tour progress is saved and can be resumed
- [ ] Multiple tours don't conflict or overlap
- [ ] Tours work with responsive design at all breakpoints
- [ ] Tours handle dynamic content and rerenders correctly
- [ ] Tour targeting works by user segment, role, and feature flag
- [ ] Tour analytics track views, completions, skips, and element interactions
- [ ] Keyboard navigation works (Arrow keys, Enter, ESC)
- [ ] Tours work on mobile with touch-optimized interactions
- [ ] Contextual tours are triggered by correct user actions
- [ ] Element highlighting is visible in light and dark modes

#### Technical Requirements

- **Tour Storage**: PostgreSQL guided_tours table with step definitions
- **Element Targeting**: CSS selector + DOM path + coordinates (for resilience)
- **Overlay Rendering**: React overlay component with dynamic positioning
- **Tour Engine**: State machine for managing tour progression
- **Trigger System**: Event-based trigger evaluation (user action, time, feature flag)
- **Analytics**: Track tour events (view, step_change, skip, complete)
- **API Endpoints**:
  - `GET /api/guided-tours` - List available tours
  - `GET /api/guided-tours/{id}` - Get tour details
  - `POST /api/guided-tours/{id}/start` - Start tour session
  - `POST /api/guided-tours/{id}/progress` - Update tour progress
  - `POST /api/guided-tours/{id}/complete` - Mark tour as complete
  - `POST /api/guided-tours/{id}/skip` - Skip tour
  - `GET /api/guided-tours/{id}/analytics` - Get tour performance

---

### v0.20.2d: Knowledge Base (10 hours)

#### Overview
Implements a comprehensive knowledge base with intelligent article organization, related article recommendations, breadcrumb navigation, and integrated search. Articles support rich formatting, version history, and contributor attribution.

#### Key Features

1. **Article Management**
   - Rich text editor with markdown support
   - Hierarchical article organization (categories, subcategories, sections)
   - Article versioning and edit history with contributor attribution
   - Automated table of contents generation
   - Internal linking between articles
   - Related articles recommendation engine
   - Article metadata (author, updated date, word count, read time)
   - Tagging and categorization
   - Status workflow (Draft, Review, Published, Archived)

2. **Article Content**
   - Titles and descriptions (SEO-optimized)
   - Body content with rich formatting
   - Step-by-step instructions with numbered steps
   - Code examples with syntax highlighting
   - Embedded images, videos, and interactive elements
   - Callout boxes (Info, Warning, Success, Error)
   - FAQ sections with expandable Q&A
   - Related links and see-also sections
   - Author bio and contributor information

3. **Navigation & Discovery**
   - Category browsing with hierarchical tree
   - Breadcrumb navigation showing article path
   - Sidebar with article outline (jump to section links)
   - Related articles widget showing similar content
   - Table of contents with deep linking to sections
   - Search integration within KB
   - Popular articles widget
   - Most recently updated articles list

4. **User Engagement**
   - Helpful/Not Helpful voting with optional feedback
   - Article ratings (1-5 stars)
   - Comments and community discussions (moderated)
   - Share article functionality (email, social media, link copy)
   - Print-friendly article format
   - Dark mode support with readable typography

5. **Analytics & Optimization**
   - Track article views, bounce rate, time on page
   - Monitor helpful vote ratio
   - Identify low-quality articles (low engagement)
   - Search queries leading to articles
   - Articles with high exit rate (improve content)
   - Reader sentiment from ratings and comments
   - Content gap identification from search queries

#### Acceptance Criteria

- [ ] Users can browse article categories with hierarchical navigation
- [ ] Article pages load in < 300ms with all content visible
- [ ] Articles display with proper formatting (bold, italic, lists, code blocks)
- [ ] Users can search within knowledge base with fast results
- [ ] Related articles are displayed on article pages
- [ ] Breadcrumb navigation shows article hierarchy
- [ ] Users can provide helpful/not helpful feedback
- [ ] Table of contents is auto-generated and functional
- [ ] Internal links between articles work correctly
- [ ] Mobile layout is responsive and readable
- [ ] Dark mode provides readable contrast
- [ ] Article metadata (author, updated date) is displayed
- [ ] Print layout removes navigation and optimizes readability
- [ ] Analytics track article views and engagement metrics
- [ ] Articles support embedding images, videos, and code examples

#### Technical Requirements

- **Storage**: PostgreSQL help_articles table with hierarchical structure
- **Search**: Full-text search integration with help center search
- **Content Rendering**: Markdown to HTML converter with sanitization
- **Rich Editor**: TipTap or similar for WYSIWYG editing
- **Image Storage**: S3 or similar for image hosting with CDN
- **Version Control**: Track article versions with diff visualization
- **Analytics**: Track views, engagement, and user feedback
- **API Endpoints**:
  - `GET /api/help/articles` - List articles with filters
  - `GET /api/help/articles/{id}` - Get article details
  - `POST /api/help/articles/{id}/feedback` - Submit helpful/not helpful
  - `GET /api/help/articles/{id}/related` - Get related articles
  - `GET /api/help/categories` - Get category hierarchy
  - `GET /api/help/search` - Search articles (shared with help center)

---

### v0.20.2e: Video Help Integration (8 hours)

#### Overview
Integrates video content into the help system with searchable transcripts, chapters, and timestamps. Videos are hosted on YouTube or Vimeo with automatic transcription and indexing for search.

#### Key Features

1. **Video Management**
   - Video upload with automatic processing
   - Support for external video platforms (YouTube, Vimeo)
   - Video metadata (title, description, duration, thumbnail)
   - Video categorization and tagging
   - Transcript generation (auto-generated or manual upload)
   - Chapter definition with timestamps
   - Video analytics (views, watch time, engagement)

2. **Video Player**
   - Responsive embed with custom player controls
   - Playback speed adjustment (0.5x to 2x)
   - Subtitle/transcript display with sync to video
   - Chapter navigation and timestamp jumping
   - Full-screen support
   - Quality selection (if using platform API)
   - Share video with timestamp link
   - Download transcript as PDF/TXT
   - Captions/Subtitles with multiple language support

3. **Transcript Integration**
   - Auto-generate transcripts using speech-to-text API (AWS Transcribe, Google Cloud Speech)
   - Searchable transcript index (word search with timestamp)
   - Searchable from help center search
   - Editable transcript with timestamp correction
   - Multiple language transcripts

4. **Related Content**
   - Link videos to help articles and tutorials
   - Related videos recommendations
   - Video playlists by topic or skill level
   - Cross-linking between video chapters and KB articles

5. **Accessibility**
   - Closed captions support
   - Transcript display for accessibility
   - Keyboard navigation support
   - Audio description tracks
   - WCAG 2.1 AA compliance

#### Acceptance Criteria

- [ ] Videos embed and play without errors
- [ ] Video player controls work (play, pause, volume, fullscreen)
- [ ] Playback speed adjustment works (0.5x to 2x)
- [ ] Transcripts are generated automatically or uploaded manually
- [ ] Transcripts are searchable and linked to video timestamps
- [ ] Users can jump to chapters from transcript or chapter list
- [ ] Video is searchable from help center search via transcript content
- [ ] Multiple videos are linkable in playlists or related content sections
- [ ] Videos work responsively on mobile devices
- [ ] Closed captions display properly with font size adjustable
- [ ] Share links preserve video timestamp
- [ ] Analytics track views, watch time, and engagement
- [ ] Related videos and articles display on video page
- [ ] Transcript can be downloaded as PDF

#### Technical Requirements

- **Video Hosting**: YouTube, Vimeo, or self-hosted (cloudinary, mux)
- **Transcription**: AWS Transcribe, Google Cloud Speech-to-Text, or manual
- **Transcript Storage**: PostgreSQL table with transcript text and timestamps
- **Video Metadata**: PostgreSQL table with video details and chapters
- **Search Integration**: Index transcripts in search engine
- **Player**: Custom or platform-provided player (YouTube IFrame, Vimeo IFrame)
- **Analytics**: Track video views, watch duration, and engagement
- **API Endpoints**:
  - `GET /api/videos` - List videos
  - `GET /api/videos/{id}` - Get video details
  - `GET /api/videos/{id}/transcript` - Get video transcript
  - `GET /api/videos/{id}/chapters` - Get video chapters
  - `POST /api/videos/{id}/analytics` - Log video engagement

---

### v0.20.2f: Help Analytics & Feedback (10 hours)

#### Overview
Implements comprehensive analytics and feedback system to measure help system effectiveness, identify content gaps, and drive continuous improvement through data-driven decisions.

#### Key Features

1. **Help Usage Analytics**
   - Track help center visits, searches, and article views
   - Monitor tutorial starts, completions, and drop-off points
   - Track guided tour views, completions, and skips
   - Time spent on help articles, tutorials, and tours
   - User engagement metrics (scroll depth, section views)
   - Search query analysis and zero-result searches
   - Traffic sources (direct, search, within-app links)
   - User cohort analysis (by role, product area, skill level)

2. **Content Effectiveness Metrics**
   - Article helpfulness ratio (helpful vs not helpful votes)
   - Article bounce rate (viewed but immediately left)
   - Tutorial completion rate and average time to complete
   - Tour completion rate and step-by-step conversion
   - Search click-through rate (which results are clicked)
   - Content gaps (search queries with zero results)
   - Time to resolution (help access to issue resolution)

3. **User Feedback**
   - Helpful/Not Helpful voting on articles
   - Article ratings (1-5 stars)
   - Comments on articles with moderation
   - Feedback forms for tutorials
   - Net Promoter Score (NPS) survey integration
   - Post-support survey to measure help effectiveness
   - In-app feedback widget for feature requests

4. **Dashboard & Reporting**
   - Help center overview dashboard (KPIs, trends)
   - Search analytics dashboard (top queries, zero-result queries)
   - Content performance dashboard (by article, category, author)
   - Tutorial analytics dashboard (completion rates, drop-offs)
   - Tour analytics dashboard (views, completions, effectiveness)
   - Video analytics dashboard (views, watch time, engagement)
   - User feedback dashboard (ratings, NPS, sentiment)
   - Custom report builder for specific analysis needs
   - Export reports as PDF/CSV

5. **Optimization Recommendations**
   - Identify low-performing content (low views, low engagement)
   - Identify content gaps from zero-result searches
   - Recommend content improvements based on feedback
   - Identify high-impact opportunities (many searches, zero results)
   - Suggest related content to link
   - A/B testing framework for content optimization

6. **Integration & Alerts**
   - Slack notifications for critical help system issues
   - Email reports on content performance (daily/weekly/monthly)
   - Integration with support system (ticket resolution correlation)
   - Integration with product analytics (feature usage correlation)
   - Alert thresholds for anomalies (sudden drop in tutorial completions)

#### Acceptance Criteria

- [ ] Help center dashboard displays key metrics (visits, searches, article views)
- [ ] Search analytics dashboard shows top queries and zero-result queries
- [ ] Content performance dashboard shows views, engagement, and feedback per article
- [ ] Tutorial analytics shows completion rate, average time, and drop-off points
- [ ] Tour analytics shows views, completions, and step-by-step conversion
- [ ] Video analytics shows views, watch time, and engagement
- [ ] User feedback dashboard displays ratings, NPS, and sentiment
- [ ] Analytics data is updated in near real-time (< 1 minute delay)
- [ ] Reports can be exported as PDF/CSV
- [ ] Custom report builder allows filtering and grouping by various dimensions
- [ ] Recommendations engine identifies optimization opportunities
- [ ] Alerts notify relevant teams of critical issues
- [ ] Analytics respect user privacy (no PII collection)
- [ ] Cohort analysis works for different user segments
- [ ] Data retention policy is defined and enforced

#### Technical Requirements

- **Event Tracking**: MediatR events for all user interactions
- **Data Storage**: PostgreSQL tables for events, metrics, and analytics
- **Analytics Engine**: Real-time aggregation (ClickHouse, TimescaleDB, or similar)
- **Dashboard**: React-based dashboard with visualizations (Chart.js, D3.js)
- **Reporting**: Scheduled report generation (daily/weekly/monthly)
- **Alerting**: Email/Slack notifications for critical thresholds
- **Privacy**: GDPR-compliant analytics with PII redaction
- **API Endpoints**:
  - `GET /api/analytics/dashboard` - Get dashboard data
  - `GET /api/analytics/search` - Get search analytics
  - `GET /api/analytics/content` - Get content performance
  - `GET /api/analytics/tutorials` - Get tutorial analytics
  - `GET /api/analytics/tours` - Get tour analytics
  - `GET /api/analytics/feedback` - Get user feedback summary
  - `POST /api/analytics/event` - Log analytics event
  - `GET /api/analytics/reports` - List available reports
  - `POST /api/analytics/reports` - Generate custom report

---

## C# Interfaces

### IHelpCenter

```csharp
/// <summary>
/// Primary service for managing and accessing help center functionality
/// </summary>
public interface IHelpCenter
{
    /// <summary>
    /// Search help articles, tutorials, and knowledge base content
    /// </summary>
    Task<HelpSearchResult> SearchAsync(
        HelpSearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get autocomplete suggestions for help search
    /// </summary>
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(
        string partialQuery,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get trending searches for the past 7/30 days
    /// </summary>
    Task<IEnumerable<TrendingSearch>> GetTrendingSearchesAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get help articles by category
    /// </summary>
    Task<PagedResult<HelpArticle>> GetArticlesByCategoryAsync(
        string categoryId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific help article with related content
    /// </summary>
    Task<HelpArticleDetail> GetArticleAsync(
        string articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get help categories with hierarchy
    /// </summary>
    Task<IEnumerable<HelpCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit helpful/not helpful feedback on an article
    /// </summary>
    Task<ArticleFeedback> SubmitArticleFeedbackAsync(
        string articleId,
        HelpfulnessVote vote,
        string? feedbackText = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rate an article (1-5 stars)
    /// </summary>
    Task<ArticleRating> RateArticleAsync(
        string articleId,
        int rating,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related articles based on content similarity
    /// </summary>
    Task<IEnumerable<HelpArticle>> GetRelatedArticlesAsync(
        string articleId,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log help search event for analytics
    /// </summary>
    Task LogSearchEventAsync(
        HelpSearchEvent searchEvent,
        CancellationToken cancellationToken = default);
}
```

### ITutorialSystem

```csharp
/// <summary>
/// Service for managing interactive tutorials
/// </summary>
public interface ITutorialSystem
{
    /// <summary>
    /// Get available tutorials with filters
    /// </summary>
    Task<PagedResult<Tutorial>> GetTutorialsAsync(
        TutorialFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tutorial details including all steps
    /// </summary>
    Task<TutorialDetail> GetTutorialAsync(
        string tutorialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a new tutorial session for the current user
    /// </summary>
    Task<TutorialSession> StartTutorialAsync(
        string tutorialId,
        string? variantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's current progress in a tutorial
    /// </summary>
    Task<TutorialProgress> GetProgressAsync(
        string tutorialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update progress on a specific tutorial step
    /// </summary>
    Task<TutorialProgress> UpdateStepProgressAsync(
        string tutorialId,
        int stepNumber,
        TutorialStepProgress stepProgress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark tutorial as completed
    /// </summary>
    Task<TutorialCompletion> CompleteTutorialAsync(
        string tutorialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Skip tutorial (user chooses not to continue)
    /// </summary>
    Task SkipTutorialAsync(
        string tutorialId,
        string? skipReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's tutorial completion history
    /// </summary>
    Task<IEnumerable<TutorialCompletion>> GetCompletionHistoryAsync(
        string? productArea = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recommended tutorials for user
    /// </summary>
    Task<IEnumerable<Tutorial>> GetRecommendedTutorialsAsync(
        string? productArea = null,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log tutorial event for analytics
    /// </summary>
    Task LogTutorialEventAsync(
        TutorialEvent tutorialEvent,
        CancellationToken cancellationToken = default);
}
```

### IGuidedTourService

```csharp
/// <summary>
/// Service for managing guided tours and contextual overlays
/// </summary>
public interface IGuidedTourService
{
    /// <summary>
    /// Get available tours for the current user context
    /// </summary>
    Task<IEnumerable<GuidedTour>> GetAvailableToursAsync(
        TourContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific tour details
    /// </summary>
    Task<GuidedTourDetail> GetTourAsync(
        string tourId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if a tour should be automatically shown
    /// </summary>
    Task<bool> ShouldShowTourAsync(
        string tourId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a tour session
    /// </summary>
    Task<TourSession> StartTourAsync(
        string tourId,
        TourStartReason startReason = TourStartReason.OnDemand,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user progress in a tour
    /// </summary>
    Task<TourProgress> UpdateTourProgressAsync(
        string tourId,
        int stepNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete tour
    /// </summary>
    Task<TourCompletion> CompleteTourAsync(
        string tourId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Skip/dismiss tour
    /// </summary>
    Task SkipTourAsync(
        string tourId,
        bool suppressFuture = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tours targeting specific element
    /// </summary>
    Task<IEnumerable<GuidedTour>> GetToursForElementAsync(
        string elementSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark tour as "do not show again" for user
    /// </summary>
    Task MarkTourAsSeenAsync(
        string tourId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's tour completion history
    /// </summary>
    Task<IEnumerable<TourCompletion>> GetCompletionHistoryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log tour event for analytics
    /// </summary>
    Task LogTourEventAsync(
        TourEvent tourEvent,
        CancellationToken cancellationToken = default);
}
```

### IKnowledgeBase

```csharp
/// <summary>
/// Service for managing knowledge base articles
/// </summary>
public interface IKnowledgeBase
{
    /// <summary>
    /// Get knowledge base categories with hierarchy
    /// </summary>
    Task<KnowledgeBaseCategoryHierarchy> GetCategoryHierarchyAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get articles in a category
    /// </summary>
    Task<PagedResult<KbArticle>> GetArticlesInCategoryAsync(
        string categoryId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get article with full content and metadata
    /// </summary>
    Task<KbArticleDetail> GetArticleAsync(
        string articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related articles based on tags/content
    /// </summary>
    Task<IEnumerable<KbArticle>> GetRelatedArticlesAsync(
        string articleId,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular articles
    /// </summary>
    Task<IEnumerable<KbArticle>> GetPopularArticlesAsync(
        TimeSpan period,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recently updated articles
    /// </summary>
    Task<IEnumerable<KbArticle>> GetRecentArticlesAsync(
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit feedback on knowledge base article
    /// </summary>
    Task<KbArticleFeedback> SubmitArticleFeedbackAsync(
        string articleId,
        HelpfulnessVote vote,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add comment to article
    /// </summary>
    Task<KbComment> AddCommentAsync(
        string articleId,
        string commentText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get article comments
    /// </summary>
    Task<PagedResult<KbComment>> GetCommentsAsync(
        string articleId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log article view for analytics
    /// </summary>
    Task LogArticleViewAsync(
        string articleId,
        CancellationToken cancellationToken = default);
}
```

### IVideoHelpService

```csharp
/// <summary>
/// Service for managing video help content
/// </summary>
public interface IVideoHelpService
{
    /// <summary>
    /// Get available help videos with filters
    /// </summary>
    Task<PagedResult<HelpVideo>> GetVideosAsync(
        VideoFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get video details with transcript and chapters
    /// </summary>
    Task<HelpVideoDetail> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get video transcript
    /// </summary>
    Task<VideoTranscript> GetTranscriptAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get video chapters
    /// </summary>
    Task<IEnumerable<VideoChapter>> GetChaptersAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search transcripts across all videos
    /// </summary>
    Task<IEnumerable<TranscriptSearchResult>> SearchTranscriptsAsync(
        string searchQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get videos in a playlist
    /// </summary>
    Task<PagedResult<HelpVideo>> GetPlaylistVideosAsync(
        string playlistId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related videos
    /// </summary>
    Task<IEnumerable<HelpVideo>> GetRelatedVideosAsync(
        string videoId,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log video view event
    /// </summary>
    Task LogVideoViewAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log video engagement event
    /// </summary>
    Task LogVideoEngagementAsync(
        VideoEngagementEvent engagementEvent,
        CancellationToken cancellationToken = default);
}
```

### IHelpAnalytics

```csharp
/// <summary>
/// Service for help system analytics and feedback
/// </summary>
public interface IHelpAnalytics
{
    /// <summary>
    /// Get help center overview dashboard data
    /// </summary>
    Task<HelpCenterDashboard> GetHelpCenterDashboardAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get search analytics
    /// </summary>
    Task<SearchAnalytics> GetSearchAnalyticsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get content performance analytics
    /// </summary>
    Task<PagedResult<ContentPerformance>> GetContentPerformanceAsync(
        ContentType contentType,
        int pageNumber = 1,
        int pageSize = 20,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tutorial analytics
    /// </summary>
    Task<TutorialAnalytics> GetTutorialAnalyticsAsync(
        string? tutorialId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get guided tour analytics
    /// </summary>
    Task<TourAnalytics> GetTourAnalyticsAsync(
        string? tourId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get video analytics
    /// </summary>
    Task<VideoAnalytics> GetVideoAnalyticsAsync(
        string? videoId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user feedback summary
    /// </summary>
    Task<FeedbackSummary> GetFeedbackSummaryAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log analytics event
    /// </summary>
    Task LogEventAsync(
        HelpEvent helpEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get optimization recommendations
    /// </summary>
    Task<IEnumerable<OptimizationRecommendation>> GetRecommendationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate custom report
    /// </summary>
    Task<HelpReport> GenerateReportAsync(
        ReportSpecification reportSpec,
        CancellationToken cancellationToken = default);
}
```

---

## ASCII Architecture Diagrams

### Help Content Indexing Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    HELP CONTENT INDEXING FLOW                   │
└─────────────────────────────────────────────────────────────────┘

CONTENT CREATION
    ↓
┌─────────────────────────────────────────────────────────────────┐
│ Help Admin/Author creates content in:                           │
│  • Knowledge Base Editor (KB Articles)                          │
│  • Tutorial Builder (Interactive Tutorials)                     │
│  • Tour Designer (Guided Tours)                                 │
│  • Video Manager (Video Content with Transcripts)               │
└─────────────────────────────────────────────────────────────────┘
    ↓
CONTENT STORAGE
    ↓
┌─────────────────────────────────────────────────────────────────┐
│ PostgreSQL Tables:                                              │
│  ├─ help_articles (KB content)                                  │
│  ├─ tutorials (tutorial definitions)                            │
│  ├─ tutorial_steps (step definitions)                           │
│  ├─ guided_tours (tour definitions)                             │
│  ├─ tour_stops (tour steps)                                     │
│  └─ video_content (video metadata & transcripts)               │
└─────────────────────────────────────────────────────────────────┘
    ↓
INDEXING & ENRICHMENT
    ↓
┌─────────────────────────────────────────────────────────────────┐
│ Indexing Service:                                               │
│  1. Extract text content from all sources                       │
│  2. Generate transcripts from videos (AWS Transcribe)           │
│  3. Extract metadata (title, category, tags)                    │
│  4. Generate embeddings (OpenAI API)                            │
│  5. Create searchable tokens (stemming, synonyms)               │
└─────────────────────────────────────────────────────────────────┘
    ↓
SEARCH ENGINE INDEXING
    ↓
┌──────────────────────────────────────────┐  ┌──────────────────────────────────────────┐
│       Elasticsearch / Meilisearch         │  │    PostgreSQL Full-Text Search Index     │
│                                          │  │                                          │
│  • Full-text index of all content        │  │  • GIN/GiST indexes for tsvector        │
│  • Per-article sections indexed          │  │  • Trigram indexes for fuzzy search     │
│  • Video transcript indexing             │  │  • Category/tag indexes                  │
│  • Faceted search (category, type, etc)  │  │  • Keyword indexes                       │
│  • Real-time indexing (< 30 seconds)     │  │  • Join indexes for related articles    │
│  • Relevance ranking configuration       │  │  • Partial indexes for recent articles  │
└──────────────────────────────────────────┘  └──────────────────────────────────────────┘
         ↓                                                  ↓
SEARCH API
    ↓
┌─────────────────────────────────────────────────────────────────┐
│ Help Center Search Endpoint                                     │
│  GET /api/help/search?q=keyword&category=X&type=Y             │
└─────────────────────────────────────────────────────────────────┘
    ↓
USER RESULTS
    ↓
┌─────────────────────────────────────────────────────────────────┐
│ Ranked Results Display:                                         │
│  1. Title matches (highest ranking)                            │
│  2. Content matches with proximity scoring                      │
│  3. Related articles (recommendation engine)                    │
│  4. Video transcripts matching search terms                    │
│  5. Tutorial steps matching search terms                        │
└─────────────────────────────────────────────────────────────────┘
```

### Search Flow & Relevance Ranking

```
┌─────────────────────────────────────────────────────────────────┐
│                  HELP CENTER SEARCH FLOW                        │
└─────────────────────────────────────────────────────────────────┘

USER SEARCH QUERY
    │
    ├─ Input: "How to set up collaboration"
    │
    ↓
QUERY PROCESSING
    │
    ├─ Tokenize: [how, to, set, up, collaboration]
    ├─ Normalize: lowercase, remove stop words
    ├─ Expand synonyms: [setup, team-work, shared, shared-workspace]
    ├─ Apply fuzzy matching for typos
    ├─ Extract filters: category, type, date range
    │
    ↓
ELASTICSEARCH / DATABASE QUERY
    │
    ├─ Query full-text index
    ├─ Apply BM25 ranking (term frequency, document length)
    ├─ Apply custom scoring (popularity, recency, user engagement)
    │
    ↓
RELEVANCE RANKING CALCULATION
    │
    Score = (BM25_Score * 0.4) +
            (Popularity_Score * 0.2) +
            (Recency_Score * 0.1) +
            (User_Engagement_Score * 0.3)
    │
    ├─ BM25_Score: Term frequency and IDF
    ├─ Popularity_Score: View count, share count
    ├─ Recency_Score: Days since updated (decay function)
    ├─ User_Engagement_Score: Click-through, helpful votes, time spent
    │
    ↓
RESULT FILTERING & PERSONALIZATION
    │
    ├─ Filter by category: "Features"
    ├─ Filter by type: "How-To", "Tutorial"
    ├─ Filter by skill level: "Beginner", "Intermediate"
    ├─ Personalize based on user's workspace context
    ├─ Suppress articles user has already viewed recently
    │
    ↓
RANKING BY RESULT TYPE
    │
    ├─ KB Articles (exact matches boost)
    ├─ Tutorial Steps (partial matches)
    ├─ Video Transcripts (lowest weight for ambiguity)
    ├─ Related Articles (shown at bottom)
    │
    ↓
SEARCH RESULTS DISPLAY
    │
    ├─ 1. Title (match highlighted)
    ├─ 2. Excerpt (150 chars with context)
    ├─ 3. Category badge
    ├─ 4. Helpful votes (X% of users found helpful)
    ├─ 5. Read time estimate
    ├─ 6. Last updated date
    │
    ↓
ANALYTICS LOGGING
    │
    ├─ Log search query (text, filters used, results count)
    ├─ Log which results user clicked
    ├─ Log if user refined search (zero-result cascade)
    ├─ Log time to click (how long before user clicked result)
    │
    ↓
FEEDBACK LOOP
    │
    ├─ Track zero-result searches (content gaps)
    ├─ Track low-click-through results (ranking issues)
    ├─ Track user helpful votes on results
    ├─ Periodically retrain ranking model with feedback
```

### Tutorial State Machine

```
┌─────────────────────────────────────────────────────────────────┐
│               TUTORIAL PROGRESSION STATE MACHINE                 │
└─────────────────────────────────────────────────────────────────┘

                    ┌─────────────────┐
                    │   NOT_STARTED   │◄─── User doesn't know tutorial exists
                    └────────┬────────┘
                             │
        (User clicks "Start Tutorial" or auto-triggered)
                             │
                             ↓
                    ┌─────────────────────┐
                    │  TUTORIAL_STARTED   │
                    │  (session created)  │
                    └────────┬────────────┘
                             │
        (User navigates to first step)
                             │
                             ↓
          ┌──────────────────────────────────────┐
          │      STEP_IN_PROGRESS                │
          │  (User is on current step)           │
          │  • Step 1: Introduction              │
          │  • Step 2: Setup instructions        │
          │  • Step 3: Hands-on exercise         │
          │  • Step 4: Validation & feedback     │
          │  ...                                 │
          └──┬──────────────────────────┬────┬───┘
             │                          │    │
    (Step validation fails)  (Next step)  (Skip)
             │                          │    │
    ┌────────▼──────┐         ┌────────▼──┐ │
    │STEP_FAILED    │         │STEP_      │ │
    │  + Show error │         │COMPLETED  │ │
    │  + Allow retry│         │  + Save   │ │
    └────────┬──────┘         │    progress
             │                └────────┬───┘
             │                        │
    (User retries)            (More steps?)
             │                        │
             └────────┬──────────────┘
                      │
             (All steps complete?)
                      │
         ┌────────────┴──────────────┐
         │ NO (more steps)           │ YES (all steps done)
         │                           │
         ↓                           ↓
    ┌──────────────┐        ┌─────────────────┐
    │STEP_IN_       │        │ TUTORIAL_       │
    │PROGRESS (next)│        │ COMPLETED       │
    └────────┬──────┘        │ • Award badge   │
             │                │ • Record in hist│
             │                │ • Notify user   │
             │                │ • Track metrics │
             │                └────────┬────────┘
             │                         │
             └─────────────────────────┘
                     │
                     ↓
             ┌──────────────────────┐
             │ USER_VIEWED_         │
             │ COMPLETION_SUMMARY   │
             │ (Show achievements)  │
             └──────────────────────┘

ALTERNATIVE PATHS:

From STEP_IN_PROGRESS:
  ├─ (User clicks Skip) ──────────────────────► STEP_SKIPPED
  │                                               │
  │                                               ↓
  │                                    (Tutorial continues or skipped fully)
  │
  ├─ (Session timeout / page reload) ─────────► TUTORIAL_PAUSED
  │                                               │
  │                                               ↓
  │                                    (User can RESUME from last step)
  │
  └─ (User closes help) ──────────────────────► TUTORIAL_EXITED
                                                  │
                                                  ↓
                                        (Progress saved, can resume)

TERMINAL STATES:
  • TUTORIAL_COMPLETED (user finished all steps)
  • TUTORIAL_ABANDONED (user exited without completing)
  • TUTORIAL_SKIPPED_FULLY (user skipped all steps)
```

---

## Tutorial Step Types Specification

