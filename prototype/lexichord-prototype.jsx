import React, { useState, useEffect } from 'react';
import { FileText, BookOpen, Piano, BarChart2, Settings, User, Moon, Sun, Menu, X, Search, Sparkles, AlertTriangle, CheckCircle, ChevronDown, Plus, Lock, Play } from 'lucide-react';

// Color System
const colors = {
  dark: {
    surfaceBase: '#0D0D0F',
    surfaceElevated: '#16161A',
    surfaceOverlay: '#1E1E24',
    surfaceEditor: '#121215',
    borderSubtle: '#2A2A32',
    borderDefault: '#3D3D47',
    textPrimary: '#EAEAEC',
    textSecondary: '#A0A0A8',
    textTertiary: '#6B6B75',
    accentPrimary: '#FF6B2C',
    accentHover: '#FF8A57',
    accentMuted: 'rgba(255, 107, 44, 0.15)',
    statusSuccess: '#34D399',
    statusWarning: '#FBBF24',
    statusError: '#F87171',
    statusInfo: '#60A5FA',
  },
  light: {
    surfaceBase: '#FAFAFA',
    surfaceElevated: '#FFFFFF',
    surfaceOverlay: '#F5F5F5',
    surfaceEditor: '#FFFFFF',
    borderSubtle: '#E5E5E5',
    borderDefault: '#D4D4D4',
    textPrimary: '#18181B',
    textSecondary: '#52525B',
    textTertiary: '#A1A1AA',
    accentPrimary: '#EA580C',
    accentHover: '#F97316',
    accentMuted: 'rgba(234, 88, 12, 0.1)',
    statusSuccess: '#059669',
    statusWarning: '#D97706',
    statusError: '#DC2626',
    statusInfo: '#2563EB',
  }
};

// Sample document content with style issues
const sampleContent = `# API Reference

This section documents the REST API endpoints for the Auth module.

## Authentication

All requests must include the \`Bearer\` token in the Authorization header.

\`\`\`http
GET /api/v2/users
Authorization: Bearer {token}
\`\`\`

Users on the blacklist will receive a 403 Forbidden response. The whitelist feature allows trusted IPs to bypass rate limiting.

## Endpoints

### GET /users

Returns a list of all users in the system. The administrator should configure appropriate permissions before deploying to production.`;

const styleIssues = [
  { line: 12, word: 'blacklist', suggestion: 'blocklist', type: 'error', rule: 'TERM-001' },
  { line: 12, word: 'whitelist', suggestion: 'allowlist', type: 'error', rule: 'TERM-001' },
  { line: 16, word: 'administrator', suggestion: 'admin', type: 'warning', rule: 'TERM-002' },
];

// Navigation Item Component
const NavItem = ({ icon: Icon, label, active, onClick, locked }) => {
  const [theme] = useState('dark');
  const c = colors[theme];

  return (
    <button
      onClick={onClick}
      className="relative w-full flex flex-col items-center py-3 px-2 transition-all duration-200 group"
      style={{
        background: active ? c.accentMuted : 'transparent',
        borderLeft: active ? `3px solid ${c.accentPrimary}` : '3px solid transparent',
      }}
      title={label}
    >
      <Icon
        size={22}
        style={{ color: active ? c.accentPrimary : c.textSecondary }}
        className="transition-colors group-hover:text-white"
      />
      {locked && (
        <Lock size={10} className="absolute top-2 right-2" style={{ color: c.textTertiary }} />
      )}
    </button>
  );
};

// Style Score Gauge
const StyleGauge = ({ score, theme }) => {
  const c = colors[theme];
  const getScoreColor = (s) => {
    if (s >= 90) return c.statusSuccess;
    if (s >= 70) return c.statusWarning;
    return c.statusError;
  };

  const getScoreLabel = (s) => {
    if (s >= 90) return 'In Harmony';
    if (s >= 70) return 'Minor Dissonance';
    return 'Needs Tuning';
  };

  return (
    <div className="text-center p-4 rounded-lg" style={{ background: c.surfaceOverlay }}>
      <div className="text-4xl font-bold mb-2" style={{ color: getScoreColor(score) }}>
        {score}%
      </div>
      <div className="w-full h-2 rounded-full mb-2" style={{ background: c.borderSubtle }}>
        <div
          className="h-full rounded-full transition-all duration-500"
          style={{
            width: `${score}%`,
            background: getScoreColor(score)
          }}
        />
      </div>
      <div className="text-sm" style={{ color: c.textSecondary }}>
        {getScoreLabel(score)}
      </div>
    </div>
  );
};

// Issue Card Component
const IssueCard = ({ issue, theme, onApply }) => {
  const c = colors[theme];
  const isError = issue.type === 'error';

  return (
    <div
      className="p-3 rounded-lg mb-2 cursor-pointer hover:opacity-80 transition-opacity"
      style={{
        background: c.surfaceOverlay,
        borderLeft: `3px solid ${isError ? c.statusError : c.statusWarning}`
      }}
    >
      <div className="flex items-center gap-2 mb-1">
        <AlertTriangle size={14} style={{ color: isError ? c.statusError : c.statusWarning }} />
        <span className="text-xs font-mono" style={{ color: c.textTertiary }}>Line {issue.line}</span>
      </div>
      <div className="text-sm mb-1" style={{ color: c.textPrimary }}>
        "<span style={{ textDecoration: 'line-through', color: c.statusError }}>{issue.word}</span>" →
        <span style={{ color: c.statusSuccess }}> {issue.suggestion}</span>
      </div>
      <button
        onClick={() => onApply(issue)}
        className="text-xs px-2 py-1 rounded transition-colors"
        style={{
          background: c.accentMuted,
          color: c.accentPrimary
        }}
      >
        Apply Fix
      </button>
    </div>
  );
};

// Agent Card Component
const AgentCard = ({ name, description, icon, license, available, theme, onStart }) => {
  const c = colors[theme];

  return (
    <div
      className="p-4 rounded-lg transition-all duration-200"
      style={{
        background: c.surfaceOverlay,
        border: `1px solid ${c.borderSubtle}`,
        opacity: available ? 1 : 0.7
      }}
    >
      <div className="flex items-center gap-2 mb-2">
        <span className="text-xl">{icon}</span>
        <span className="font-semibold" style={{ color: c.textPrimary }}>{name}</span>
      </div>
      <p className="text-sm mb-3" style={{ color: c.textSecondary }}>
        {description}
      </p>
      <div className="flex items-center justify-between">
        <span className="text-xs px-2 py-1 rounded" style={{
          background: c.surfaceElevated,
          color: c.textTertiary
        }}>
          {license}
        </span>
        {available ? (
          <button
            onClick={onStart}
            className="flex items-center gap-1 text-sm px-3 py-1.5 rounded transition-colors"
            style={{ background: c.accentPrimary, color: '#fff' }}
          >
            <Play size={14} /> Start
          </button>
        ) : (
          <button
            className="flex items-center gap-1 text-sm px-3 py-1.5 rounded"
            style={{ background: c.borderSubtle, color: c.textTertiary }}
          >
            <Lock size={14} /> Upgrade
          </button>
        )}
      </div>
    </div>
  );
};

// Main App Component
export default function LexichordPrototype() {
  const [theme, setTheme] = useState('dark');
  const [activeNav, setActiveNav] = useState('editor');
  const [showInspector, setShowInspector] = useState(true);
  const [styleScore, setStyleScore] = useState(82);
  const [content, setContent] = useState(sampleContent);
  const [issues, setIssues] = useState(styleIssues);
  const [agentChat, setAgentChat] = useState([]);
  const [chatInput, setChatInput] = useState('');

  const c = colors[theme];

  // Simulate fixing an issue
  const handleApplyFix = (issue) => {
    setContent(prev => prev.replace(issue.word, issue.suggestion));
    setIssues(prev => prev.filter(i => i.word !== issue.word));
    setStyleScore(prev => Math.min(100, prev + 6));
  };

  // Simulate AI chat
  const handleSendMessage = () => {
    if (!chatInput.trim()) return;

    setAgentChat(prev => [...prev, { role: 'user', content: chatInput }]);
    setChatInput('');

    setTimeout(() => {
      setAgentChat(prev => [...prev, {
        role: 'assistant',
        content: "I've analyzed your document. Here are my suggestions:\n\n1. The introduction could be more concise\n2. Consider adding code examples\n3. The terminology section needs updating"
      }]);
    }, 1000);
  };

  // Highlight words with issues
  const renderHighlightedContent = () => {
    let highlighted = content;
    issues.forEach(issue => {
      const color = issue.type === 'error' ? c.statusError : c.statusWarning;
      highlighted = highlighted.replace(
        new RegExp(`\\b${issue.word}\\b`, 'g'),
        `<span style="text-decoration: wavy underline ${color}; text-decoration-thickness: 2px;">${issue.word}</span>`
      );
    });
    return highlighted;
  };

  return (
    <div className="h-screen flex flex-col" style={{ background: c.surfaceBase, color: c.textPrimary }}>
      {/* Title Bar */}
      <header
        className="h-10 flex items-center justify-between px-4 border-b"
        style={{ background: c.surfaceElevated, borderColor: c.borderSubtle }}
      >
        <div className="flex items-center gap-3">
          <Menu size={18} style={{ color: c.textSecondary }} className="cursor-pointer" />
          <span className="font-semibold tracking-wide" style={{ color: c.accentPrimary }}>
            LEXICHORD
          </span>
          <span className="text-xs px-2 py-0.5 rounded" style={{ background: c.accentMuted, color: c.accentPrimary }}>
            PROTOTYPE
          </span>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setTheme(t => t === 'dark' ? 'light' : 'dark')}
            className="p-1.5 rounded transition-colors"
            style={{ background: c.surfaceOverlay }}
          >
            {theme === 'dark' ? <Sun size={16} style={{ color: c.textSecondary }} /> : <Moon size={16} style={{ color: c.textSecondary }} />}
          </button>
          <Settings size={18} style={{ color: c.textSecondary }} className="cursor-pointer" />
          <div className="flex items-center gap-2 px-2 py-1 rounded" style={{ background: c.surfaceOverlay }}>
            <User size={16} style={{ color: c.textSecondary }} />
            <span className="text-sm" style={{ color: c.textSecondary }}>Ryan</span>
            <ChevronDown size={14} style={{ color: c.textTertiary }} />
          </div>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden">
        {/* Navigation Rail */}
        <nav
          className="w-14 flex flex-col border-r"
          style={{ background: c.surfaceElevated, borderColor: c.borderSubtle }}
        >
          <div className="flex-1">
            <NavItem icon={FileText} label="Editor" active={activeNav === 'editor'} onClick={() => setActiveNav('editor')} />
            <NavItem icon={BookOpen} label="Knowledge" active={activeNav === 'knowledge'} onClick={() => setActiveNav('knowledge')} />
            <NavItem icon={Piano} label="Agents" active={activeNav === 'agents'} onClick={() => setActiveNav('agents')} />
            <NavItem icon={BarChart2} label="Analytics" active={activeNav === 'analytics'} onClick={() => setActiveNav('analytics')} />
          </div>
          <div className="border-t" style={{ borderColor: c.borderSubtle }}>
            <NavItem icon={Settings} label="Settings" active={activeNav === 'settings'} onClick={() => setActiveNav('settings')} />
            <NavItem icon={User} label="Profile" active={activeNav === 'profile'} onClick={() => setActiveNav('profile')} />
          </div>
        </nav>

        {/* Main Content Area */}
        <main className="flex-1 flex flex-col overflow-hidden">
          {/* Tab Bar */}
          <div
            className="h-9 flex items-center px-2 border-b"
            style={{ background: c.surfaceElevated, borderColor: c.borderSubtle }}
          >
            <div className="flex items-center gap-1 px-3 py-1 rounded text-sm" style={{ background: c.surfaceOverlay }}>
              <span>📁 My Project</span>
              <ChevronDown size={14} style={{ color: c.textTertiary }} />
            </div>
            <div className="flex items-center ml-4">
              <div
                className="flex items-center gap-2 px-3 py-1 rounded-t text-sm"
                style={{
                  background: c.surfaceBase,
                  borderBottom: `2px solid ${c.accentPrimary}`
                }}
              >
                <FileText size={14} style={{ color: c.accentPrimary }} />
                <span>api-reference.md</span>
                <X size={14} style={{ color: c.textTertiary }} className="cursor-pointer hover:opacity-70" />
              </div>
              <div
                className="flex items-center gap-2 px-3 py-1 text-sm cursor-pointer"
                style={{ color: c.textSecondary }}
              >
                <FileText size={14} />
                <span>intro.md</span>
                <X size={14} style={{ color: c.textTertiary }} />
              </div>
            </div>
            <button className="ml-2 p-1 rounded hover:bg-white/5">
              <Plus size={16} style={{ color: c.textTertiary }} />
            </button>
          </div>

          {/* Content + Inspector */}
          <div className="flex-1 flex overflow-hidden">
            {/* Editor/Content Panel */}
            <div className="flex-1 overflow-auto p-6" style={{ background: c.surfaceEditor }}>
              {activeNav === 'editor' && (
                <div
                  className="max-w-3xl mx-auto font-mono text-sm leading-relaxed"
                  style={{ color: c.textPrimary }}
                  dangerouslySetInnerHTML={{ __html: renderHighlightedContent().replace(/\n/g, '<br/>').replace(/```(\w+)?\n([\s\S]*?)```/g, '<pre style="background: ' + c.surfaceOverlay + '; padding: 12px; border-radius: 6px; margin: 8px 0; overflow-x: auto;">$2</pre>').replace(/`([^`]+)`/g, '<code style="background: ' + c.surfaceOverlay + '; padding: 2px 6px; border-radius: 4px;">$1</code>').replace(/^# (.+)$/gm, '<h1 style="font-size: 1.5rem; font-weight: bold; margin: 16px 0 8px; font-family: Inter, sans-serif;">$1</h1>').replace(/^## (.+)$/gm, '<h2 style="font-size: 1.25rem; font-weight: 600; margin: 16px 0 8px; font-family: Inter, sans-serif;">$1</h2>').replace(/^### (.+)$/gm, '<h3 style="font-size: 1.1rem; font-weight: 600; margin: 12px 0 6px; font-family: Inter, sans-serif;">$1</h3>') }}
                />
              )}

              {activeNav === 'knowledge' && (
                <div className="max-w-4xl mx-auto">
                  <h1 className="text-2xl font-bold mb-6">Knowledge Hub</h1>
                  <div
                    className="flex items-center gap-3 p-3 rounded-lg mb-6"
                    style={{ background: c.surfaceOverlay, border: `1px solid ${c.borderSubtle}` }}
                  >
                    <Search size={20} style={{ color: c.textTertiary }} />
                    <input
                      type="text"
                      placeholder="Search your knowledge base..."
                      className="flex-1 bg-transparent outline-none"
                      style={{ color: c.textPrimary }}
                    />
                    <kbd className="px-2 py-1 text-xs rounded" style={{ background: c.surfaceElevated, color: c.textTertiary }}>⌘K</kbd>
                  </div>

                  <div className="grid grid-cols-2 gap-6">
                    <div>
                      <h2 className="text-lg font-semibold mb-4" style={{ color: c.textSecondary }}>Sources</h2>
                      {[
                        { name: 'Codebase', files: 142, status: 'indexed' },
                        { name: 'Documentation', files: 38, status: 'indexed' },
                        { name: 'API Specs', files: 3, status: 'indexing', progress: 67 },
                      ].map((source, i) => (
                        <div
                          key={i}
                          className="p-4 rounded-lg mb-3"
                          style={{ background: c.surfaceOverlay }}
                        >
                          <div className="flex items-center gap-2 mb-2">
                            {source.status === 'indexed' ? (
                              <CheckCircle size={16} style={{ color: c.statusSuccess }} />
                            ) : (
                              <div className="w-4 h-4 rounded-full border-2 border-t-transparent animate-spin" style={{ borderColor: c.accentPrimary }} />
                            )}
                            <span className="font-medium">{source.name}</span>
                            <span className="text-xs" style={{ color: c.textTertiary }}>({source.files} files)</span>
                          </div>
                          {source.progress && (
                            <div className="w-full h-1.5 rounded-full" style={{ background: c.borderSubtle }}>
                              <div
                                className="h-full rounded-full"
                                style={{ width: `${source.progress}%`, background: c.accentPrimary }}
                              />
                            </div>
                          )}
                        </div>
                      ))}
                      <button
                        className="w-full p-3 rounded-lg border-2 border-dashed text-center transition-colors"
                        style={{ borderColor: c.borderSubtle, color: c.textSecondary }}
                      >
                        + Add Source
                      </button>
                    </div>

                    <div>
                      <h2 className="text-lg font-semibold mb-4" style={{ color: c.textSecondary }}>Recent Queries</h2>
                      <div className="space-y-3">
                        {['authentication flow', 'error handling', 'rate limiting'].map((query, i) => (
                          <div
                            key={i}
                            className="p-3 rounded-lg cursor-pointer hover:opacity-80"
                            style={{ background: c.surfaceOverlay }}
                          >
                            <div className="flex items-center gap-2">
                              <Search size={14} style={{ color: c.textTertiary }} />
                              <span>{query}</span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {activeNav === 'agents' && (
                <div className="max-w-4xl mx-auto">
                  <h1 className="text-2xl font-bold mb-6">The Ensemble</h1>
                  <p className="mb-6" style={{ color: c.textSecondary }}>
                    Specialized AI agents to assist with your writing workflow.
                  </p>

                  <div className="grid grid-cols-2 gap-4 mb-8">
                    <AgentCard
                      name="The Editor"
                      description="Refines prose for clarity, conciseness, and style compliance."
                      icon="🎭"
                      license="Writer Pro"
                      available={true}
                      theme={theme}
                      onStart={() => {}}
                    />
                    <AgentCard
                      name="The Simplifier"
                      description="Reduces complexity and eliminates jargon for broader audiences."
                      icon="✨"
                      license="Writer Pro"
                      available={true}
                      theme={theme}
                      onStart={() => {}}
                    />
                    <AgentCard
                      name="The Chronicler"
                      description="Generates changelogs and release notes from Git history."
                      icon="📜"
                      license="Teams"
                      available={false}
                      theme={theme}
                      onStart={() => {}}
                    />
                    <AgentCard
                      name="The Scribe"
                      description="Converts OpenAPI/Swagger specs into human-readable documentation."
                      icon="📖"
                      license="Teams"
                      available={false}
                      theme={theme}
                      onStart={() => {}}
                    />
                  </div>

                  {/* Agent Chat */}
                  <div
                    className="rounded-lg p-4"
                    style={{ background: c.surfaceOverlay, border: `1px solid ${c.borderSubtle}` }}
                  >
                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center gap-2">
                        <span className="text-xl">🎭</span>
                        <span className="font-semibold">Active Session: The Editor</span>
                      </div>
                      <button
                        className="text-sm px-3 py-1 rounded"
                        style={{ background: c.borderSubtle, color: c.textSecondary }}
                      >
                        End Session
                      </button>
                    </div>

                    <div className="h-48 overflow-y-auto mb-4 space-y-3">
                      {agentChat.length === 0 ? (
                        <div className="text-center py-8" style={{ color: c.textTertiary }}>
                          Start a conversation with The Editor...
                        </div>
                      ) : (
                        agentChat.map((msg, i) => (
                          <div
                            key={i}
                            className={`p-3 rounded-lg ${msg.role === 'user' ? 'ml-12' : 'mr-12'}`}
                            style={{
                              background: msg.role === 'user' ? c.accentMuted : c.surfaceElevated,
                              borderLeft: msg.role === 'assistant' ? `3px solid ${c.accentPrimary}` : 'none'
                            }}
                          >
                            <div className="text-xs mb-1" style={{ color: c.textTertiary }}>
                              {msg.role === 'user' ? 'You' : 'The Editor'}
                            </div>
                            <div className="whitespace-pre-wrap">{msg.content}</div>
                          </div>
                        ))
                      )}
                    </div>

                    <div className="flex gap-2">
                      <input
                        type="text"
                        value={chatInput}
                        onChange={(e) => setChatInput(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                        placeholder="Type your message..."
                        className="flex-1 px-4 py-2 rounded-lg outline-none"
                        style={{
                          background: c.surfaceElevated,
                          border: `1px solid ${c.borderSubtle}`,
                          color: c.textPrimary
                        }}
                      />
                      <button
                        onClick={handleSendMessage}
                        className="px-4 py-2 rounded-lg"
                        style={{ background: c.accentPrimary, color: '#fff' }}
                      >
                        Send
                      </button>
                    </div>
                  </div>
                </div>
              )}

              {activeNav === 'analytics' && (
                <div className="max-w-4xl mx-auto">
                  <h1 className="text-2xl font-bold mb-6">Style Dashboard</h1>

                  <div className="grid grid-cols-2 gap-6 mb-8">
                    <StyleGauge score={styleScore} theme={theme} />

                    <div className="p-4 rounded-lg" style={{ background: c.surfaceOverlay }}>
                      <h3 className="font-semibold mb-4">Voice Profile</h3>
                      {[
                        { label: 'Directness', value: 75, left: 'Soft', right: 'Assertive' },
                        { label: 'Formality', value: 60, left: 'Casual', right: 'Formal' },
                        { label: 'Complexity', value: 40, left: 'Simple', right: 'Technical' },
                      ].map((metric, i) => (
                        <div key={i} className="mb-4">
                          <div className="text-sm mb-1" style={{ color: c.textSecondary }}>{metric.label}</div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs" style={{ color: c.textTertiary }}>{metric.left}</span>
                            <div className="flex-1 h-2 rounded-full relative" style={{ background: c.borderSubtle }}>
                              <div
                                className="absolute w-3 h-3 rounded-full -top-0.5 transform -translate-x-1/2"
                                style={{ left: `${metric.value}%`, background: c.accentPrimary }}
                              />
                            </div>
                            <span className="text-xs" style={{ color: c.textTertiary }}>{metric.right}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div
                    className="p-4 rounded-lg"
                    style={{ background: c.surfaceOverlay }}
                  >
                    <h3 className="font-semibold mb-4">Issues by Category</h3>
                    {[
                      { label: 'Terminology Violations', count: issues.filter(i => i.type === 'error').length, max: 10 },
                      { label: 'Style Warnings', count: issues.filter(i => i.type === 'warning').length, max: 10 },
                      { label: 'Readability', count: 0, max: 10 },
                    ].map((cat, i) => (
                      <div key={i} className="flex items-center gap-3 mb-3">
                        <div className="w-40 text-sm" style={{ color: c.textSecondary }}>{cat.label}</div>
                        <div className="flex-1 h-2 rounded-full" style={{ background: c.borderSubtle }}>
                          <div
                            className="h-full rounded-full"
                            style={{
                              width: `${(cat.count / cat.max) * 100}%`,
                              background: cat.count > 0 ? c.statusError : c.statusSuccess
                            }}
                          />
                        </div>
                        <div className="w-8 text-sm text-right" style={{ color: c.textSecondary }}>{cat.count}</div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {activeNav === 'settings' && (
                <div className="max-w-2xl mx-auto">
                  <h1 className="text-2xl font-bold mb-6">Settings</h1>

                  <div className="space-y-6">
                    <div
                      className="p-4 rounded-lg"
                      style={{ background: c.surfaceOverlay }}
                    >
                      <h3 className="font-semibold mb-4">Appearance</h3>

                      <div className="mb-4">
                        <label className="text-sm mb-2 block" style={{ color: c.textSecondary }}>Theme</label>
                        <div className="flex gap-2">
                          {['Light', 'Dark', 'System'].map((t) => (
                            <button
                              key={t}
                              onClick={() => setTheme(t.toLowerCase() === 'system' ? 'dark' : t.toLowerCase())}
                              className="px-4 py-2 rounded-lg transition-colors"
                              style={{
                                background: (theme === t.toLowerCase() || (t === 'Dark' && theme === 'dark')) ? c.accentMuted : c.surfaceElevated,
                                border: `1px solid ${(theme === t.toLowerCase() || (t === 'Dark' && theme === 'dark')) ? c.accentPrimary : c.borderSubtle}`,
                                color: (theme === t.toLowerCase() || (t === 'Dark' && theme === 'dark')) ? c.accentPrimary : c.textSecondary
                              }}
                            >
                              {t}
                            </button>
                          ))}
                        </div>
                      </div>

                      <div className="mb-4">
                        <label className="text-sm mb-2 block" style={{ color: c.textSecondary }}>Accent Color</label>
                        <div className="flex gap-2">
                          {['#FF6B2C', '#60A5FA', '#34D399', '#A78BFA', '#F87171'].map((color) => (
                            <button
                              key={color}
                              className="w-8 h-8 rounded-full border-2 transition-transform hover:scale-110"
                              style={{
                                background: color,
                                borderColor: color === '#FF6B2C' ? '#fff' : 'transparent'
                              }}
                            />
                          ))}
                        </div>
                      </div>
                    </div>

                    <div
                      className="p-4 rounded-lg"
                      style={{ background: c.surfaceOverlay }}
                    >
                      <h3 className="font-semibold mb-4">License</h3>
                      <div className="flex items-center justify-between">
                        <div>
                          <div className="font-medium">Writer Pro</div>
                          <div className="text-sm" style={{ color: c.textSecondary }}>Expires: Feb 26, 2026</div>
                        </div>
                        <button
                          className="px-4 py-2 rounded-lg"
                          style={{ background: c.accentPrimary, color: '#fff' }}
                        >
                          Manage
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Inspector Panel */}
            {showInspector && activeNav === 'editor' && (
              <aside
                className="w-80 border-l overflow-y-auto"
                style={{ background: c.surfaceElevated, borderColor: c.borderSubtle }}
              >
                <div className="p-4">
                  <div className="flex items-center justify-between mb-4">
                    <h2 className="font-semibold">Inspector</h2>
                    <button onClick={() => setShowInspector(false)}>
                      <X size={16} style={{ color: c.textTertiary }} />
                    </button>
                  </div>

                  <StyleGauge score={styleScore} theme={theme} />

                  <div className="mt-6">
                    <div className="flex items-center justify-between mb-3">
                      <h3 className="font-semibold text-sm" style={{ color: c.textSecondary }}>
                        Issues ({issues.length})
                      </h3>
                      {issues.length > 0 && (
                        <button
                          onClick={() => {
                            issues.forEach(handleApplyFix);
                          }}
                          className="text-xs px-2 py-1 rounded"
                          style={{ background: c.accentMuted, color: c.accentPrimary }}
                        >
                          Apply All
                        </button>
                      )}
                    </div>

                    {issues.length === 0 ? (
                      <div className="text-center py-8" style={{ color: c.textTertiary }}>
                        <CheckCircle size={32} className="mx-auto mb-2" style={{ color: c.statusSuccess }} />
                        <div>No issues found!</div>
                      </div>
                    ) : (
                      issues.map((issue, i) => (
                        <IssueCard key={i} issue={issue} theme={theme} onApply={handleApplyFix} />
                      ))
                    )}
                  </div>

                  <div className="mt-6 pt-6 border-t" style={{ borderColor: c.borderSubtle }}>
                    <h3 className="font-semibold text-sm mb-3" style={{ color: c.textSecondary }}>
                      Document Info
                    </h3>
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between">
                        <span style={{ color: c.textTertiary }}>Words</span>
                        <span>127</span>
                      </div>
                      <div className="flex justify-between">
                        <span style={{ color: c.textTertiary }}>Characters</span>
                        <span>847</span>
                      </div>
                      <div className="flex justify-between">
                        <span style={{ color: c.textTertiary }}>Reading time</span>
                        <span>~1 min</span>
                      </div>
                      <div className="flex justify-between">
                        <span style={{ color: c.textTertiary }}>Modified</span>
                        <span>Just now</span>
                      </div>
                    </div>
                  </div>
                </div>
              </aside>
            )}
          </div>

          {/* Status Bar */}
          <footer
            className="h-7 flex items-center justify-between px-4 text-xs border-t"
            style={{ background: c.surfaceElevated, borderColor: c.borderSubtle, color: c.textTertiary }}
          >
            <div className="flex items-center gap-4">
              <span>📊 127 words</span>
              <span>📏 ~1 min read</span>
              <span style={{ color: styleScore >= 90 ? c.statusSuccess : styleScore >= 70 ? c.statusWarning : c.statusError }}>
                🎯 Style: {styleScore}%
              </span>
            </div>
            <div className="flex items-center gap-4">
              <span className="flex items-center gap-1">
                <Sparkles size={12} style={{ color: c.statusSuccess }} />
                AI: Ready
              </span>
              <span>Ln 16, Col 24</span>
            </div>
          </footer>
        </main>
      </div>
    </div>
  );
}
