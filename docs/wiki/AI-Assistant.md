# AI asistent (SideSeat Copilot)

[← Wiki](Home.md)

SideSeat ima ugrađenog AI asistenta koji čita aktualne podatke, **priprema akcije ovisno o ulozi**
i dohvaća javne informacije s interneta. AI nikad ne mijenja podatke izravno — svaku promjenu
priprema kao akciju koju korisnik mora **potvrditi**.

## Kako radi
1. Korisnik piše poruku u Copilot widgetu.
2. Asistent dobiva poslovni kontekst i **samo alate dopuštene njegovoj ulozi**.
3. Za podatke poziva *read* alate; za promjene poziva `prepare_*` alate koji vrate akciju na potvrdu.
4. Potvrđena akcija izvršava se kroz `SideSeatCommandService` uz poslovna pravila i autorizaciju.

Tehnički: `OpenWebUiService` vodi tool-calling petlju prema AI provideru (OpenWebUI ili DeepSeek),
a `AiToolService` definira i izvršava alate. Pristup alatima filtrira `GetDefinitions(principal)`.

## Alati za čitanje (read)

| Alat | Tko | Opis |
|---|---|---|
| `get_current_user` | svi | sigurni podaci prijavljenog korisnika |
| `get_rides` | svi | vožnje (dostupne / moje / objavljene / sve\*) |
| `get_reservations` | svi | rezervacije (moje / na mojim vožnjama / sve\*) |
| `get_balance` | svi | saldo, rezervirana sredstva, transakcije |
| `get_cities` | svi | gradovi + ID-evi i koordinate |
| `get_reviews` | svi | ocjene (moje / o meni / sve\*) |
| `get_vehicles` | **admin** | vozila |
| `get_users` | **admin** | korisnici |
| `get_payments` | **admin** | plaćanja |
| `search_public_web` | svi | javna web pretraga (Wikipedia + internet) |

\* opseg „sve“ dostupan je samo administratoru.

## Alati za promjene (`prepare_*`, traže potvrdu)

| Domena | Alati | Tko |
|---|---|---|
| Rezervacije | `prepare_create_reservation`, `prepare_check_in_reservation` | svi |
| Ocjene | `prepare_create_review`, `prepare_update_review`, `prepare_delete_review` | svi (svoje) |
| Saldo | `prepare_create_balance_transaction` | svi (sebi), admin (svima) |
| Vožnje | `prepare_create_ride`, `prepare_update_ride`, `prepare_delete_ride`, `prepare_start_ride`, `prepare_finish_ride` | **vozač** |
| Gradovi/Vozila/Korisnici/Plaćanja | `prepare_*_city`, `prepare_*_vehicle`, `prepare_*_user`, `prepare_*_payment` | **admin** |
| Admin rezervacije | `prepare_update_reservation`, `prepare_delete_reservation` | **admin** |

Pristup se provjerava **dvostruko**: alat se ne nudi modelu ako uloga nema pravo, a i pri izvršenju
se ponovno provjerava uloga.

## Dohvat podataka s interneta
`search_public_web` kombinira **Wikipediju** i **DuckDuckGo** (opseg `auto`, `wikipedia` ili `internet`),
s jezikom `hr`/`en`, cacheom i timeoutima. Rezultati se citiraju kao Markdown linkovi.
Za SideSeat poslovne podatke koriste se interni alati, nikad web pretraga.

## Konfiguracija
AI se uključuje varijablama `AI_API_TYPE`, `AI_BASE_URL`, `AI_API_KEY`, `AI_MODEL`,
a web pretraga `PUBLIC_WEB_SEARCH_*`. Vidi [Konfiguraciju](Configuration.md).
