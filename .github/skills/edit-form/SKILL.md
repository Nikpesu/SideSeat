---
name: edit-form
description: 'Use when: adding create/edit forms in SideSeat with tag helpers, validation, and standard layout.'
argument-hint: 'e.g., create Voznja create/edit form'
user-invocable: true
---

# SideSeat Edit/Create Form Skill

## When to Use
- Add create/edit pages for an entity using form tag helpers.
- Add GET/POST actions and validate input before saving.

## Procedure
1. Create a view model with required fields and validation attributes.
2. Add GET action to load the form and populate select lists.
3. Add POST action to validate ModelState and save changes via DbContext.
4. Use a shared form partial if create and edit are similar.
5. Create Views/<Entity>/Create.cshtml and Edit.cshtml or a shared partial.
6. Use form tag helpers: asp-action, asp-for, asp-validation-for.
7. Add anti-forgery token and redirect on success.

## Voznja Example Requirements
- Inputs: Vozac, PolazniGrad, OdredisniGrad, Polazak, OcekivaniDolazak,
  CijenaPoMjestu, UkupnoMjesta, SlobodnaMjesta, Opis, Status.
- Use select lists for Vozac and Grad options.
- Follow the same page header and breadcrumb style as other views.
