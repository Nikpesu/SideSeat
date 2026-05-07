---
name: list-page
description: 'Use when: creating a list page in SideSeat, following the existing ss-table layout and repository pattern.'
argument-hint: 'e.g., add Voznja list page'
user-invocable: true
---

# SideSeat List Page Skill

## When to Use
- Add a new list page for an entity, following current UI style.
- Add Index actions and list views that render a table with filters.

## Procedure
1. Identify the entity and repository method to fetch a list.
2. Add or update Controller Index action to return the list.
3. Create or update Views/<Entity>/Index.cshtml to use the ss-table pattern.
4. Ensure the view uses the same breadcrumb and header pattern as existing lists.
5. Add navigation link in Views/Shared/_Layout.cshtml if needed.
6. Optionally add a custom route in Program.cs for a short URL.

## Voznja Example Requirements
- Use the existing Voznja list layout in Views/Voznja/Index.cshtml as the template.
- Include fields used in current UI: route, driver, times, seats, price, status.
