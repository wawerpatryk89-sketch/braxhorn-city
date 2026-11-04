# Patryk — oryginał i kontrola ruchu, 4.11.2026

Baza: `e93d6c01f1c64fc5527987c9a0a7a2c57daf86e8`, gałąź
`codex/osiedle-integracja-final-patryk-20261104`. Zgłoszenie: plastikowy wygląd
i nieprawidłowy bieg na osiedlu; nagranie IMG_0015.mp4 (3,17 s).

## Sprawdzone źródło

Odczytano oryginalny `/Braxhorn city/Patryk.zip` z 3.11. Model FBX i wszystkie
cztery mapy są identyczne bajtowo z hashami wymaganymi przez `prywatne.py`.
FBX SHA256: `fb8e2630bc7f835a743d917c35c378c7d074f0fde993a0bbf864b31ab67e1d97`.
FBX zawiera 65 kości Mixamo i źródłowe normalne siatki. Wszystkie mapy mają
4096×4096. Nie podmieniono sylwetki, stroju, siatki, UV ani map źródłowych.
Nie ustalono, który dokładnie SHA ma APK widoczny na nagraniu.

## Zmiany kodu — bez dowodu poprawnego wyglądu w Unity

- Usunięto wymuszane obracanie kostek w każdej klatce Walk/Run. Poprzedni kod
  po przekroczeniu 38° obracał stopę natychmiast do kierunku bioder; poniżej
  progu zostawiał ją bez korekty. Taka funkcja ma nieciągłość na progu i nie
  naprawia ustawienia całej nogi.
- Dla Walk/Run sprawdzane są kolejne istniejące źródła i oba dostępne awatary.
  Pierwszy klip spełniający istniejące kryteria nóg i długości kości zostaje
  zapisany. Brak poprawnego kandydata przerywa generowanie zamiast użyć
  wadliwego klipu. To może ujawnić blokadę wymagającą naprawy rigu w Unity.
- Próby ruchu obejmują oba końce klipu i co najmniej częstotliwość zapisu.
- Import Patryka jawnie zachowuje normalne modelu i wyłącza kompresję siatki.
  Gotowa mapa normalnych nie jest przeliczana z mapy wysokości. Kontrola
  ustawień naprawia także odziedziczony limit mipmap przy poprawnej reszcie
  importera. Color/Normal pozostają 4K, ORM 2K, ASTC 4×4 / jakość 50.

## Weryfikacja wykonana

- Rzeczywisty oryginał przepuszczono przez `przygotuj_patryka`: PASS dla hashy,
  identycznych bajtów Color/Normal oraz rozdzielczości Color/Normal 4K i ORM 2K.
- Parser C# dla trzech zmienionych plików: bez błędów składni.
- `git diff --check`: PASS.

Brak Unity Editor / Unity MCP w tej sesji. Nie wykonano kompilacji C# w Unity,
retargetingu, renderu ani testu Androida. Plastikowy wygląd nie ma jeszcze
potwierdzonej przyczyny. To nie jest zakończona naprawa całego osiedla.

## Wymagane dokończenie w Unity 6000.0.58f2

1. Wygenerować osiedle na tym kodzie; zachować logi wszystkich kandydatów.
   Jeżeli nie przejdzie żaden, poprawić referencyjną pozę/awatar na oryginalnym
   szkielecie, nie luzować progów ani przywracać prostowania kostek.
2. Porównać oryginał i postać w scenie w tym samym świetle: Color, normalne,
   roughness, metallic; skontrolować materiał po wszystkich etapach budowy.
3. Obejrzeć przód/bok/tył, chód–bieg–stop, skręt, skok, cios, kopnięcie,
   koszykówkę, tenis, auto i ponowne wejście do osiedla. Sprawdzić stopy,
   kolana, ślizganie, proporcje oraz ostrość tekstur na telefonie.
4. Osobno sprawdzić wcześniejsze poprawki osiedla: loże, przejścia, salon,
   auto, sporty i migotanie. Nie uznawać ich za zaliczone na podstawie kodu.

Nie uruchomiono buildu. Obowiązuje osobna decyzja właściciela według AGENTS.md.
