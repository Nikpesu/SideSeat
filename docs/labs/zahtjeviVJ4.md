# Zahtjevi VJ4 (Lab 4) + Lokacije u projektu

Izvor zahtjeva: [lab-4/Lab4.md](/c:/Users/nikpe/Documents/GitHub/SideSeat/lab-4/Lab4.md)

## 1) Potpuno funkcionalne stranice za pregled, pretragu, unos, uređivanje i brisanje entiteta
- Uvjet: CRUD stranice moraju raditi gdje poslovna pravila dopuštaju.
- Lokacije:
- [GradController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/GradController.cs)
- [KorisnikController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/KorisnikController.cs)
- [VoziloController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/VoziloController.cs)
- [VoznjaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/VoznjaController.cs)
- [RezervacijaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/RezervacijaController.cs)
- [PlacanjeController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/PlacanjeController.cs)
- [OcjenaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/OcjenaController.cs)
- [Views](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views)

### 1.1 Svaka lista mora imati AJAX pretragu
- Uvjet: list stranice filtriraju bez full page refresh-a.
- Lokacije:
- [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (`initializeAjaxLists`)
- [Views/Shared/_Layout.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Shared/_Layout.cshtml) (učitavanje `site.js`)
- Primjeri listi s filterima: [Views/Voznja/Index.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Voznja/Index.cshtml), [Views/Rezervacija/Index.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Rezervacija/Index.cshtml), [Views/Placanje/Index.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Placanje/Index.cshtml)

### 1.2 CRUD endpointi moraju raditi ispravno
- Uvjet: endpointi ne smiju pucati i moraju vraćati očekivani rezultat.
- Lokacije:
- Controller akcije (`Index/Details/Create/Edit/Delete`) u svim gore navedenim controllerima.
- FK-safe brisanja: [VoznjaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/VoznjaController.cs), [RezervacijaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/RezervacijaController.cs)

## 2) Dropdown s autocomplete opcijom
- Uvjet: custom kontrola za pretragu povezanih podataka.
- Lokacije:
- Partial kontrola: [Views/Shared/_AutocompleteLookup.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Shared/_AutocompleteLookup.cshtml)
- View model: [AutocompleteLookupViewModel.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Models/ViewModels/AutocompleteLookupViewModel.cs)
- JS ponašanje: [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (`initializeAutocompleteFields`)

### 2.1 Autocomplete mora koristiti AJAX prema serveru
- Uvjet: asinkrono dohvaćanje rezultata.
- Lokacije:
- Frontend `fetch`: [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js)
- JSON endpointi:
- [VoznjaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/VoznjaController.cs) (`SearchCities`, `SearchDrivers`)
- [RezervacijaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/RezervacijaController.cs) (`SearchUsers`, `SearchRides`)
- [OcjenaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/OcjenaController.cs) (`SearchUsers`, `SearchReservations`)

## 3) Validacija (client + server)

### 3.1 Client side validacija na blur
- Uvjet: validacija se okida kad kontrola izgubi fokus.
- Lokacije:
- [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (`jQuery.validator.setDefaults`, `onfocusout`)
- Dodatna validacija custom kontrola: `ensureValidSelection` (autocomplete), `syncHidden` (datetime)

### 3.2 Server side validacija uvijek prisutna
- Uvjet: server mora validirati i kad klijent pošalje loše podatke.
- Lokacije:
- `ModelState.IsValid` provjere kroz kontrolere:
- [AuthController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/AuthController.cs)
- [KorisnikController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/KorisnikController.cs)
- [VoznjaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/VoznjaController.cs)
- [RezervacijaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/RezervacijaController.cs)
- [PlacanjeController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/PlacanjeController.cs)
- [OcjenaController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/OcjenaController.cs)

### 3.3 Validacijske poruke uklopljene u UI
- Uvjet: poruke su vidljive i stilski uklopljene.
- Lokacije:
- `asp-validation-for`, `validation-summary` u viewovima formi (npr. Create/Edit stranice)
- [Views/Shared/_ValidationScriptsPartial.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Shared/_ValidationScriptsPartial.cshtml)
- [site.css](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/css/site.css)

## 4) Napredno korištenje JavaScripta + animacije u službi aplikacije
- Uvjet: funkcionalne animacije i napredniji JS obrasci.
- Lokacije:
- [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js)
- `debounce` za pretrage
- DOM parser + partial replace
- focus/selection restore nakon AJAX osvježavanja
- `requestAnimationFrame` za pozicioniranje datepickera
- `ss-list-flash` klasa za vizualni feedback nakon refresh-a
- [site.css](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/css/site.css) (animacijski stilovi)

## 5) Datumska kontrola (datum + vrijeme)

### 5.1 Napravljena preko partial view-a
- Uvjet: reusable partial umjesto native input date kao glavne kontrole.
- Lokacije:
- [Views/Shared/_DateTimeInput.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Shared/_DateTimeInput.cshtml)
- [DateTimeInputViewModel.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Models/ViewModels/DateTimeInputViewModel.cs)

### 5.2 Primijenjena gdje se koristi datum
- Uvjet: koristi se na formama i filterima datuma.
- Lokacije:
- Primjena kroz viewove koji renderiraju datum filter/form polja (Home, Voznja, Rezervacija, Placanje, Ocjena i drugi relevantni viewovi)
- JS obrada svih instanci preko `[data-ss-datetime]`:
- [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (`initializeDateFields`)

### 5.3 Radi na hr+en formatu prema browser postavkama
- Uvjet: podržani hrvatski i engleski format.
- Lokacije:
- Parser/formatter: [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (`formatDateValue`, `parseDateValue`)
- Lokalizacija request-a: [Program.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Program.cs) (`UseRequestLocalization`, `hr-HR`, `en-US`)

### 5.4 Ne koristiti default browser datepicker
- Uvjet: JS/custom datepicker.
- Lokacije:
- [Views/Shared/_DateTimeInput.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Shared/_DateTimeInput.cshtml) (`type="text"` + custom picker)
- [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js) (render custom calendar popup)

## 6) Dodatno povezano (autentifikacija i status kodovi)
- Nije zaseban bod u tablici, ali je bitno za stabilan UX kroz aplikaciju.
- Lokacije:
- Login/register modal i flow: [AuthController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/AuthController.cs), [site.js](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/wwwroot/js/site.js)
- Cookie auth i redirecti: [Program.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Program.cs)
- HTTP status stranica: [HomeController.cs](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Controllers/HomeController.cs), [Views/Home/HttpStatus.cshtml](/c:/Users/nikpe/Documents/GitHub/SideSeat/src/SideSeat/Views/Home/HttpStatus.cshtml)
