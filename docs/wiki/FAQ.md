# Često postavljana pitanja (FAQ)

[← Wiki](Home.md)

**Je li plaćanje stvarno?**
Ne. Uplate su mock (demo) — stvarna naplata se ne izvršava, ali se sve bilježi u povijesti transakcija.

**Kako dati napojnicu?**
Isključivo karticom, u obrascu za ocjenu vozača, nakon završene vožnje i samo pri prvoj ocjeni vozača.
Vidi [Saldo i plaćanja](Payments-and-Balance.md).

**Zašto ne mogu isplatiti cijeli saldo?**
Sredstva rezervirana za potvrđene/u procesu rezervacije (plaćene saldom) zaključana su do završetka
ili otkazivanja vožnje. Isplativo je `saldo − rezervirana sredstva`.

**Kako postati vozač?**
Ispuni KYC (**Račun → KYC vozača**). Tek tada možeš objavljivati vožnje. Vidi [Vodič za vozača](Driver-Guide.md).

**Radi li aplikacija bez AI-ja?**
Da. Ako `AI_API_KEY` nije postavljen, AI widget je isključen, a sve ostalo radi normalno.

**Vidi li AI tuđe podatke?**
Ne. AI dobiva samo alate primjerene tvojoj ulozi i čita samo podatke koje smiješ vidjeti; opseg „sve“
imaju samo administratori. Vidi [AI asistent](AI-Assistant.md).

**Lijevi meni se „skuplja“ kad otvorim sve grupe?**
Ispravljeno — meni je skrolabilan i grupe se više ne stišću.

**Što animira aute u pozadini?**
`route-background.js` projicira **stvarne dostupne rute** (planirane vožnje) na SVG i animira aute po njima,
uz nadogradnju na stvarnu cestovnu geometriju (OSRM). Poštuje `prefers-reduced-motion`.

**Gdje je dokumentacija razvoja?**
[Arhitektura, folderi i klase](../ARCHITECTURE.md) i [changelog](../../changelogs/README.md).
