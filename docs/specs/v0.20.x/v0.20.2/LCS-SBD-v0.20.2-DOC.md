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

### Step Type Definitions

#### 1. Introduction Step
Introduces the tutorial goal, prerequisites, and estimated time. Sets user expectations.

```json
{
  "stepType": "introduction",
  "stepNumber": 1,
  "title": "Getting Started with Collaboration",
  "duration": "2 minutes",
  "content": "In this tutorial, you'll learn how to...",
  "prerequisites": ["Workspace created", "At least one project"],
  "estimatedTotalTime": "15 minutes",
  "skills": ["Beginner"]
}
```

#### 2. Text & Content Step
Displays informational content with text, images, videos, and callout boxes.

```json
{
  "stepType": "content",
  "stepNumber": 2,
  "title": "Understanding Workspace Roles",
  "content": "Markdown/HTML content here",
  "sections": [
    {
      "heading": "Admin Role",
      "body": "Admins have full control...",
      "type": "info"
    },
    {
      "heading": "Editor Role",
      "body": "Editors can modify content...",
      "type": "warning"
    }
  ],
  "images": ["url1", "url2"],
  "videos": ["videoId1"]
}
```

#### 3. Interactive Input Step
Requires user input validation before proceeding.

```json
{
  "stepType": "input",
  "stepNumber": 3,
  "title": "Create Your First Workspace",
  "instructions": "Enter a workspace name...",
  "form": {
    "fields": [
      {
        "name": "workspaceName",
        "label": "Workspace Name",
        "type": "text",
        "required": true,
        "placeholder": "e.g., My Team Workspace",
        "validation": {
          "minLength": 3,
          "maxLength": 50,
          "pattern": "^[a-zA-Z0-9-_\\s]+$",
          "errorMessage": "Name must be 3-50 characters..."
        }
      }
    ]
  },
  "submitButton": "Create Workspace",
  "validationMessage": "Workspace created successfully!"
}
```

#### 4. Hands-On Exercise Step
Guides user through performing an action in the application.

```json
{
  "stepType": "exercise",
  "stepNumber": 4,
  "title": "Invite Team Members",
  "instructions": "Follow these steps to invite your team...",
  "steps": [
    "Click the 'Settings' button in the top navigation",
    "Select 'Team Members' from the sidebar",
    "Click 'Invite Member' button",
    "Enter email addresses (comma-separated)",
    "Click 'Send Invitations'"
  ],
  "validation": {
    "type": "actionCompleted",
    "triggerElement": "button[data-test='invite-submit']",
    "expectedState": "Team members successfully invited",
    "message": "Great! Team members have been invited."
  },
  "hint": "Can't find the Settings button? It's in the top-right corner next to your profile."
}
```

#### 5. Quiz/Knowledge Check Step
Tests user understanding with multiple choice or true/false questions.

```json
{
  "stepType": "quiz",
  "stepNumber": 5,
  "title": "Knowledge Check",
  "instructions": "Answer the following questions...",
  "questions": [
    {
      "id": "q1",
      "question": "What permission level allows editing but not deletion?",
      "type": "multipleChoice",
      "options": [
        {"id": "a", "text": "Viewer"},
        {"id": "b", "text": "Editor"},
        {"id": "c", "text": "Admin"},
        {"id": "d", "text": "Guest"}
      ],
      "correctAnswer": "b",
      "feedback": {
        "correct": "Correct! Editors can modify content but cannot delete it.",
        "incorrect": "Not quite. Remember, Editors can modify but not delete."
      }
    }
  ],
  "passingScore": 0.8,
  "failureMessage": "Please review the previous steps and try again."
}
```

#### 6. Code Example Step
Displays code snippets with copy-to-clipboard and optional syntax highlighting.

```json
{
  "stepType": "code",
  "stepNumber": 6,
  "title": "Using the API",
  "instructions": "Here's how to call the collaboration API...",
  "codeExamples": [
    {
      "language": "csharp",
      "title": "C# Example",
      "code": "var collaboration = new CollaborationService();\nawait collaboration.InviteMemberAsync(\"email@example.com\");",
      "copyable": true
    },
    {
      "language": "javascript",
      "title": "JavaScript Example",
      "code": "const collaboration = new CollaborationService();\nawait collaboration.inviteMember('email@example.com');",
      "copyable": true
    }
  ],
  "notes": "API keys required. See authentication documentation."
}
```

#### 7. Video Step
Embeds video content with optional transcript and chapters.

```json
{
  "stepType": "video",
  "stepNumber": 7,
  "title": "Video: Advanced Collaboration Features",
  "videoId": "youtube-video-id",
  "platform": "youtube",
  "duration": "5:30",
  "transcript": "Here's the full transcript...",
  "chapters": [
    {"timestamp": "0:00", "title": "Introduction"},
    {"timestamp": "1:30", "title": "Real-time Editing"},
    {"timestamp": "3:00", "title": "Comments & Feedback"}
  ],
  "autoplay": false,
  "subtitles": true
}
```

#### 8. Branching Step
Presents conditional paths based on user choice or context.

```json
{
  "stepType": "branching",
  "stepNumber": 8,
  "title": "Choose Your Setup Path",
  "instructions": "Which best describes your use case?",
  "branches": [
    {
      "id": "branch_team",
      "label": "I'm setting up a team",
      "nextStep": 9,
      "description": "Multiple team members collaborating"
    },
    {
      "id": "branch_solo",
      "label": "I'm working solo",
      "nextStep": 15,
      "description": "Individual user with optional guests"
    },
    {
      "id": "branch_enterprise",
      "label": "I'm an enterprise user",
      "nextStep": 20,
      "description": "SSO, advanced permissions, compliance"
    }
  ]
}
```

#### 9. Completion Step
Celebrates user completion and shows achievement badge.

```json
{
  "stepType": "completion",
  "stepNumber": 10,
  "title": "Congratulations!",
  "content": "You've successfully completed this tutorial!",
  "achievements": [
    {
      "id": "collab_master",
      "name": "Collaboration Master",
      "icon": "🎯",
      "description": "Completed the collaboration tutorial"
    }
  ],
  "nextSteps": [
    {"title": "Advanced Collaboration Features", "tutorialId": "adv-collab"},
    {"title": "Team Management", "tutorialId": "team-mgmt"}
  ],
  "shareButton": true,
  "feedbackForm": true
}
```

---

## Guided Tour Specification

### Tour Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    GUIDED TOUR ARCHITECTURE                     │
└─────────────────────────────────────────────────────────────────┘

TOUR DEFINITION (WYSIWYG Builder)
    │
    ├─ Tour Name: "Workspace Setup Tour"
    ├─ Trigger: "first_login" or "manual"
    ├─ Target Audience: "new_users"
    ├─ Feature Flags: ["workspace_collaboration"]
    │
    ↓
TOUR STEPS (Tour Stops)
    │
    Step 1: Highlight "Create Workspace" button
    Step 2: Highlight "Settings" menu
    Step 3: Highlight "Team Members" section
    Step 4: Highlight "Invite Button"
    │
    ↓
ELEMENT TARGETING
    │
    ├─ CSS Selector: "#create-workspace-btn"
    ├─ Fallback DOM Path: "html > body > div#app > header > button"
    ├─ Data Attributes: "data-tour='workspace-create'"
    ├─ Coordinates: {x: 500, y: 100} (for resilience)
    │
    ↓
OVERLAY & POSITIONING
    │
    ├─ Spotlight Effect: Circle around element
    ├─ Tooltip Position: "bottom" (auto-adjust if off-screen)
    ├─ Z-Index: 10000 (ensure above all content)
    ├─ Padding: 8px around highlighted element
    │
    ↓
TOUR EXECUTION
    │
    ├─ Render overlay
    ├─ Highlight target element
    ├─ Display tooltip with text
    ├─ Wait for user interaction (next, skip, etc.)
    ├─ Handle dynamic content (re-evaluate selectors)
    │
    ↓
PERSISTENCE & STATE
    │
    ├─ Save tour progress in localStorage
    ├─ Track completed tours in user_settings
    ├─ Suppress repeat tours (unless forced)
    │
    ↓
ANALYTICS
    │
    ├─ Log: tour view, step changes, skips
    ├─ Measure: completion rate, drop-off points
    ├─ Track: conversions (did user complete action after tour?)
```

### Tour Step Definition

```json
{
  "tourId": "tour-workspace-setup",
  "tourName": "Workspace Setup Tour",
  "description": "Learn how to set up your workspace",
  "version": 1,
  "enabled": true,
  "triggers": [
    {
      "type": "firstLogin",
      "delay": "3 seconds"
    }
  ],
  "targeting": {
    "userRole": ["user"],
    "isNewUser": true,
    "featureFlags": ["workspace_collaboration"],
    "audience": "new_users"
  },
  "stops": [
    {
      "stopNumber": 1,
      "title": "Welcome to Your Workspace!",
      "description": "Let's set up your workspace. First, click here to create a new workspace.",
      "elementSelector": "#create-workspace-btn",
      "elementFallback": "[data-tour='workspace-create']",
      "position": "bottom",
      "offsetX": 0,
      "offsetY": 10,
      "highlightPadding": 8,
      "actionType": "click",
      "expectedAction": "createElement",
      "nextCondition": "always",
      "skipAllowed": true,
      "duration": null
    },
    {
      "stopNumber": 2,
      "title": "Workspace Settings",
      "description": "Configure your workspace settings here.",
      "elementSelector": "#settings-menu",
      "position": "right",
      "actionType": "hover",
      "expectedAction": "menuOpen",
      "nextCondition": "manual",
      "skipAllowed": true
    }
  ],
  "styling": {
    "primaryColor": "#0066FF",
    "tooltipWidth": 320,
    "darkMode": true,
    "fontFamily": "system-ui"
  },
  "analytics": {
    "trackViews": true,
    "trackCompletions": true,
    "trackSkips": true,
    "conversionTracking": true
  }
}
```

---

## Knowledge Base Article Schema

### Article Structure

```json
{
  "articleId": "kb-article-collab-001",
  "title": "How to Set Up Team Collaboration",
  "slug": "setup-team-collaboration",
  "description": "Step-by-step guide for setting up collaboration in your workspace",
  "category": {
    "id": "collab",
    "name": "Collaboration",
    "path": "Features > Collaboration"
  },
  "tags": ["collaboration", "team", "setup", "beginner"],
  "skillLevel": "beginner",
  "contentType": "how-to",
  "author": {
    "id": "user-123",
    "name": "John Doe",
    "avatar": "url"
  },
  "created": "2025-01-15T10:00:00Z",
  "updated": "2025-01-20T14:30:00Z",
  "status": "published",
  "body": {
    "markdown": "## Setting Up Collaboration\n\n1. Navigate to workspace settings\n2. Click on Team Members\n3. Invite your team...",
    "sections": [
      {
        "id": "section-1",
        "heading": "Prerequisites",
        "content": "...",
        "level": 2
      },
      {
        "id": "section-2",
        "heading": "Step-by-Step Instructions",
        "content": "...",
        "subsections": [...]
      }
    ]
  },
  "media": {
    "images": [
      {
        "url": "s3://bucket/image1.png",
        "alt": "Workspace settings screen",
        "caption": "Navigate to workspace settings"
      }
    ],
    "videos": [
      {
        "videoId": "yt-123",
        "platform": "youtube",
        "title": "Setting Up Team Collaboration",
        "timestamp": "1:30"
      }
    ]
  },
  "relatedArticles": [
    {
      "id": "kb-article-collab-002",
      "title": "Managing Team Permissions",
      "score": 0.95
    }
  ],
  "faq": [
    {
      "question": "Can I change permissions after inviting?",
      "answer": "Yes, you can always modify team member permissions..."
    }
  ],
  "metadata": {
    "wordCount": 1250,
    "estimatedReadTime": "5 minutes",
    "views": 5432,
    "helpfulCount": 342,
    "unhelpfulCount": 18,
    "helpfulRatio": 0.95,
    "lastViewedBy": "user-456",
    "lastViewedAt": "2025-02-01T09:15:00Z"
  },
  "seo": {
    "metaDescription": "Learn how to set up team collaboration...",
    "keywords": ["collaboration", "team", "setup"],
    "canonicalUrl": "https://help.example.com/collab/setup"
  },
  "searchTerms": [
    "how to collaborate",
    "team setup",
    "sharing permissions"
  ]
}
```

---

## PostgreSQL Schema

### Tables

```sql
-- Help Articles (Knowledge Base)
CREATE TABLE help_articles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    body TEXT NOT NULL,
    body_html TEXT,
    category_id UUID REFERENCES help_categories(id),
    status VARCHAR(50) DEFAULT 'draft', -- draft, review, published, archived
    author_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMP,
    word_count INT,
    estimated_read_time_minutes INT,
    views_count INT DEFAULT 0,
    helpful_votes INT DEFAULT 0,
    unhelpful_votes INT DEFAULT 0,
    last_viewed_at TIMESTAMP,
    search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('english', COALESCE(title, '') || ' ' || COALESCE(description, '') || ' ' || COALESCE(body, ''))
    ) STORED,
    INDEX idx_slug ON help_articles(slug),
    INDEX idx_category ON help_articles(category_id),
    INDEX idx_status ON help_articles(status),
    INDEX idx_search_vector ON help_articles USING gin(search_vector),
    INDEX idx_created_at ON help_articles(created_at DESC)
);

-- Help Categories
CREATE TABLE help_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES help_categories(id),
    display_order INT,
    icon_url VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_parent ON help_categories(parent_id),
    INDEX idx_display_order ON help_categories(display_order)
);

-- Tutorials
CREATE TABLE tutorials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    category_id UUID REFERENCES help_categories(id),
    skill_level VARCHAR(50), -- beginner, intermediate, advanced
    estimated_duration_minutes INT,
    status VARCHAR(50) DEFAULT 'draft',
    author_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMP,
    views_count INT DEFAULT 0,
    completions_count INT DEFAULT 0,
    average_rating NUMERIC(3, 2),
    INDEX idx_slug ON tutorials(slug),
    INDEX idx_category ON tutorials(category_id),
    INDEX idx_status ON tutorials(status)
);

-- Tutorial Steps
CREATE TABLE tutorial_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tutorial_id UUID NOT NULL REFERENCES tutorials(id) ON DELETE CASCADE,
    step_number INT NOT NULL,
    step_type VARCHAR(50) NOT NULL, -- introduction, content, input, exercise, quiz, code, video, branching, completion
    title VARCHAR(255) NOT NULL,
    content TEXT,
    content_html TEXT,
    instructions TEXT,
    form_schema JSONB,
    validation_rules JSONB,
    code_examples JSONB,
    video_id VARCHAR(255),
    video_platform VARCHAR(50),
    branches JSONB,
    hints TEXT[],
    duration_minutes INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tutorial_id, step_number),
    INDEX idx_tutorial ON tutorial_steps(tutorial_id),
    INDEX idx_step_number ON tutorial_steps(step_number)
);

-- User Tutorial Progress
CREATE TABLE user_tutorial_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tutorial_id UUID NOT NULL REFERENCES tutorials(id) ON DELETE CASCADE,
    session_id UUID,
    current_step_number INT,
    completed_steps INT[],
    status VARCHAR(50), -- in_progress, completed, abandoned, paused
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    time_spent_minutes INT,
    variant_id VARCHAR(255),
    UNIQUE(user_id, tutorial_id),
    INDEX idx_user ON user_tutorial_progress(user_id),
    INDEX idx_tutorial ON user_tutorial_progress(tutorial_id),
    INDEX idx_status ON user_tutorial_progress(status)
);

-- Guided Tours
CREATE TABLE guided_tours (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    trigger_type VARCHAR(50), -- first_login, manual, contextual, feature_unlock
    trigger_delay_seconds INT,
    target_user_role VARCHAR(50)[],
    is_new_user_only BOOLEAN DEFAULT false,
    feature_flags VARCHAR(255)[],
    enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMP,
    views_count INT DEFAULT 0,
    completions_count INT DEFAULT 0,
    INDEX idx_enabled ON guided_tours(enabled),
    INDEX idx_trigger_type ON guided_tours(trigger_type)
);

-- Tour Stops (Steps in a tour)
CREATE TABLE tour_stops (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tour_id UUID NOT NULL REFERENCES guided_tours(id) ON DELETE CASCADE,
    stop_number INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    element_selector VARCHAR(500),
    element_fallback VARCHAR(500),
    position VARCHAR(50), -- top, bottom, left, right
    offset_x INT DEFAULT 0,
    offset_y INT DEFAULT 10,
    highlight_padding INT DEFAULT 8,
    action_type VARCHAR(50), -- click, hover, manual
    expected_action VARCHAR(50),
    next_condition VARCHAR(50), -- always, manual, conditional
    skip_allowed BOOLEAN DEFAULT true,
    duration_seconds INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tour_id, stop_number),
    INDEX idx_tour ON tour_stops(tour_id)
);

-- User Tour Progress
CREATE TABLE user_tour_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tour_id UUID NOT NULL REFERENCES guided_tours(id) ON DELETE CASCADE,
    session_id UUID,
    current_stop_number INT,
    completed_stops INT[],
    status VARCHAR(50), -- in_progress, completed, skipped
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    suppressed_until TIMESTAMP,
    UNIQUE(user_id, tour_id),
    INDEX idx_user ON user_tour_progress(user_id),
    INDEX idx_tour ON user_tour_progress(tour_id)
);

-- FAQ Entries
CREATE TABLE faq_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    article_id UUID REFERENCES help_articles(id),
    question TEXT NOT NULL,
    answer TEXT NOT NULL,
    display_order INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_article ON faq_entries(article_id)
);

-- Video Content
CREATE TABLE video_content (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    video_id VARCHAR(255) NOT NULL UNIQUE,
    platform VARCHAR(50), -- youtube, vimeo, custom
    duration_seconds INT,
    thumbnail_url VARCHAR(255),
    transcript TEXT,
    transcript_vector tsvector,
    chapters JSONB,
    categories VARCHAR(255)[],
    tags VARCHAR(100)[],
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    views_count INT DEFAULT 0,
    watch_time_seconds INT DEFAULT 0,
    INDEX idx_video_id ON video_content(video_id),
    INDEX idx_transcript_vector ON video_content USING gin(transcript_vector)
);

-- Help Article Feedback
CREATE TABLE help_article_feedback (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    article_id UUID NOT NULL REFERENCES help_articles(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    helpful BOOLEAN NOT NULL,
    feedback_text TEXT,
    rating INT, -- 1-5 stars
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_article ON help_article_feedback(article_id),
    INDEX idx_user ON help_article_feedback(user_id),
    INDEX idx_helpful ON help_article_feedback(helpful)
);

-- Search Events (Analytics)
CREATE TABLE help_search_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    search_query VARCHAR(500),
    filters JSONB,
    result_count INT,
    clicked_results INT[],
    first_click_position INT,
    time_to_click_seconds INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    session_id UUID,
    INDEX idx_user ON help_search_events(user_id),
    INDEX idx_created_at ON help_search_events(created_at DESC),
    INDEX idx_query ON help_search_events(search_query)
);

-- Help Analytics Events
CREATE TABLE help_analytics_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    event_type VARCHAR(100), -- help_search, tutorial_start, tutorial_complete, tour_view, etc.
    event_data JSONB,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    session_id UUID,
    INDEX idx_user ON help_analytics_events(user_id),
    INDEX idx_event_type ON help_analytics_events(event_type),
    INDEX idx_timestamp ON help_analytics_events(timestamp DESC)
);

-- Create indexes for better query performance
CREATE INDEX idx_help_search_events_query_ts ON help_search_events(search_query, created_at DESC);
CREATE INDEX idx_article_feedback_helpful_ts ON help_article_feedback(helpful, created_at DESC);
CREATE INDEX idx_tutorial_steps_tutorial_order ON tutorial_steps(tutorial_id, step_number);
```

---

## UI Mockups Description

### 1. Help Center Search Interface

```
┌──────────────────────────────────────────────────────────────────┐
│  Help Center                                                   🔍 │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 🔍 How to set up collaboration...  [search filters ▼]    │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  SUGGESTED SEARCHES: workspaces • team • permissions            │
│                                                                   │
├─────────────────────────────────────────────────────────────────┤
│ FILTERS                           SEARCH RESULTS (24)            │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ Category                          ┌────────────────────────────┐ │
│ ☐ Getting Started                 │ ★ How to Set Up Collab     │ │
│ ☑ Features (3)                    │ Features > Collaboration   │ │
│ ☐ Troubleshooting                 │ 95% helpful | Updated 2d   │ │
│ ☐ Integration                     │ Set up collaboration in... │ │
│                                    │ [Read Article] [Video]     │ │
│ Article Type                       └────────────────────────────┘ │
│ ☑ How-To (12)                      ┌────────────────────────────┐ │
│ ☑ FAQ (8)                          │ Managing Team Permissions  │ │
│ ☐ Troubleshooting                 │ Features > Collaboration   │ │
│ ☐ Video                            │ 92% helpful | Updated 5d   │ │
│                                    │ Learn how to modify team... │ │
│ Skill Level                        │ [Read Article]             │ │
│ ☐ Beginner                         └────────────────────────────┘ │
│ ☑ Intermediate (10)                ┌────────────────────────────┐ │
│ ☑ Advanced (6)                     │ Troubleshooting: Member... │ │
│                                    │ Troubleshooting            │ │
│ Updated                            │ 78% helpful | Updated 1w   │ │
│ ☑ Last 7 days (18)                │ If team members can't...   │ │
│ ☐ Last 30 days (5)                │ [Read Article]             │ │
│ ☐ Last 90 days (1)                └────────────────────────────┘ │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 2. Tutorial Player Interface

```
┌──────────────────────────────────────────────────────────────────┐
│ Getting Started with Collaboration  [50% Complete]         [ ] │ │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  TUTORIAL OUTLINE                 TUTORIAL CONTENT              │
│  ──────────────────────────────    ──────────────────────────  │
│  1. Introduction            ✓      Step 3 of 6: Add Members    │
│  2. Workspace Setup         ✓      ────────────────────────    │
│  3. Add Members             ● (now)                             │
│  4. Set Permissions                 Now that your workspace is   │
│  5. Best Practices          ⚪      set up, let's invite your    │
│  6. Completion              ⚪      team members!               │
│                                                                   │
│                                    ┌─────────────────┐           │
│                                    │ 💡 HINT (click) │           │
│                                    └─────────────────┘           │
│                                                                   │
│                                    INTERACTIVE FORM:             │
│                                    ─────────────────             │
│                                    Email addresses:              │
│                                    [jane@company.com]            │
│                                    [bob@company.com ]            │
│                                    [_______________]             │
│                                                                   │
│                                    Role: [Admin  ▼]              │
│                                                                   │
│                                    [+ Add another] [Submit]     │
│                                                                   │
│  [Estimated time: 3 min left]      [Skip] [← Prev] [Next →]    │
└──────────────────────────────────────────────────────────────────┘
```

### 3. Guided Tour Overlay

```
┌──────────────────────────────────────────────────────────────────┐
│                    MAIN APPLICATION UI                           │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Workspace: My Team                 [Settings] [Profile]│   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
│  [Projects] [Team] [Analytics] [Help]                           │
│                                                                   │
│  ╔════════════════════════════════════════════════════════╗     │
│  ║                                                        ║     │
│  ║  ┌──────────────────────────────────────────────────┐ ║     │
│  ║  │                                                  │ ║     │
│  ║  │         ⭐ SPOTLIGHT ON FEATURE ⭐             │ ║     │
│  ║  │                                                  │ ║     │
│  ║  │  ┌────────────────────────────────────────────┐ │ ║     │
│  ║  │  │ Step 2 of 4: Team Management              │ │ ║     │
│  ║  │  │                                            │ │ ║     │
│  ║  │  │ Click the "Team" menu to manage team      │ │ ║     │
│  ║  │  │ members and permissions.                  │ │ ║     │
│  ║  │  │                                            │ │ ║     │
│  ║  │  │              [Skip]   [Next ▶]            │ │ ║     │
│  ║  │  └────────────────────────────────────────────┘ │ ║     │
│  ║  │                                                  │ ║     │
│  ║  │  [Team] ◄── HIGHLIGHTED ELEMENT                │ ║     │
│  ║  │   • Members                                     │ ║     │
│  ║  │   • Roles                                       │ ║     │
│  ║  │   • Permissions                                 │ ║     │
│  ║  │                                                  │ ║     │
│  ║  └──────────────────────────────────────────────────┘ ║     │
│  ║                                                        ║     │
│  ║  (background dimmed and blurred)                      ║     │
│  ║                                                        ║     │
│  ╚════════════════════════════════════════════════════════╝     │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 4. Knowledge Base Browser

```
┌──────────────────────────────────────────────────────────────────┐
│  Knowledge Base                                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│ CATEGORIES                                                       │
│ ──────────────────────────────────────────────────────────────  │
│ ▼ Features (23 articles)                                        │
│   ▼ Collaboration (8)                                            │
│     • How to Set Up Collaboration    [5 min read] [95% helpful] │
│     • Managing Team Permissions      [7 min read]               │
│     • Best Practices                 [6 min read]               │
│   ▼ Analytics (10)                                               │
│   ▼ Library (5)                                                  │
│ ▶ Getting Started (12 articles)                                  │
│ ▶ Troubleshooting (8 articles)                                   │
│ ▶ Integration (6 articles)                                       │
│                                                                   │
├──────────────────────────────────────────────────────────────────┤
│                      ARTICLE CONTENT VIEW                        │
│                                                                   │
│ HOME > FEATURES > COLLABORATION > SETUP                          │
│                                                                   │
│ How to Set Up Team Collaboration                    ★★★★★      │
│ By John Doe  •  Updated Jan 20  •  5 min read  •  5.4K views   │
│                                                                   │
│ TABLE OF CONTENTS:                                               │
│ 1. Prerequisites                                                │
│ 2. Step-by-Step Instructions                                   │
│ 3. Troubleshooting                                             │
│ 4. FAQ                                                         │
│                                                                   │
│ [Article body with rich formatting]                             │
│                                                                   │
│ ────────────────────────────────────────────────────────────   │
│                                                                   │
│ Was this helpful?  [👍 Yes] [👎 No] [Comments ...]             │
│                                                                   │
│ RELATED ARTICLES:                                               │
│ • Managing Team Permissions                                     │
│ • Troubleshooting: Member Access Issues                        │
│ • Best Practices for Collaboration                             │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Dependency Chain

### Component Dependencies

```
help_system/
├── HelpCenter
│   ├── Depends: IHelpSearch
│   ├── Depends: IKnowledgeBase
│   ├── Depends: IArticleFeedback
│   ├── Depends: IHelpAnalytics
│   └── Depends: SearchEngine (Elasticsearch or PostgreSQL)
│
├── TutorialSystem
│   ├── Depends: ITutorialRepository
│   ├── Depends: IUserProgressRepository
│   ├── Depends: ICodeValidator
│   ├── Depends: IQuizEngine
│   └── Depends: IHelpAnalytics
│
├── GuidedTourService
│   ├── Depends: ITourRepository
│   ├── Depends: IUserTourProgressRepository
│   ├── Depends: IElementTargetingService
│   ├── Depends: ITriggerEvaluationService
│   └── Depends: IHelpAnalytics
│
├── KnowledgeBaseService
│   ├── Depends: IArticleRepository
│   ├── Depends: IArticleVersionRepository
│   ├── Depends: IContentRenderer
│   ├── Depends: ISearchService
│   └── Depends: IRecommendationEngine
│
├── VideoHelpService
│   ├── Depends: IVideoRepository
│   ├── Depends: ITranscriptService
│   ├── Depends: IVideoHostingProvider
│   └── Depends: IHelpAnalytics
│
└── HelpAnalytics
    ├── Depends: IEventStore
    ├── Depends: IAnalyticsRepository
    ├── Depends: IReportingService
    └── Depends: IAlertingService

INFRASTRUCTURE DEPENDENCIES:
├── PostgreSQL (data storage)
├── Elasticsearch/Meilisearch (full-text search)
├── Redis (caching, session state)
├── S3/CloudStorage (media storage)
├── Video Platform API (YouTube/Vimeo)
├── Speech-to-Text Service (AWS Transcribe, Google Cloud Speech)
├── MediatR (event publishing)
└── AutoMapper (DTO mapping)
```

---

## License Gating

### Feature Availability by License Tier

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| **Searchable Help Center** | ✓ Limited (100 articles) | ✓ Full | ✓ Full |
| **Knowledge Base** | ✓ Read-only | ✓ Contribute | ✓ Full Control |
| **Interactive Tutorials** | ✓ Basic (5 tutorials) | ✓ All (50+ tutorials) | ✓ Custom |
| **Tutorial Analytics** | ✗ | ✓ | ✓ |
| **Guided Tours** | ✓ Auto-triggered | ✓ All triggers | ✓ All triggers + Custom |
| **Video Help** | ✓ Platform videos | ✓ Platform + Custom | ✓ Unlimited |
| **Search Analytics** | ✗ | ✓ Basic | ✓ Advanced |
| **Content Feedback** | ✓ View feedback | ✗ | ✓ Full Access |
| **Custom Help Content** | ✗ | ✗ | ✓ |
| **Help Analytics Dashboard** | ✗ | ✓ Basic Dashboard | ✓ Advanced Reports |
| **API Access** | ✗ | ✓ Read-only | ✓ Full |
| **SSO for Help** | ✗ | ✗ | ✓ |
| **Custom Branding** | ✗ | ✗ | ✓ |

---

## Performance Targets

### Response Time Targets

| Operation | Target | Metric | Notes |
|-----------|--------|--------|-------|
| Help Search | < 100ms | P95 | Includes network latency |
| Article Load | < 300ms | P95 | Full page render |
| Tutorial Load | < 500ms | P95 | First paint + content render |
| Tour Initialization | < 200ms | P95 | Overlay + element targeting |
| Video Embed Load | < 1000ms | P95 | YouTube/Vimeo embed ready |
| Transcript Search | < 150ms | P95 | Video transcript indexed search |
| Analytics Event Log | < 50ms | P99 | Async, non-blocking |
| Related Articles | < 200ms | P95 | Recommendation engine |
| Guided Tour Render | < 150ms | P95 | Overlay rendering |

### Scalability Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Help Articles | 10,000+ | Searchable, indexed |
| Concurrent Users | 10,000+ | Simultaneous help access |
| Search QPS | 5,000+ | Queries per second |
| Article Views/Day | 1M+ | Analytics tracking |
| Tutorial Completions/Day | 100K+ | Progress tracking |
| Tour Views/Day | 500K+ | Distributed load |
| Video Content | 1,000+ | With transcripts |

### Availability & Reliability

| Metric | Target |
|--------|--------|
| Help System Uptime | 99.9% |
| Search Engine Availability | 99.95% |
| Database Availability | 99.95% |
| CDN Availability | 99.99% |
| Help Content Sync | Real-time (< 30 sec) |

---

## Testing Strategy

### Unit Testing

```csharp
[TestFixture]
public class HelpSearchServiceTests
{
    private IHelpCenter _helpCenter;
    private Mock<ISearchEngine> _mockSearchEngine;

    [SetUp]
    public void Setup()
    {
        _mockSearchEngine = new Mock<ISearchEngine>();
        _helpCenter = new HelpCenterService(_mockSearchEngine.Object, new Mock<IHelpAnalytics>().Object);
    }

    [Test]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = new HelpSearchQuery { SearchText = "collaboration" };
        var expectedResults = new List<HelpSearchResult> { /* mock data */ };
        _mockSearchEngine.Setup(x => x.SearchAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _helpCenter.SearchAsync(query);

        // Assert
        Assert.That(result.Count, Is.EqualTo(expectedResults.Count));
        _mockSearchEngine.Verify(x => x.SearchAsync("collaboration"), Times.Once);
    }

    [Test]
    public async Task SearchAsync_WithZeroResults_LogsContentGap()
    {
        // Arrange
        var query = new HelpSearchQuery { SearchText = "nonexistent-feature" };
        var emptyResults = new List<HelpSearchResult>();
        _mockSearchEngine.Setup(x => x.SearchAsync(It.IsAny<string>()))
            .ReturnsAsync(emptyResults);

        // Act & Assert
        var result = await _helpCenter.SearchAsync(query);
        Assert.That(result.Count, Is.EqualTo(0));
        // Verify content gap logging
    }
}

[TestFixture]
public class TutorialProgressTests
{
    [Test]
    public async Task UpdateStepProgress_WithValidation_AdvancesToNextStep()
    {
        // Arrange
        var tutorialService = new TutorialSystemService();
        var tutorialId = Guid.NewGuid().ToString();
        var stepProgress = new TutorialStepProgress { StepNumber = 1, IsCompleted = true };

        // Act
        var result = await tutorialService.UpdateStepProgressAsync(tutorialId, 1, stepProgress);

        // Assert
        Assert.That(result.CurrentStepNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateStepProgress_WithFailedValidation_DoesNotAdvance()
    {
        // Arrange
        var tutorialService = new TutorialSystemService();
        var tutorialId = Guid.NewGuid().ToString();
        var stepProgress = new TutorialStepProgress { StepNumber = 1, IsCompleted = false };

        // Act
        var result = await tutorialService.UpdateStepProgressAsync(tutorialId, 1, stepProgress);

        // Assert
        Assert.That(result.CurrentStepNumber, Is.EqualTo(1));
    }
}
```

### Integration Testing

```csharp
[TestFixture]
public class HelpCenterIntegrationTests
{
    private IServiceCollection _services;
    private IHelpCenter _helpCenter;

    [OneTimeSetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        _services.AddHelpSystemServices();
        _services.AddScoped<IHelpCenter, HelpCenterService>();

        var provider = _services.BuildServiceProvider();
        _helpCenter = provider.GetRequiredService<IHelpCenter>();
    }

    [Test]
    public async Task SearchAndViewArticle_TracksAnalytics()
    {
        // Arrange
        var searchQuery = new HelpSearchQuery { SearchText = "collaboration" };

        // Act
        var searchResult = await _helpCenter.SearchAsync(searchQuery);
        var articleDetail = await _helpCenter.GetArticleAsync(searchResult.FirstOrDefault()?.ArticleId);

        // Assert
        Assert.That(articleDetail, Is.Not.Null);
        // Verify analytics events were logged
    }
}
```

### End-to-End Testing

```gherkin
Scenario: User searches help and finds article
  Given user is on help center
  When user searches for "collaboration"
  Then search results display relevant articles
    And first result is "How to Set Up Collaboration"
    And result shows helpful votes and read time

  When user clicks first result
  Then article opens with full content
    And table of contents is visible
    And related articles display
    And helpful/not helpful button is available

  When user clicks "Helpful"
  Then feedback is recorded
    And helpful vote count increments

Scenario: User completes tutorial
  Given user starts "Getting Started" tutorial
  When user navigates to first step
  Then introduction displays with estimated time

  When user completes each step
  Then progress bar updates
    And step counter shows current/total

  When user completes final step
  Then completion page displays
    And badge is awarded
    And completion is recorded in history

Scenario: Guided tour appears for new user
  Given new user logs in for first time
  When application loads
  Then "Workspace Setup" tour auto-triggers after 3 seconds

  When user hovers over highlighted element
  Then tooltip shows guidance

  When user clicks "Next"
  Then tour advances to next step

  When user clicks "Skip Tour"
  Then tour dismisses
    And suppression is recorded
```

### Performance Testing

```csharp
[TestFixture]
[Explicit]
public class HelpSystemPerformanceTests
{
    [Test]
    public async Task SearchPerformance_ConcurrentUsers()
    {
        // Test with 1000 concurrent search requests
        var options = new ParallelOptions { MaxDegreeOfParallelism = 1000 };
        var stopwatch = Stopwatch.StartNew();

        var searchTasks = Enumerable.Range(1, 1000)
            .Select(i => _helpCenter.SearchAsync(new HelpSearchQuery { SearchText = "collaboration" }))
            .ToList();

        await Task.WhenAll(searchTasks);
        stopwatch.Stop();

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100 * 10)); // 100ms target with buffer
    }

    [Test]
    public async Task TutorialLoadPerformance_WithLargeContent()
    {
        // Test loading tutorial with 50 steps and rich content
        var stopwatch = Stopwatch.StartNew();
        var tutorial = await _tutorialService.GetTutorialAsync("large-tutorial-id");
        stopwatch.Stop();

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500));
        Assert.That(tutorial.Steps.Count, Is.EqualTo(50));
    }
}
```

### User Testing

#### Usability Testing Plan

1. **Task Completion Testing**
   - User finds and reads article on specific topic
   - User completes tutorial end-to-end
   - User dismisses tour and continues with work

2. **Search Effectiveness Testing**
   - User searches for common questions
   - Track search query patterns
   - Identify zero-result searches
   - Measure click-through rates

3. **Tutorial Effectiveness Testing**
   - User completes tutorial without guidance
   - Measure dropout rate per step
   - Track time spent per step
   - Gather feedback on clarity

4. **Navigation & Discoverability Testing**
   - User discovers help resources organically
   - Tour triggers appropriately
   - Related articles are relevant
   - Recommendations appear at right time

---

## Risks & Mitigations

### Risk 1: Search Index Staleness

**Risk**: Search index becomes out-of-sync with database, returning stale results

**Probability**: Medium
**Impact**: High (users get outdated information)

**Mitigation**:
- Implement real-time indexing with < 30 second latency
- Add monitoring/alerts for index sync lag
- Periodic full-index rebuild (daily off-peak)
- Dual-write pattern for critical updates
- Fallback to database full-text search if index unavailable

### Risk 2: Tutorial Progress Loss

**Risk**: User's tutorial progress is lost due to session timeout or crash

**Probability**: Low
**Impact**: High (user frustration)

**Mitigation**:
- Auto-save progress every 30 seconds
- Store progress in persistent database (not just localStorage)
- Implement offline caching with sync on reconnect
- Clear error messages and recovery instructions
- Test recovery with network disconnection simulation

### Risk 3: Element Targeting Failures in Tours

**Risk**: Tour elements can't be found due to UI changes or dynamic content

**Probability**: High (constant UI evolution)
**Impact**: Medium (tour fails silently)

**Mitigation**:
- Use multiple targeting strategies (CSS selector, DOM path, data attributes, coordinates)
- Implement element validation before tour starts
- Log element targeting failures for monitoring
- Graceful degradation (show tour hint without highlight if element not found)
- Add automated testing to catch DOM changes

### Risk 4: Help Content Quality

**Risk**: Outdated, inaccurate, or low-quality help content reduces trust

**Probability**: High
**Impact**: High (damages credibility)

**Mitigation**:
- Establish content ownership and review process
- Flag articles older than 90 days for review
- Monitor helpful/unhelpful votes for quality issues
- Surface low-engagement content for improvement
- Link articles to feature releases and deprecations
- Community feedback and rating system

### Risk 5: Tutorial Accessibility Issues

**Risk**: Tutorial content not accessible to users with disabilities

**Probability**: Medium
**Impact**: High (accessibility liability)

**Mitigation**:
- WCAG 2.1 AA compliance for all tutorial content
- Alt text for images and descriptions for videos
- Keyboard navigation support (Tab, Enter, Arrow keys)
- Screen reader testing with NVDA/JAWS
- Color contrast requirements
- Caption and transcript for video content
- Code examples with proper syntax highlighting

### Risk 6: Video Transcript Quality

**Risk**: Auto-generated transcripts contain errors, making search unreliable

**Probability**: Medium
**Impact**: Medium (search effectiveness)

**Mitigation**:
- Use high-quality speech-to-text service (AWS Transcribe with custom vocab)
- Manual review/correction of transcripts for critical videos
- Display confidence scores for auto-generated transcripts
- Allow user corrections and community contributions
- Monitor search query matches against transcript quality

### Risk 7: Analytics Data Privacy

**Risk**: Help system analytics expose sensitive user/workspace information

**Probability**: Low
**Impact**: High (GDPR/compliance violation)

**Mitigation**:
- Redact PII from search queries and event data
- Encrypt analytics data at rest and in transit
- Implement data retention and purge policies
- Use analytics service account (not user identity) when possible
- Regular privacy audits and compliance reviews
- User consent for analytics tracking

---

## MediatR Events

### Event Definitions

```csharp
// ============== SEARCH EVENTS ==============

/// <summary>
/// Fired when user performs a help center search
/// </summary>
public class HelpSearchedEvent : INotification
{
    public Guid UserId { get; set; }
    public string SearchQuery { get; set; }
    public Dictionary<string, object> Filters { get; set; }
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user clicks a search result
/// </summary>
public class SearchResultClickedEvent : INotification
{
    public Guid UserId { get; set; }
    public string SearchQuery { get; set; }
    public string ResultId { get; set; }
    public int ResultPosition { get; set; }
    public int SecondsToClick { get; set; }
    public DateTime ClickedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when search returns zero results
/// </summary>
public class ZeroResultSearchEvent : INotification
{
    public Guid UserId { get; set; }
    public string SearchQuery { get; set; }
    public Dictionary<string, object> Filters { get; set; }
    public DateTime SearchedAt { get; set; }
    public string SessionId { get; set; }
}

// ============== ARTICLE EVENTS ==============

/// <summary>
/// Fired when user views a help article
/// </summary>
public class ArticleViewedEvent : INotification
{
    public Guid UserId { get; set; }
    public string ArticleId { get; set; }
    public string ArticleTitle { get; set; }
    public string Source { get; set; } // search, direct, related, recommendation
    public DateTime ViewedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user leaves a help article after reading
/// </summary>
public class ArticleExitedEvent : INotification
{
    public Guid UserId { get; set; }
    public string ArticleId { get; set; }
    public int TimeSpentSeconds { get; set; }
    public decimal ScrollDepthPercentage { get; set; }
    public DateTime ExitedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user submits helpful/not helpful feedback
/// </summary>
public class ArticleFeedbackSubmittedEvent : INotification
{
    public Guid UserId { get; set; }
    public string ArticleId { get; set; }
    public bool IsHelpful { get; set; }
    public string? FeedbackText { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user rates an article
/// </summary>
public class ArticleRatedEvent : INotification
{
    public Guid UserId { get; set; }
    public string ArticleId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }
    public string SessionId { get; set; }
}

// ============== TUTORIAL EVENTS ==============

/// <summary>
/// Fired when user starts a tutorial
/// </summary>
public class TutorialStartedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TutorialId { get; set; }
    public string TutorialTitle { get; set; }
    public string VariantId { get; set; }
    public DateTime StartedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user completes a tutorial step
/// </summary>
public class TutorialStepCompletedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TutorialId { get; set; }
    public int StepNumber { get; set; }
    public string StepTitle { get; set; }
    public int TimeSpentSeconds { get; set; }
    public bool ValidationPassed { get; set; }
    public DateTime CompletedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user abandons a tutorial step
/// </summary>
public class TutorialStepSkippedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TutorialId { get; set; }
    public int StepNumber { get; set; }
    public string SkipReason { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime SkippedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user completes entire tutorial
/// </summary>
public class TutorialCompletedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TutorialId { get; set; }
    public string TutorialTitle { get; set; }
    public int TotalTimeSpentSeconds { get; set; }
    public int SkippedStepsCount { get; set; }
    public DateTime CompletedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user abandons a tutorial
/// </summary>
public class TutorialAbandonedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TutorialId { get; set; }
    public int LastCompletedStep { get; set; }
    public int TotalSteps { get; set; }
    public int TimeSpentSeconds { get; set; }
    public string AbandonReason { get; set; }
    public DateTime AbandonedAt { get; set; }
    public string SessionId { get; set; }
}

// ============== GUIDED TOUR EVENTS ==============

/// <summary>
/// Fired when a guided tour is shown to user
/// </summary>
public class GuidedTourStartedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TourId { get; set; }
    public string TourName { get; set; }
    public string StartReason { get; set; } // auto, manual, contextual
    public DateTime StartedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user advances to next tour step
/// </summary>
public class TourStepAdvancedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TourId { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public int TimeOnStepSeconds { get; set; }
    public DateTime AdvancedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user completes a guided tour
/// </summary>
public class GuidedTourCompletedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TourId { get; set; }
    public string TourName { get; set; }
    public int TotalTimeSpentSeconds { get; set; }
    public int StepsCompleted { get; set; }
    public int StepsSkipped { get; set; }
    public DateTime CompletedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user skips/dismisses a guided tour
/// </summary>
public class GuidedTourSkippedEvent : INotification
{
    public Guid UserId { get; set; }
    public string TourId { get; set; }
    public int StepReachedWhenSkipped { get; set; }
    public int TotalSteps { get; set; }
    public bool SuppressInFuture { get; set; }
    public DateTime SkippedAt { get; set; }
    public string SessionId { get; set; }
}

// ============== VIDEO EVENTS ==============

/// <summary>
/// Fired when user starts watching help video
/// </summary>
public class VideoPlayedEvent : INotification
{
    public Guid UserId { get; set; }
    public string VideoId { get; set; }
    public string VideoTitle { get; set; }
    public DateTime PlayedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired periodically as user watches video
/// </summary>
public class VideoEngagementEvent : INotification
{
    public Guid UserId { get; set; }
    public string VideoId { get; set; }
    public int CurrentPositionSeconds { get; set; }
    public int TotalDurationSeconds { get; set; }
    public decimal CompletionPercentage { get; set; }
    public DateTime RecordedAt { get; set; }
    public string SessionId { get; set; }
}

/// <summary>
/// Fired when user searches video transcripts
/// </summary>
public class VideoTranscriptSearchedEvent : INotification
{
    public Guid UserId { get; set; }
    public string SearchQuery { get; set; }
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }
    public string SessionId { get; set; }
}

// ============== ANALYTICS EVENTS ==============

/// <summary>
/// Base event for help system analytics
/// </summary>
public abstract class HelpAnalyticsEvent : INotification
{
    public Guid UserId { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object> EventData { get; set; }
    public DateTime Timestamp { get; set; }
    public string SessionId { get; set; }
}
```

### Event Handlers

```csharp
/// <summary>
/// Handles search analytics logging
/// </summary>
public class HelpSearchedEventHandler : INotificationHandler<HelpSearchedEvent>
{
    private readonly IHelpAnalyticsRepository _repository;
    private readonly ILogger<HelpSearchedEventHandler> _logger;

    public HelpSearchedEventHandler(
        IHelpAnalyticsRepository repository,
        ILogger<HelpSearchedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(HelpSearchedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.LogSearchEventAsync(
                notification.UserId,
                notification.SearchQuery,
                notification.Filters,
                notification.ResultCount,
                notification.SessionId,
                cancellationToken);

            _logger.LogInformation(
                "Help search logged: Query={Query}, UserId={UserId}, Results={Count}",
                notification.SearchQuery,
                notification.UserId,
                notification.ResultCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging help search event");
            // Don't throw - analytics shouldn't break user experience
        }
    }
}

/// <summary>
/// Handles tutorial completion analytics and follow-ups
/// </summary>
public class TutorialCompletedEventHandler : INotificationHandler<TutorialCompletedEvent>
{
    private readonly IHelpAnalyticsRepository _analyticsRepository;
    private readonly IUserAchievementService _achievementService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TutorialCompletedEventHandler> _logger;

    public TutorialCompletedEventHandler(
        IHelpAnalyticsRepository analyticsRepository,
        IUserAchievementService achievementService,
        INotificationService notificationService,
        ILogger<TutorialCompletedEventHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _achievementService = achievementService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(TutorialCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Log analytics
            await _analyticsRepository.LogTutorialCompletionAsync(
                notification.UserId,
                notification.TutorialId,
                notification.TotalTimeSpentSeconds,
                notification.SkippedStepsCount,
                cancellationToken);

            // Award achievement badge
            await _achievementService.AwardBadgeAsync(
                notification.UserId,
                "tutorial_completed",
                $"Completed: {notification.TutorialTitle}",
                cancellationToken);

            // Recommend next tutorial
            var nextTutorials = await _achievementService.GetRecommendedTutorialsAsync(
                notification.UserId,
                cancellationToken);

            // Send notification if enabled
            await _notificationService.SendAsync(
                notification.UserId,
                "Congratulations!",
                $"You completed {notification.TutorialTitle}",
                "tutorial_completion",
                cancellationToken);

            _logger.LogInformation(
                "Tutorial completed: UserId={UserId}, TutorialId={TutorialId}, Time={TimeSpent}s",
                notification.UserId,
                notification.TutorialId,
                notification.TotalTimeSpentSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tutorial completion event");
        }
    }
}

/// <summary>
/// Handles tour completion tracking and suppression
/// </summary>
public class GuidedTourCompletedEventHandler : INotificationHandler<GuidedTourCompletedEvent>
{
    private readonly IHelpAnalyticsRepository _analyticsRepository;
    private readonly IUserTourProgressRepository _progressRepository;
    private readonly ILogger<GuidedTourCompletedEventHandler> _logger;

    public GuidedTourCompletedEventHandler(
        IHelpAnalyticsRepository analyticsRepository,
        IUserTourProgressRepository progressRepository,
        ILogger<GuidedTourCompletedEventHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task Handle(GuidedTourCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Log tour completion
            await _analyticsRepository.LogTourCompletionAsync(
                notification.UserId,
                notification.TourId,
                notification.TotalTimeSpentSeconds,
                notification.StepsCompleted,
                cancellationToken);

            // Mark tour as completed (don't show auto-trigger again)
            await _progressRepository.MarkTourCompletedAsync(
                notification.UserId,
                notification.TourId,
                cancellationToken);

            _logger.LogInformation(
                "Tour completed: UserId={UserId}, TourId={TourId}, Steps={Steps}",
                notification.UserId,
                notification.TourId,
                notification.StepsCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tour completion event");
        }
    }
}
```

---

## Implementation Timeline

### Sprint 1 (Week 1)

**Days 1-2: Setup & Infrastructure (4 hours)**
- Set up PostgreSQL schema and migrations
- Configure Elasticsearch/search infrastructure
- Set up MediatR event handlers
- Create base interfaces and repository implementations

**Days 2-3: Searchable Help Center (8 hours)**
- Implement IHelpCenter interface
- Build search engine integration
- Create search API endpoints
- Implement search analytics logging

**Day 4: Code Review & Integration (3 hours)**
- Review code quality and test coverage
- Integration testing with database
- Performance baseline testing

### Sprint 2 (Week 2)

**Days 1-2: Interactive Tutorials (12 hours)**
- Implement ITutorialSystem interface
- Build tutorial player UI components
- Implement step validation and progress tracking
- Create tutorial analytics

**Days 3-4: Guided Tours & Knowledge Base (20 hours)**
- Implement IGuidedTourService
- Build tour overlay components
- Implement IKnowledgeBase
- Build knowledge base UI

**Day 5: Video & Analytics (10 hours)**
- Implement IVideoHelpService
- Build video integration and transcript search
- Implement IHelpAnalytics
- Create analytics dashboard

**Final Day: Testing & Documentation (5 hours)**
- E2E testing and UAT
- Performance testing
- Documentation and release notes

---

## Conclusion

v0.20.2-DOC represents a significant investment in user success through a comprehensive help system. By combining searchable content, interactive guidance, and data-driven improvement, Lexichord will dramatically improve user self-service capability and satisfaction.

### Success Metrics

1. **Help Center Usage**: Target 1M+ article views per month
2. **Tutorial Adoption**: Target 50K+ tutorial starts per month, 80%+ completion rate
3. **Support Impact**: Target 40% reduction in support tickets related to onboarding
4. **User Satisfaction**: Target +15 point increase in NPS
5. **Search Effectiveness**: Target 95%+ of searches return helpful results

