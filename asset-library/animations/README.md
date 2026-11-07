# BRAXHORN CITY — wspólna baza paczek animacji A/T

Materiały źródłowe dla wykonawców wszystkich rejonów, do późniejszego tworzenia animacji zawodników. Obie paczki pozostają nieprzypisane do postaci.

Status: `source_archive_unassigned`.

| Paczka | Oryginalny plik | Rozmiar | Pliki FBX |
|---|---|---:|---:|
| A | `animacje pozycja A.zip` | 93 704 733 B | 118 |
| T | `pozycja T animacje.zip` | 140 632 399 B | 119 |

Oznaczenia A/T pochodzą z nazw dostarczonych paczek. Zgodność z poszczególnymi szkieletami pozostaje do sprawdzenia podczas przyszłej pracy nad animacjami.

## Pobranie

Oryginalne ZIP-y znajdują się w wersji roboczej GitHub Release:

- Tag: `global-animation-library-at-20261107`.
- [Otwórz paczki na GitHub](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/tag/untagged-40a1b1110d8e5e5d03da) po zalogowaniu kontem z dostępem do tego wydania.
- Pliki, identyfikatory załączników i pełne sumy SHA-256: [catalog.json](catalog.json).

Wykonawca korzystający z GitHub CLI może pobrać oba oryginały przez autoryzowane REST:

```bash
gh api repos/wawerpatryk89-sketch/braxhorn-city/releases/assets/719031450 \
  -H 'Accept: application/octet-stream' > 'animacje pozycja A.zip'
gh api repos/wawerpatryk89-sketch/braxhorn-city/releases/assets/719031603 \
  -H 'Accept: application/octet-stream' > 'pozycja T animacje.zip'
```

GitHub zapisał nazwy załączników jako `animacje.pozycja.A.zip` i `pozycja.T.animacje.zip`. Oryginalne nazwy znajdują się w tabeli i katalogu; zawartość ZIP-ów pozostała identyczna.

Po pobraniu porównaj rozmiar i SHA-256 z katalogiem. Materiały są wspólne dla wszystkich gałęzi rejonów; wydanie źródłowe ma status DRAFT.

## Zakres sprawdzenia

Lokalnie sprawdzono rozmiary, SHA-256, integralność ZIP (CRC) oraz liczbę plików FBX. Po wysłaniu GitHub potwierdził stan `uploaded`, identyczny rozmiar i identyczne SHA-256 obu archiwów. Archiwów nie rozpakowywano ani nie importowano do Unity. Działanie animacji nie było testowane. Ten katalog zapisuje źródła do późniejszego wykorzystania.
