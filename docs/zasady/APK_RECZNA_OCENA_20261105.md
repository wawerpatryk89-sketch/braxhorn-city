# APK do ręcznej oceny — decyzja właściciela 5.11.2026

Dotyczy wszystkich rejonów BRAXHORN CITY: miasta, osiedla, obozu, Miasta Mistrzów
oraz kolejnych rejonów. Przy dalszych pracach używaj procedur Unity Essentials.

Tymczasowo kontrole jakości przejść, kolizji, ustawienia obiektów, proporcji,
kości i pozy postaci nie są warunkiem wydania testowego APK. Właściciel oceni
je w grze. Zachowuj problemy w raportach, bez nazywania ich zaliczonym testem.
Nie wyłączaj fizycznych colliderów i nie podmieniaj modeli ani animacji.

Wspólna implementacja: Assets/Editor/BraxBuildQuality.cs. Domyślnie tryb advisory.
Powrót do twardych kontroli: dodaj w projekcie plik
ProjectSettings/brax-quality-strict.txt. W obozie dodatkowy test Play Mode jest
osobnym, domyślnie wyłączonym parametrem quality_review; może też działać
w samodzielnym workflow diagnostycznym. Jego niepowodzenie nie blokuje APK.

Nie wolno ignorować błędów kompilacji C#, niewytworzenia wymaganych danych
przez generator, błędów zapisu scen, pakowania, odszyfrowania, podpisu ani braku
pliku APK. Nie catchuj całego generatora i nie udawaj sukcesu BuildPipeline.
Brak modelu, awatara lub klipu potrzebnego do dalszego generowania jest błędem
wykonania, innym niż niska ocena poprawnie wygenerowanej postaci.

Raport: build/QualityReview/quality-review.json, status MANUAL_DEVICE_REVIEW_REQUIRED.
Każde ostrzeżenie ma również prefiks BRAXHORN_QUALITY_ADVISORY w logu Unity.
Zielony workflow oznacza najwyżej powstanie pliku, nie akceptację jakości gry.

Ta decyzja zastępuje wcześniejsze wymaganie blokowania APK oceną jakości.
Nie upoważnia do uruchamiania, ponawiania ani anulowania buildów.
Zmiany działają tylko dla kolejnych uruchomień z nowym kodem, nie dla runów w toku.
