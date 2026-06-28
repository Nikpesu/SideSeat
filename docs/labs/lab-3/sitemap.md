# SideSeat sitemap

## Pocetna i osnovne stranice
- / -> HomeController.Index -> Views/Home/Index.cshtml
- /Home/Index -> HomeController.Index -> Views/Home/Index.cshtml
- /Home/Privacy -> HomeController.Privacy -> Views/Home/Privacy.cshtml

## Gradovi
- /Grad/Index -> GradController.Index -> Views/Grad/Index.cshtml
- /Grad/Details/{id} -> GradController.Details -> Views/Grad/Details.cshtml
- /gradovi -> GradController.Index -> Views/Grad/Index.cshtml (custom)
- /gradovi/{id} -> GradController.Details -> Views/Grad/Details.cshtml (custom)

## Korisnici
- /Korisnik/Index -> KorisnikController.Index -> Views/Korisnik/Index.cshtml
- /Korisnik/Details/{id} -> KorisnikController.Details -> Views/Korisnik/Details.cshtml
- /korisnici/{id}/profil -> KorisnikController.Details -> Views/Korisnik/Details.cshtml (custom)

## Vozila
- /Vozilo/Index -> VoziloController.Index -> Views/Vozilo/Index.cshtml
- /Vozilo/Details/{id} -> VoziloController.Details -> Views/Vozilo/Details.cshtml

## Voznje
- /Voznja/Index -> VoznjaController.Index -> Views/Voznja/Index.cshtml
- /Voznja/Details/{id} -> VoznjaController.Details -> Views/Voznja/Details.cshtml
  - prikaz putnika koji se voze/su se vozili (rezervacije te voznje)
  - prikaz ocjena te voznje + prosjek ocjena voznje
  - prikaz ocjena vozaca + srednja ocjena vozaca
- /Voznja/Active -> VoznjaController.Active -> Views/Voznja/Active.cshtml
- /Voznja/Create -> VoznjaController.Create -> Views/Voznja/Create.cshtml
- /Voznja/Edit/{id} -> VoznjaController.Edit -> Views/Voznja/Edit.cshtml
- /voznje/aktivne -> VoznjaController.Active -> Views/Voznja/Active.cshtml (custom)

## Rezervacije
- /Rezervacija/Index -> RezervacijaController.Index -> Views/Rezervacija/Index.cshtml
- /Rezervacija/Details/{id} -> RezervacijaController.Details -> Views/Rezervacija/Details.cshtml

## Placanja
- /Placanje/Index -> PlacanjeController.Index -> Views/Placanje/Index.cshtml
- /Placanje/Details/{id} -> PlacanjeController.Details -> Views/Placanje/Details.cshtml

## Ocjene
- /Ocjena/Index -> OcjenaController.Index -> Views/Ocjena/Index.cshtml
- /Ocjena/Details/{id} -> OcjenaController.Details -> Views/Ocjena/Details.cshtml
- /Ocjena/Create?rezervacijaId={id} -> OcjenaController.Create -> Views/Ocjena/Create.cshtml
  - putnik moze ocijeniti vozaca
  - vozac moze ocijeniti putnika za istu rezervaciju
