# Lab 3 — EF / Routing

[← Pregled labosa](Labs.md) · Predaja: **8.5.2026.** · Izvor: `docs/labs/lab-3/`

## Cilj i kriteriji

| Kriterij | Bodovi |
|---|---|
| Prilagodba modela za EF (anotacije, veze), konfiguracija EF | 1 |
| Razumijevanje EF principa (usmeno) | 1 |
| Razumijevanje routing principa (usmeno) | 1 |
| Semantički i routing model (md file) pomoću AI | 1 |
| Izrada i korištenje skill-ova (EF / Routing / UX skill) | 1 |

Nužni uvjeti:
- Konfigurirati EF: anotacije i `virtual`/`ICollection<>` veze, baza (MSSQL/Docker),
  connection string, `DbContext` + DI.
- Prebaciti app s mock repozitorija na EF repozitorij + inicijalna migracija.
- Prilagođeni routing na **≥ 4 akcije** (default se ne broji).
- `semantic-model.md` i `sitemap.md`.
- Barem jedan skill (EF / list / edit-form).

## Implementacija u SideSeatu

**EF konfiguracija** — `src/SideSeat/Data/SideSeatDbContext.cs` definira `DbSet`-ove i veze
(`Fluent API`/anotacije), registriran u `Program.cs`:
`builder.Services.AddDbContext<SideSeatDbContext>(o => o.UseSqlServer(connectionString))`.
Baza je SQL Server (LocalDB lokalno, `sideseat-db` u Dockeru).

**EF repository** — `src/SideSeat/Repositories/SideSeatEfRepository.cs` zamijenio je mock
repozitorije; registriran kao scoped servis.

**Migracije** — `src/SideSeat/Migrations/` (inicijalna + seed migracije
`SeedData`, `ExpandSeedData`, `PersistSeedDataInDatabase`); primjenjuju se automatski na startu
(`dbContext.Database.MigrateAsync()` u `Program.cs`, s retry petljom za Docker).

**Prilagođeni routing** — u `Program.cs` definirano je više od 4 custom rute, npr.:
- `gradovi` → `Grad/Index`
- `gradovi/{id:int}` → `Grad/Details`
- `voznje/aktivne` → `Voznja/Active`
- `korisnici/{id:int}/profil` → `Korisnik/Details`

uz default rutu `{controller=Home}/{action=Index}/{id?}`.

**Semantički i routing model** — `docs/labs/lab-3/semantic-model.md` (klase, svojstva, veze) i
`docs/labs/lab-3/sitemap.md` (URL → controller/action/view).

**Skill-ovi** — `.github/skills/`: `entity-framework`, `list-page`, `edit-form`
(+ `futuristic-ui-design`, `version-control-dockerhub`).

## Za usmeno

- Što su navigacijska svojstva, `virtual`, lazy/eager loading (`Include`).
- Što radi migracija i kako EF mapira klase na tablice te 1-N/N-N veze.
- Kako se evaluiraju rute (redoslijed, `defaults`, `{id:int}` constraint) i zašto je
  nomenklatura `XyzController` bitna.
