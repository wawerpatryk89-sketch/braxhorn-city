# Siłownia

Wspólna paczka Mixamo zawiera 13 klipów. Każdy profil wybiera trzy ćwiczenia. Rejon korzysta z jednej karty postaci i może wskazać dla niej stanowisko i godzinę spotkania.

| Postać | Ćwiczenie 1 | Ćwiczenie 2 | Ćwiczenie 3 |
| --- | --- | --- | --- |
| [Aneta](../uslugi/fitness/aneta/) | Pajacyki | Przysiad na jednej nodze | Burpees |
| [Patryk](../grywalni/patryk/) | Przysiad ze sztangą na plecach | Uginanie przedramion | Unoszenie ramion w przód |
| [Gitara](../glowne-poza-liga/gitara/) | Wymach odważnikiem | Unoszenie ramion w przód | Pajacyki |
| [Kiler](../glowne-poza-liga/kiler/) | Przysiad ze sztangą na plecach II | Rwanie II | Wysokie podciąganie w pozycji sumo |
| [Kamil](../glowne-poza-liga/kamil/) | Burpees | Uginanie przedramion | Przysiad na jednej nodze |
| [Mateusz](../glowne-poza-liga/mateusz/) | Przysiad ze sztangą nad głową | Rwanie | Wymach odważnikiem |
| [Monika](../uslugi/monika/) | Pajacyki | Unoszenie ramion w przód | Wymach odważnikiem |

[Oryginalna paczka gym.zip](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/gym.zip) · [Pierwsze sześć postaci – 18 przypisań](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Silownia_6_postaci_18_ruchow.jpg) · [Podgląd Moniki](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Monika_16_ruchow.jpg) · [Katalog klipów i profili](gym.json)

## Użycie w grze

Na gałęzi z tym katalogiem rozpakuj paczkę bazową postaci. Kod C# pobieraj z tej gałęzi. Z katalogu głównego repozytorium wykonaj polecenie dla wybranych postaci:

```sh
python3 unity-tools/claude/gym.py /sciezka/do/gym.zip aneta patryk
```

Importer sprawdza SHA-256 archiwum i wybranych FBX. Przygotowanie obsady Osiedla w CI automatycznie pobiera ćwiczenia wybranych postaci ze szkicu. W Unity dla NPC uruchom ich dotychczasowe polecenie w menu **Braxhorn → Postacie**. Kontroler dostaje trzy stany. Stanowisko wywołuje np. `TryGym("JumpingJacks")` na komponencie Anety. Klip wraca do spoczynku po jednym wykonaniu. System stanowiska decyduje o następnym powtórzeniu. Patryk korzysta z istniejącej ścieżki Legacy Osiedla, np. `animation.CrossFade("GymBackSquat")`.

Aby dodać kolejną postać z dostępem do siłowni, dodaj profil z jej identyfikatorem oraz trzema identyfikatorami z listy `clips` w `gym.json`, połącz kartę postaci z profilem i ponownie przygotuj jej kontroler. Inne profile i źródłowa paczka są niezależne od tego wyboru. Klipy opisują ruch ciała. Sztangę, odważnik lub hantle dostarcza stanowisko w scenie. Rozmieszczenie postaci ustala obsada danego rejonu.
