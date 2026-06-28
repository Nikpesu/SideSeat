# Vodič za Lab 3 - Entity Framework i Routing

Ovaj dokument služi kao priprema za usmeno ispitivanje i razumijevanje svega što je implementirano u sklopu Lab 3 na projektu **SideSeat**.

---

## 1. Entity Framework (EF) Core
**Što je EF?**
EF je ORM (Object-Relational Mapper) koji nam omogućuje da s bazom podataka radimo koristeći C# objekte, umjesto da ručno pišemo SQL upite.

### Ključni pojmovi:
- **DbContext (`SideSeatDbContext`):** Glavna klasa koja predstavlja sesiju s bazom podataka. Preko nje dohvaćamo (`DbSet`) i spremamo podatke.
	- Referenca (klasa i konfiguracija): [src/SideSeat/Data/SideSeatDbContext.cs](src/SideSeat/Data/SideSeatDbContext.cs#L6-L28)
- **DbSet:** Predstavlja tablicu u bazi. Npr. `public DbSet<Grad> Gradovi { get; set; }`.
- **LINQ:** Jezik upita koji koristimo za filtriranje podataka (npr. `_db.Voznje.Where(v => v.Status == StatusVoznje.Planirana).ToList()`).

### Prilagodba modela (Anotacije):
Da bi EF znao kako mapirati klase u tablice, koristimo:
- `[Key]`: Označava primarni ključ (PK).
- `[ForeignKey("Ime")]`: Označava vanjski ključ (FK) koji povezuje dvije tablice.
- `virtual`: Omogućuje **Lazy Loading** (učitavanje povezanih podataka po potrebi).
- `ICollection<T>`: Koristi se za 1-N veze (npr. jedan Grad ima kolekciju Vožnji).

---

## 2. Migracije i Baza Podataka
Migracije su način na koji pratimo promjene u kodu i primjenjujemo ih na strukturu baze podataka.

**Naredbe koje trebaš znati:**
1. `dotnet ef migrations add Naziv`: Kreira novu skriptu s promjenama.
2. `dotnet ef database update`: Primjenjuje te skripte na pravu bazu.

**Seed Data:**
U `SideSeatDbContext.cs` koristimo metodu `OnModelCreating` kako bismo unijeli početne podatke (gradove, testne korisnike) tako da aplikacija ne bude prazna nakon instalacije.
Referenca za connection string: [src/SideSeat/appsettings.json](src/SideSeat/appsettings.json#L9)

---

## 3. Repozitorij obrazac (Repository Pattern)
Umjesto da kontroler direktno priča s `DbContext`-om, koristimo **Repozitorij** (`SideSeatEfRepository`).

Primjer implementacije repozitorija: [src/SideSeat/Repositories/SideSeatEfRepository.cs](src/SideSeat/Repositories/SideSeatEfRepository.cs#L10)

**Zašto?**
- **Odvajanje logike:** Kontroler brine o prikazu, a repozitorij o podacima.
- **Lakše testiranje:** Možemo lako zamijeniti EF repozitorij s Mock repozitorijem.
- **Ponovna upotreba:** Isti upit (npr. dohvat vožnje s uključenim gradovima) možemo koristiti na više mjesta.

---

## 4. Routing (Usmjeravanje)
Routing određuje kako se URL koji upišeš u browser pretvara u poziv akcije u kontroleru.

**Defaultna ruta:** `{controller=Home}/{action=Index}/{id?}`
Primjer: `/Grad/Details/5` poziva `GradController`, metodu `Details` s parametrom `id=5`.

**Custom rute (u `Program.cs`):**
Dodali smo ljepše URL-ove:
- `/gradovi` -> šalje na `Grad/Index`.
- `/korisnici/1/profil` -> šalje na `Korisnik/Details/1`.

To radimo pomoću `app.MapControllerRoute` prije defaultne rute (redoslijed je bitan!).
Implementacija ruta i registracija `DbContext` nalaze se u: [src/SideSeat/Program.cs](src/SideSeat/Program.cs#L15-L16). Dodatna mapa ruta: [lab-3/sitemap.md](lab-3/sitemap.md)

---

## 5. Semantički Modeli
U mapi `lab-3` izradili smo:
- **`semantic-model.md`**: Opisuje "što je što" u bazi (koje su tablice i kako su povezane). Važno za razumijevanje poslovne logike.
- **`sitemap.md`**: Mapa svih stranica. Govori nam koji URL vodi do kojeg kontrolera i koji View se prikazuje.
Datoteke: [lab-3/semantic-model.md](lab-3/semantic-model.md), [lab-3/sitemap.md](lab-3/sitemap.md)

---

## 6. Skill-ovi (AI agenti)
Skill-ovi su upute za AI (poput mene) kako da ti pomogne u specifičnim zadacima.
- **EF Skill:** Pomaže mi da ispravno dodam nove tablice ili migracije.
- **Edit Form Skill:** Pomaže mi da brzo napravim formu za unos podataka (kao što smo napravili za `Voznja/Create`).

Skillovi i primjeri: [.github/skills/entity-framework/SKILL.md](.github/skills/entity-framework/SKILL.md), [.github/skills/list-page/SKILL.md](.github/skills/list-page/SKILL.md), [.github/skills/edit-form/SKILL.md](.github/skills/edit-form/SKILL.md)

---

## 7. Partial Views & Form Helpers
- **Partial View (`_Form.cshtml`):** Mali komad HTML-a koji možemo ubaciti na više mjesta. Koristimo ga da ista forma služi i za **Create** i za **Edit**.
- **Tag Helpers (`asp-for`, `asp-items`):** Specijalni atributi koji povezuju HTML elemente s tvojim C# modelom (npr. padajući izbornik s gradovima).

Primjeri view-eva: [src/SideSeat/Views/Voznja/Create.cshtml](src/SideSeat/Views/Voznja/Create.cshtml), [src/SideSeat/Views/Voznja/Edit.cshtml](src/SideSeat/Views/Voznja/Edit.cshtml), [src/SideSeat/Views/Voznja/Active.cshtml](src/SideSeat/Views/Voznja/Active.cshtml)

---

## Što te mogu pitati na usmenom?
1. Što je `DbContext` i čemu služi?
   Odgovor: `DbContext` je glavna EF Core klasa koja predstavlja sesiju s bazom i preko koje dohvaćamo, pratimo i spremamo podatke.
2. Kako si povezao `Voznju` i `Grad` u bazi?
   Odgovor: Preko `PolazniGradId` kao foreign key-a i navigacijskog svojstva na entitetu `Grad`.
3. Što radi `Include()` u upitu?
   Odgovor: Učitava povezane podatke unaprijed, primjerice uz vožnju i povezane gradove ili korisnike.
4. Gdje se definiraju rute u aplikaciji?
   Odgovor: U `Program.cs`, kroz `MapControllerRoute` i defaultnu rutu.
5. Koja je razlika između `Add` i `SaveChanges` kod EF-a?
   Odgovor: `Add` dodaje objekt u kontekst, a `SaveChanges` tek zapisuje promjene u bazu.
6. Što je `DbSet` i kako ga koristiš u `DbContext`-u?
   Odgovor: `DbSet` predstavlja tablicu u bazi i koristi se za rad s konkretnim entitetima.
7. Zašto si koristio `virtual` na navigacijskim svojstvima?
   Odgovor: Da EF može po potrebi raditi lazy loading i pravilno pratiti relacije.
8. Što je razlika između 1-1 i 1-N veze u modelu?
   Odgovor: U 1-1 vezi jedan zapis ima točno jedan povezani zapis, a u 1-N jedan zapis ima više povezanih zapisa.
9. Zašto je trebalo dodati `DeleteBehavior.Restrict` u migraciji?
   Odgovor: Da se izbjegnu multiple cascade path problemi pri brisanju povezanih podataka.
10. Što radi `AsNoTracking()` i kada ga koristiš?
	Odgovor: Dohvaća podatke bez praćenja promjena, pa je brže za read-only upite.
11. Zašto `SideSeatEfRepository` koristi `Include()` i `ThenInclude()`?
	Odgovor: Da se uz glavni entitet odmah učitaju i povezani podaci koje prikaz treba.
12. Kako funkcionira custom ruta poput `/gradovi` ili `/korisnici/{id}/profil`?
	Odgovor: Ruta mapira čitljiv URL na konkretnu akciju kontrolera, npr. `Grad/Index` ili `Korisnik/Details`.
13. Koja je razlika između `MapControllerRoute` i defaultne rute?
	Odgovor: `MapControllerRoute` definira posebne rute koje se provjeravaju prije defaultne rute.
14. Zašto si izradio `semantic-model.md` i `sitemap.md`?
	Odgovor: Da dokumentiram strukturu baze i mapu stranica, što pomaže pri obrani i razumijevanju aplikacije.
15. Što si točno dodao u `.github/skills` i čemu to služi?
	Odgovor: Dodani su EF, list page i edit form skillovi kao pomoćne upute za brži i točniji rad.
16. Kako `Voznja/Create` i `Voznja/Edit` koriste isti obrazac za unos podataka?
	Odgovor: Dijele isti partial view i iste form helpers, pa se isti obrazac koristi za unos i izmjenu.
17. Što je repository pattern i zašto je bolji od direktnog rada u kontroleru?
	Odgovor: To je sloj koji odvaja pristup podacima od kontrolera i olakšava održavanje i testiranje.
18. Kako bi objasnio razliku između Lazy Loading i Eager Loading?
	Odgovor: Lazy loading učitava podatke tek kad su zatraženi, a eager loading ih učita odmah kroz `Include()`.
19. Što bi se dogodilo da nemaš pravilno podešen foreign key između tablica?
	Odgovor: Relacije bi bile neispravne, EF bi mogao raditi pogrešne veze, a migracije ili upiti bi mogli padati.
20. Koji je redoslijed koraka kod dodavanja nove entitetske klase u EF projektu?
	Odgovor: Dodati entitet, podesiti anotacije i navigacije, dodati `DbSet`, po potrebi prilagoditi `OnModelCreating`, zatim napraviti migraciju i update baze.

Sretno na obrani vježbe! 🚀

---

## Checklist za predaju
- [x] Konfiguriran EF u projektu
- [x] Model prilagođen za EF anotacijama i vezama
- [x] Podeseni `virtual` i `ICollection<>` odnosi
- [x] Podesen connection string i baza podataka
- [x] Dodan `DbContext` i potrebni DI
- [x] Prebacena aplikacija s mock repositoryja na EF repository
- [x] Generirana inicijalna migracija
- [x] Podeseno custom routing za vise akcija controller-a
- [x] Izradjen `semantic-model.md`
- [x] Izradjen `sitemap.md`
- [x] Konfigurirani skillovi u `.github/skills`
- [x] Pripremljene teme za usmeno: EF i routing

---

Referencije su premještene u relevantne sekcije iznad.
