# Plansza 1: nazwane punkty i trasy

Źródło prawdy: `dev/src/district.js` (`POINTS`, `ROUTES`, `ZONES`) i `dev/src/interiors.js` (`INTERIOR_POINTS`). W grze: `window.__mg.POINTS`, `window.__mg.jumpTo(id)`.

Układ: x rośnie na wschód (ocean na zachodzie, x < −84), z rośnie na południe (siłownia na północy, z < 11; ulica z 20,6…36,1; podwórko z 48…100). `y` to wysokość podłogi, `face` to kierunek patrzenia (0 = na południe, π = na północ, π/2 = na wschód).

## Siłownia (baza)

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `gym_start` | -1.5 | 0 | 2.8 | m1.start | start misji, środek siłowni |
| `gym_ring_access` | 9 | 0 | -1.2 | m1.warmup | schodki na ring (rozgrzewka) |
| `gym_ring_center` | 9 | 0.95 | -5.5 | m1.warmup | środek ringu |
| `gym_talk` | 3.2 | 0 | 3.4 | m1.brief|m1.return1|m1.end | rozmowa z trenerem: zlecenie, wypłata, koniec misji |
| `gym_trainer` | 4.3 | 0 | 2.4 | m1 | miejsce trenera (zleceniodawca) |
| `gym_door_in` | -6 | 0 | 9.6 |  | drzwi od środka |
| `gym_door_out` | -6 | 0 | 12.8 |  | drzwi od zaułka |

## Ulica

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `street_gym` | -6.75 | 0.15 | 21.6 |  | wylot zaułka siłowni na ulicę |

## Sklep BRAXHORN NUTRITION

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `shop_door` | -20.7 | 0.15 | 21.4 | m1.heal | wejście do sklepu z chodnika |
| `shop_inside` | -20.7 | 0.15 | 19.4 |  | za drzwiami sklepu |
| `shop_counter` | -22.1 | 0.15 | 15 | m1.heal | miejsce zakupu: lada (cena, efekt leczenia, saldo) |
| `shop_clerk` | -22.1 | 0.15 | 12.85 | m1.heal | sprzedawca za ladą |
| `shop_fridge` | -16.3 | 0.15 | 13.6 |  | lodówka z napojami |

## Dyskoteka NOVA

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `club_door` | -42 | 0.15 | 35 | m1.club | wejście do dyskoteki z ulicy |
| `club_bouncer` | -39.3 | 0.15 | 38.2 | m1.club | miejsce przyszłej ochrony w przedsionku |
| `club_vestibule` | -42 | 0.15 | 39 |  | przedsionek |
| `club_hall_entry` | -42 | 0.15 | 43 |  | wejście na salę |
| `club_dance` | -44 | 0.15 | 48.5 |  | środek parkietu |
| `club_bar` | -36.7 | 0.15 | 48 |  | przy barze (gracz) |
| `club_barman` | -33.4 | 0.15 | 48 | m1.club | barman |
| `club_contact` | -50 | 0.15 | 46.4 | m1.club | osoba związana ze zleceniem (loża) |
| `club_dj` | -49.2 | 0.55 | 53.6 |  | DJ |
| `club_back_door` | -35 | 0.15 | 55.9 | m1.club | przejście na zaplecze |
| `club_stairs_top` | -37.5 | 0.15 | 55.9 | m1.club | schody do piwnicy |
| `club_yard_exit` | -34.4 | 0.15 | 63.4 | m1.club | wyjście z zaplecza na podwórko |
| `club_people` | 10 miejsc | 0.15 | |  | miejsca dla przyszłych bawiących się ludzi |

## Piwnica pod klubem (starcie nr 2)

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `cellar_entry` | -40.2 | -3.6 | 60.8 | m1.fight2 | wejście do piwnicy |

| `fight2_center` | -45.5 | -3.6 | 52 | m1.fight2 | punkt starcia nr 2: piwnica pod klubem |
| `fight2_enemy_wait` | -49.4 | -3.6 | 47.2 | m1.fight2 | przeciwnik przy stole z torbą |
| `fight2_bag` | -50.4 | -2.8 | 47 | m1.fight2 | torba z pieniędzmi siłowni (na stole) |

## Nabrzeże i punkt dostaw

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `quay_entry` | -70 | 0.15 | 28.5 |  | koniec ulicy przy nabrzeżu |
| `quay_delivery` | -77.4 | 0.15 | 83 | m1.pickup | punkt dostaw: odbiór torby ze sprzętem (+mała zapłata) |
| `quay_delivery_item` | -78.6 | 0.85 | 81.2 | m1.pickup | torba ze sprzętem na palecie |

| `pier_end` | -108 | 0.32 | 73 |  | koniec mola |

## Podwórko i boisko (starcie nr 1)

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `yard_gate_west` | -62.5 | 0.15 | 77 | m1.return1 | brama podwórka od nabrzeża (skrót) |
| `yard_brama_street` | -10 | 0.15 | 35.2 |  | brama na podwórko od ulicy (naprzeciw siłowni) |
| `yard_brama_yard` | -10 | 0.15 | 50 | m1.return1 | wyjście z podwórka w stronę sklepu i siłowni |

| `fight1_center` | -10 | 0.15 | 80 | m1.fight1 | punkt starcia nr 1: boisko |
| `fight1_player` | -15 | 0.15 | 80 | m1.fight1 | gracz wchodzi na boisko |
| `fight1_enemy_wait` | -10 | 0.15 | 89.4 | m1.fight1 | przeciwnik czeka na ławce |
| `fight1_enemy_enter` | -3.5 | 0.15 | 98.6 | m1.fight1 | naturalne wejście przeciwnika: klatka schodowa bloku |
| `fight1_camera` | -10 | 5.5 | 66.5 |  | miejsce kamery obejmującej obu zawodników (boisko od strony bramy) |

## Zamknięte przejścia

| Punkt | x | y | z | Etap | Opis |
|---|---|---|---|---|---|
| `closed_forest` | -74 | 0.15 | 111 | later.plansza3 | zamknięta droga do lasu z bunkrami |
| `closed_estate` | 101 | 0.15 | 28.5 | later.plansza2 | zamknięty wjazd na osiedle domków |

## Etapy misji „Dług w Braxhorn” → punkty

1. `m1.start` / `m1.warmup`: `gym_start` → `gym_ring_access` → `gym_ring_center` (rozgrzewka).
2. `m1.brief`: `gym_talk` (trener w `gym_trainer`) daje zlecenie: torba ze sprzętem z punktu dostaw.
3. `m1.pickup`: wyjście `gym_door_in` → `street_gym` → mijamy sklep (`shop_door`) i dyskotekę (`club_door`) → `quay_entry` → `quay_delivery` (torba: `quay_delivery_item`, mała zapłata).
4. `m1.return1` / `m1.fight1`: skrót przez `yard_gate_west` → boisko: `fight1_player`, przeciwnik czeka w `fight1_enemy_wait`, wychodzi z `fight1_enemy_enter`, środek walki `fight1_center`, kamera `fight1_camera` → wyjście `yard_brama_yard` → `yard_brama_street`.
5. `m1.heal`: sklep `shop_inside` → `shop_counter` (sprzedawca `shop_clerk`).
6. `m1.return1`: `gym_talk`: oddanie sprzętu, wypłata, nowe zadanie: torba z pieniędzmi w dyskotece.
7. `m1.club`: `club_door` → `club_bouncer` (ochrona) → `club_hall_entry` → bar `club_bar`/`club_barman`, kontakt `club_contact`, ludzie `club_people` → `club_back_door` → `club_stairs_top`.
8. `m1.fight2`: `cellar_entry` → `fight2_center`, przeciwnik `fight2_enemy_wait`, torba `fight2_bag` → wyjście `club_yard_exit` na podwórko.
9. `m1.end`: `gym_talk`: oddanie pieniędzy, nagroda; odblokowany cel: klub walk pod wiaduktem (starcie nr 3, później).

## Trasy przyszłych przechodniów (x, z)

- `near_sidewalk`: (-64, 22.3) → (-27, 22.3) → (-15, 22.3) → (2, 22.3) → (60, 22.3)
- `far_sidewalk`: (-64, 34.6) → (-10, 34.6) → (60, 34.6) → (100, 34.6)
- `quay`: (-70, 2) → (-70, 30) → (-72, 60) → (-72, 100) → (-74, 108)
- `pier`: (-80, 73) → (-108, 73)
- `yard_loop`: (-10, 52) → (-30, 58) → (-50, 66) → (-58, 77) → (-50, 94) → (-26, 92.5) → (6, 92.5) → (12, 70) → (-2, 58) → (-10, 52)
- `brama`: (-10, 34.6) → (-10, 52)
