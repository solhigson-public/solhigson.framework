---
name: Browser Automation Patterns
description: Chrome MCP screenshot workarounds, dev server lifecycle, and extension overlay handling for browser-automated QA
scope: elfrique
tier: matched
work_types:
  - qa
  - walkthrough
  - browser
agent_types:
  - qa
  - implementation
---

# Browser Automation Patterns

## Chrome CDP Screenshot Scroll Bug

`Page.captureScreenshot` via Chrome MCP does NOT follow scroll position. Screenshots always capture from the fixed top-of-page position regardless of `scrollY` value. This means `window.scrollTo()` and `element.scrollIntoView()` produce blank screenshots for below-fold content.

### Workaround: CSS translateY

Instead of scrolling, shift the page content upward using CSS `transform` on `document.documentElement`:

```js
// Capture a section at offset 534px from top
window.scrollTo(0, 0);
document.documentElement.style.transform = 'translateY(-534px)';
document.documentElement.style.overflow = 'visible';
// Take screenshot here — content at 534px is now in the viewport
```

### Two-Phase Sequence for Scroll + Screenshot

When a page requires both interaction (clicking elements) and screenshotting at various positions, use this two-phase sequence:

1. **Scroll for interaction**: Use `element.scrollIntoView()` to navigate to the target section. Interact with elements at this position (dropdowns, tabs, modals, form controls).
2. **Screenshot via translateY**: Before capturing, reset scroll to top (`window.scrollTo(0,0)`) and apply `translateY(-{offset}px)` to shift content into the capture area. Take screenshot. Reset transform (`document.documentElement.style.transform = ''`) before any subsequent click interactions — coordinates are wrong with transform active.

### Rules

- MUST NOT screenshot after `scrollIntoView()` — the capture will show blank/wrong content
- MUST NOT use `window.scrollTo()` for screenshot positioning — it does not affect the capture area
- MUST reset `transform` to `''` before any click interaction — transformed coordinates do not match visual positions
- MUST re-apply `translateY` before each screenshot at a new position

## Chrome Extension Overlay

The Claude in Chrome extension injects a `#claude-agent-glow-border` div into the page body. This element can consume significant vertical space and corrupt screenshots.

### Removal

Remove on every page navigation:

```js
document.getElementById('claude-agent-glow-border')?.remove();
```

The extension may re-inject the element. For persistent removal, install a MutationObserver:

```js
new MutationObserver((mutations) => {
  for (const m of mutations) {
    for (const n of m.addedNodes) {
      if (n.id === 'claude-agent-glow-border') n.remove();
    }
  }
}).observe(document.body, { childList: true });
```

Note: Even with DOM removal, the screenshot capture area may still be affected. The `translateY` workaround is the authoritative fix for scrolled screenshots.

## Dev Server Lifecycle

### Background Process Persistence

Agent-spawned background Bash processes die when the agent completes. Long-running servers (e.g., dev servers for QA) MUST be started by the session or agent that will use them for the duration of its execution. When the main session starts a server, use `run_in_background: true`. When a QA agent starts a server, the server lives for the agent's execution duration — no background flag needed since the agent blocks on browser interactions.

### Startup Sequence

1. Start server via Bash (main session or QA agent, depending on the execution model)
2. Wait 20-25 seconds for .NET build + startup
3. Verify with `netstat -an | grep "<port>.*LISTENING"` before navigating
4. If port is occupied from a previous server: find PID with `netstat -ano` and kill with `taskkill //PID <pid> //F`

### Common Errors

- `chrome-error://chromewebdata/` means connection refused OR cert error — check if server is actually running before assuming cert issues
- NEVER navigate to the URL before confirming the server is listening — Chrome caches "connection refused" error pages and requires a fresh tab or hard reload to recover
