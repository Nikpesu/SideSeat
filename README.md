# 🚗 SideSeat - Projekt u izradi

**SideSeat** je planirani sustav za dijeljenje prijevoza (carpooling) koji se razvija kao seminarski rad za kolegij **Razvoj web aplikacija u ASP.NET MVC tehnologiji**. 

> [!IMPORTANT]
> **Status projekta:** U fazi planiranja i dizajna arhitekture. 🛠️

## 📋 Planirani moduli i funkcionalnosti

Projekt će se razvijati u fazama, prateći životni ciklus razvoja softvera:

### 1. Faza: Modeliranje podataka (Entity Framework Core)
* Definiranje entiteta: `Korisnik`, `Vožnja`, `Rezervacija`, `Grad`.
* Postavljanje relacija (jedan-na-mnogo između vozača i vožnji, više-na-više za rezervacije).
* Implementacija **Code First** migracija.

### 2. Faza: Autentifikacija i autorizacija
* Korištenje **ASP.NET Core Identity** sustava.
* Razdvajanje uloga: 
    * **Vozač:** Može kreirati, uređivati i brisati svoje vožnje.
    * **Putnik:** Može pregledavati i rezervirati dostupna mjesta.
    * **Admin:** Nadzor nad svim korisnicima i aktivnostima.

### 3. Faza: Korisničko sučelje (Razor Views)
* Implementacija **Bootstrap 5** predložaka za responzivan dizajn.
* Izrada formi s **Data Annotation** validacijom (npr. provjera da datum polaska nije u prošlosti).
* Dinamička pretraga vožnji pomoću LINQ upita.

### 4. Faza: Dodatne funkcionalnosti (Opcionalno)
* Sustav ocjenjivanja vozača (Rating system).
* Integracija Google Maps API-ja za prikaz rute.
* Slanje email obavijesti o potvrdi rezervacije.

---

## 🏗️ Tehnološki stog (Tech Stack)
* **Backend:** C# / .NET 8 / ASP.NET MVC Core
* **ORM:** Entity Framework Core
* **Baza:** MS SQL Server
* **Frontend:** HTML5, CSS3, Bootstrap, JavaScript/jQuery

## 📅 Trenutni napredak
- [x] Osmišljavanje koncepta i naziva (**SideSeat**)
- [ ] Izrada dijagrama baze podataka
- [ ] Postavljanje osnovne strukture projekta u Visual Studiju
- [ ] Implementacija modela i baze podataka
