# Konfiguracija

[← Wiki](Home.md)

Postavke se zadaju kroz varijable okruženja (vidi `.env.example`) ili `appsettings*.json`.
Vrijednosti u tablici su primjeri/zadane vrijednosti.

## Baza i autentifikacija
| Varijabla | Opis |
|---|---|
| `SA_PASSWORD` | lozinka SQL Server `sa` korisnika (Docker) |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` | Google prijava (opcionalno; prazno = isključeno) |
| `DUMMY_DATA` | `true` seeda demo korisnike/podatke |

## AI asistent
| Varijabla | Opis |
|---|---|
| `AI_API_TYPE` | `OpenWebUi` ili `DeepSeek` |
| `AI_BASE_URL` | bazni URL AI providera |
| `AI_API_KEY` | API ključ providera (prazno = AI isključen) |
| `AI_MODEL` | naziv modela (prazno = automatski prvi dostupni) |

## MCP poslužitelj
| Varijabla | Opis |
|---|---|
| `MCP_API_KEY` | ključ za pristup MCP-u (prazno = isključeno) |
| `MCP_USER_ID` | ID korisnika u čije ime MCP klijent djeluje |

## Karte i rute
| Varijabla | Opis |
|---|---|
| `MAPS_TILE_URL` | URL kartografskih pločica (OSM) |
| `MAPS_TILE_ATTRIBUTION` | atribucija karte |
| `MAPS_NOMINATIM_BASE_URL` | geokodiranje (Nominatim) |
| `MAPS_NOMINATIM_USER_AGENT` | User-Agent za Nominatim |
| `MAPS_ROUTING_BASE_URL` | usmjeravanje (OSRM) |
| `MAPS_ROUTING_TIMEOUT_MILLISECONDS` | timeout OSRM-a |
| `MAPS_CONTACT_EMAIL` | kontakt za vanjske servise |

## Javna web pretraga
| Varijabla | Opis |
|---|---|
| `PUBLIC_WEB_SEARCH_ENABLED` | uključi/isključi web pretragu |
| `PUBLIC_WEB_SEARCH_WIKIPEDIA_API_URL_TEMPLATE` | Wikipedia API predložak (`{language}`) |
| `PUBLIC_WEB_SEARCH_DUCKDUCKGO_API_URL` | DuckDuckGo API |
| `PUBLIC_WEB_SEARCH_USER_AGENT` | User-Agent za pretragu |
| `PUBLIC_WEB_SEARCH_TIMEOUT_MILLISECONDS` | timeout izvora |
| `PUBLIC_WEB_SEARCH_CACHE_MINUTES` | trajanje cachea |
| `PUBLIC_WEB_SEARCH_MAX_RESULTS` | maksimalan broj rezultata |

## Docker / izdanje
| Varijabla | Opis |
|---|---|
| `SIDESEAT_IMAGE` | image tag (npr. `nikolica/sideseat:latest`) |
| `APP_DOMAIN` | domena za reverse proxy/produkciju |
