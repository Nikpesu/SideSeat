---
name: futuristic-ui-design
description: 'Use when: designing or implementing SideSeat frontend UI (Razor views, dashboards, list/details screens, CSS) with a futuristic but non-generic aesthetic that still follows the project theme. Prompts user for key design choices and asks for photos/assets when imagery is needed.'
argument-hint: 'e.g., redesign Voznja index in futuristic transit-control style'
user-invocable: true
---

# SideSeat Futuristic UI Design Skill

## What This Skill Does
Creates production-ready SideSeat UI with a distinctive futuristic identity while preserving existing MVC flows, navigation, and theme consistency.

## Mandatory Discovery Interview (Ask First)
Before writing code, ask the user **one question at a time** and lock decisions for:

1. Scope of pages/components (which controllers/views).
2. Futuristic direction (pick a strong concept, not generic "AI look").
3. Motion intensity (subtle, medium, high).
4. Visual density (spacious, balanced, data-dense).
5. Theme mode (light-first, dark-first, dual).
6. Imagery policy:
   - Ask whether the design should include photos/renders/illustrations.
   - If imagery is needed, ask the user to provide files/paths or approved sources.
   - If they do not provide assets, get explicit confirmation to proceed with pure CSS/vector styling only.

After this interview, summarize the chosen direction in concise bullets and then implement.

## SideSeat Theme Anchoring (Required)
- Reuse existing tokens from `src/SideSeat/wwwroot/css/site.css` (`--ss-primary`, `--ss-accent`, `--ss-secondary`, etc.).
- Keep SideSeat structure patterns: `.ss-shell`, `.ss-page`, `.ss-breadcrumb`, `.ss-table-shell`, `.ss-btn`, `.ss-detail-card`.
- Maintain full route-safe navigation and Razor Tag Helpers.
- Keep responsive behavior for approximately 360, 768, 1024, and 1440 widths.
- Maintain accessible contrast and visible focus states.

## Futuristic-But-Project-Coherent Rules
- Build a clear concept (example directions: transit command deck, holographic timetable, industrial mobility console).
- Keep futuristic details purposeful: layered panels, controlled glow, depth, information hierarchy, readable data surfaces.
- Avoid generic AI visuals:
  - no default purple-neon-on-black template
  - no cookie-cutter hero+cards layout reused without context
  - no stock "tech" look without relation to SideSeat transportation domain
- Typography must be intentional and project-appropriate (avoid bland defaults).

## Implementation Flow
1. Inspect target views and shared layout/navigation.
2. Define or extend CSS variables first, then component classes.
3. Implement pages with consistent headers, breadcrumbs, list/details patterns.
4. Add high-value interactions (load reveal, hover/focus states) without hurting clarity.
5. Ensure all critical links and actions remain obvious on mobile and desktop.

## Do Not
- Do not change repository/domain/controller logic unless explicitly requested.
- Do not break required Index ↔ Details navigation paths.
- Do not scatter fragile inline styles across multiple views.

## Completion Checklist
- Distinct futuristic identity is visible and non-generic.
- SideSeat theme tokens and patterns are respected.
- User-selected design choices are reflected in implementation.
- If imagery was requested, provided assets are integrated (or fallback was explicitly approved).

