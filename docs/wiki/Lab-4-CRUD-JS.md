# Lab 4 — CRUD forma, rich JS, dropdown

[← Pregled labosa](Labs.md) · Predaja: **22.5.2026.** · Izvor: `docs/labs/lab-4/`

## Cilj i kriteriji

| Kriterij | Bodovi |
|---|---|
| Kompletno funkcionalan CRUD za sve entitete | 2 |
| Padajući izbornik s AJAX autocomplete pretragom | 2 |
| Validacija (client + server side) | 1 |
| Napredno korištenje JavaScripta | 1 |
| Datumska kontrola (partial view) | 1 |

Nužni uvjeti:
- Pregled, pretraga, unos, uređivanje i brisanje gdje poslovna pravila dopuštaju; svaka lista
  ima AJAX pretragu; CRUD mora raditi bez grešaka.
- Custom dropdown s AJAX autocomplete za povezane podatke (gradovi, korisnici…).
- Client-side validacija (na gubitak fokusa) + obavezna server-side validacija.
- Animacije u službi aplikacije (napredni JS).
- Vlastita datum+vrijeme kontrola kao partial view, hr+en format, bez default browser datepickera.

## Implementacija u SideSeatu

**CRUD za sve entitete** — MVC kontroleri s `Index`/`Details`/`Create`/`Edit`/`Delete`:
`GradController`, `VoziloController`, `VoznjaController`, `RezervacijaController`,
`KorisnikController`, `OcjenaController`, `PlacanjeController`. Operacije idu kroz
`SideSeatEfRepository`. Ispravnost je pokrivena testovima (`MvcCrudTests`, `ApiCrudTests`,
`SqlServerConstraintTests`).

**AJAX autocomplete dropdown** — custom kontrola s asinkronim dohvatom; geokodiranje gradova ide
preko `Controllers/Api/MapsApiController.cs` i servisa `NominatimCityGeocodingService`, a logika
autocompletea je u `wwwroot/js/site.js`. Globalna pretraga (command palette) dodatno koristi
`SearchApiController`.

**Validacija** — server-side preko Data Annotations na form/view modelima (`Models/Forms`,
`Models/ViewModels`) i `ModelState.IsValid` u akcijama; client-side preko unobtrusive validacije
koja se okida na gubitak fokusa. Poruke su stilizirane u skladu sa sučeljem.

**Napredni JavaScript** — `wwwroot/js/`:
- `route-background.js` — animirana karta Hrvatske s autićima koji voze stvarnim rutama i
  „kamerom" koja prati auto (CSS zoom/pan bez ponovnog učitavanja pločica),
- `route-maps.js` — interaktivne karte ruta (Leaflet),
- `site.js` — autocomplete, command palette, modali, AI chat render, tema.

**Datumska kontrola** — vlastiti partial view za datum+vrijeme (bez native datepickera);
lokalizacija hr-HR/en-US podešena u `Program.cs` (`UseRequestLocalization`).

## Za usmeno

- Razlika Create vs Edit (insert vs update), i strategije Delete (cascade / set null / zabrana)
  te soft-delete.
- Zašto server-side validacija mora postojati i kad imamo client-side.
- Kako AJAX autocomplete radi (debounce, poziv na API, render rezultata) — vidi `site.js`.
- Zašto vlastita datumska kontrola umjesto browser default kontrole.
