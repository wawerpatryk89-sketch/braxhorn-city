"""MIASTO R1 (12.11.2026, właściciel 20:32: „naprawiasz jeden błąd i nie sprawdzasz, czy więcej nie ma”).

Przegląd całej ścieżki zapisu po bolidach 43195552389 (ucięty limitem zadania) i 43287609038 (zatrzymany przed stratą):

1. Znacznik gotowego miasta (pamiec-rejonu.json) Unity pisało dopiero PO obróbce tekstur — w zimnym przebiegu ok. 309. minuty
   kroku, czyli po limicie kroku 285 min. Przerwany build nie zapisałby ani miasta, ani Library (warunek zapisu Library
   wymagał ważnego znacznika). Teraz: termin z workflow + reguła etapów w Unity — zimny start kończy ETAP 1 czysto po
   generowaniu (znacznik, wyjście 0, bez APK), a CI zapisuje miasto i Library.
2. Krok zapisu Library po UDANYM APK miasta zawsze kończył się błędem „rm: Permission denied” (build/Android zapisuje root
   z kontenera; bolid 42865354389 z 10.11) — Library nie była zapisywana, a continue-on-error to ukrywał.
3. Zimny przebieg przerwany PRZED końcem generowania też zapisuje Library (inaczej kolejny znów od zera).
4. PRZYCZYNA zimnych przebiegów od 12.11: klucz profilu Library zawiera skrót Packages/manifest.json — dopisanie modułu Video
   (7f337c70) unieważniło całą zapisaną Library miasta (11 GB w wydaniu unity-ci-cache-miasto-2a1cb92d…, ta sama, z którą
   bolid 42865354389 dał APK w 2 h 33 min). Teraz workflow bierze tę kopię jawnie wskazanym kluczem poprzedniego profilu.
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
BUILD = (ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
REJON = (ROOT / "unity-city/Assets/Editor/BraxRejonBuildu.cs").read_text(encoding="utf-8")
APK = WF[WF.index("\n  android-apk:"):WF.index("\n  scene-only-unity6:")]


def krok(nazwa):
    i = APK.index("    - name: " + nazwa)
    j = APK.find("\n    - name: ", i + 10)
    return APK[i:j if j > 0 else len(APK)]


def liczba(tekst, nazwa):
    return int(re.search(r"\b" + nazwa + r":\s*(\d+)", tekst).group(1))


class EtapyUnity(unittest.TestCase):
    def test_regula_etapu_czyta_termin_z_pliku_ci(self):
        self.assertIn("public static bool KoniecEtapu(out string opis)", REJON)
        cialo = REJON[REJON.index("public static bool KoniecEtapu"):REJON.index("// Pomiar etapów do logu CI")]
        self.assertIn('Wartosc("termin")', cialo)
        self.assertIn('Wartosc("reszta_min")', cialo)
        self.assertIn("return zostalo < potrzeba;", cialo)
        # bez terminu (lokalnie, inne workflowy) — zawsze jeden przebieg jak dotąd
        self.assertIn("termin <= 0) return false;", cialo)

    def test_etap_1_konczy_sie_przed_teksturami_ze_znacznikiem_i_bez_apk(self):
        paczka = BUILD[BUILD.index("static void Paczka("):]
        etap = paczka.index("BraxRejonBuildu.KoniecEtapu(out string etapOpis)")
        tekstury = paczka.index("CapFacadeTextures(target);")
        self.assertLess(etap, tekstury)
        blok = paczka[etap:tekstury]
        self.assertIn("BraxRejonBuildu.ZapiszZnacznik(target,scenePaths,sourceCommit,t0);", blok)
        self.assertIn('"etap-1.txt"', blok)
        self.assertIn("return;", blok)
        self.assertNotIn("BuildPlayer", blok)
        # tylko świeżo wygenerowany rejon na Androidzie; trafienie z pamięci i „oba” bez zmian
        self.assertIn('if(!zPamieci&&rejon!="oba"&&target==BuildTarget.Android&&BraxRejonBuildu.KoniecEtapu', paczka)
        # zwykły przebieg: znacznik jak dotąd po teksturach, przed kompilacją
        po = paczka[tekstury:]
        self.assertLess(po.index("BraxRejonBuildu.ZapiszZnacznik("), po.index("BuildPipeline.BuildPlayer("))


class EtapyWorkflow(unittest.TestCase):
    def test_termin_zapisany_przed_unity_i_spojny_z_limitami(self):
        t = krok("Termin kroku Unity i regula etapow")
        self.assertLess(APK.index("Termin kroku Unity i regula etapow"), APK.index("Kompilacja aplikacji Android (APK) z tej samej sceny"))
        self.assertIn(">> unity-city/rejon-ci.txt", t)
        self.assertIn("termin=%s", t)
        self.assertIn("reszta_min=%s", t)
        unity = krok("Kompilacja aplikacji Android (APK) z tej samej sceny")
        self.assertEqual(liczba(t, "LIMIT_KROKU_MIN"), int(re.search(r"timeout-minutes: (\d+)", unity).group(1)))
        self.assertEqual(liczba(t, "LIMIT_ZADANIA_MIN"), int(re.search(r"\n    timeout-minutes: (\d+)", APK).group(1)))
        self.assertGreaterEqual(liczba(t, "ZAPISY_MIN"), 30)
        self.assertIn('echo "START_ZADANIA=$(date +%s)" >> "$GITHUB_ENV"', krok("Sprawdz staly podpis i numer aktualizacji PRZED kompilacja"))

    def test_regula_na_zmierzonych_przebiegach(self):
        t = krok("Termin kroku Unity i regula etapow")
        krok_min, zadanie, zapisy, reszta = (liczba(t, n) for n in ("LIMIT_KROKU_MIN", "LIMIT_ZADANIA_MIN", "ZAPISY_MIN", "RESZTA_MIN"))

        def po_generowaniu(przygotowanie, generowanie_gotowe):
            termin = min(przygotowanie + krok_min - 3, zadanie - zapisy)      # minuty zadania
            return termin - (przygotowanie + generowanie_gotowe)

        # bolid 43195552389 (zimny): przygotowanie 16 min, generowanie gotowe w 225. minucie kroku, potem tekstury 82 + APK 29+
        zimny = po_generowaniu(16, 225)
        self.assertLess(zimny, reszta, "zimny przebieg musi skończyć etap 1 po generowaniu")
        self.assertGreater(zimny, 0, "…i to jeszcze przed limitem kroku")
        # bolid 42865354389 (Library przywrócona): generowanie gotowe w 112. minucie kroku, tekstury 18 + APK 20
        cieply = po_generowaniu(20, 112)
        self.assertGreaterEqual(cieply, reszta, "przebieg z Library musi zbudować APK od razu")
        # etap 2 w najgorszym razie: generowanie jak w zimnym (113 min) po imporcie z Library, tekstury i APK w zimnym tempie
        najgorszy = po_generowaniu(20, 16 + 9 + 113)
        self.assertGreaterEqual(najgorszy, reszta)
        self.assertGreater(reszta, 82 + 29 + 10, "reszta musi pokrywać zimne tekstury + kompilację + Gradle")

    def test_etap_1_nie_szuka_apk_ale_zapisuje_miasto_i_library(self):
        e = krok("Etap 1 z 2")
        self.assertIn("id: etap", e)
        self.assertIn("build/Android/etap-1.txt", e)
        self.assertIn("etap1=true", e)
        self.assertIn("steps.etap.outputs.etap1 != 'true'", krok("Wgraj APK do ukrytego wydania"))
        for nazwa in ("Zaszyfruj kompletny checkpoint miasta", "Pamiec Library Unity dla Androida - pelny zapis z Bee"):
            self.assertNotIn("etap1", krok(nazwa))                     # zapisy nie zależą od etapu
        self.assertLess(APK.index("- name: Etap 1 z 2"), APK.index("- name: Wgraj APK do ukrytego wydania"))

    def test_library_zapis_nie_pada_na_apk_roota(self):
        k = krok("Pamiec Library Unity dla Androida - pelny zapis z Bee")
        self.assertIn('sudo rm -f "build/Android/braxhorn-$tag.apk" || true', k)
        self.assertNotRegex(k, r'\n\s+rm -f "build/Android')

    def test_library_zapis_po_zimnym_przerwanym_przebiegu(self):
        k = krok("Pamiec Library Unity dla Androida - pelny zapis z Bee")
        self.assertIn("steps.library_restore.outputs.restored != 'true'", k)
        self.assertIn("!cancelled()", k)
        self.assertIn("id: library_restore", APK)
        z = krok("Zatrzymaj kontener Unity po przerwanej kompilacji")
        self.assertIn("docker stop -t 45", z)                          # najpierw łagodnie — baza importu Library domknięta


class LibraryPoprzedniegoProfilu(unittest.TestCase):
    KLUCZ = "library-android-v2-miasto-2a1cb92dc336cbc2aebbffc0"

    def test_krok_pobiera_kopie_poprzedniego_profilu_gdy_brak_biezacej(self):
        k = krok("Library z poprzedniego profilu pakietow")
        self.assertIn("id: durable_migracja", k)
        self.assertIn("POPRZEDNI_PROFIL: " + self.KLUCZ, k)
        self.assertIn('durable_cache.py download --zone "$ZONE"', k)
        self.assertIn('--from-key "$POPRZEDNI_PROFIL"', k)
        self.assertIn("continue-on-error: true", k)                    # brak kopii = zimny start z regułą etapów, nie błąd
        self.assertIn("steps.bibl_and.outputs.cache-matched-key == ''", k)
        self.assertIn("steps.durable_download.outputs.downloaded != 'true'", k)
        kolejnosc = [APK.index("- name: " + n) for n in ("Trwala kopia Library gdy Actions usunelo cache",
            "Library z poprzedniego profilu pakietow", "Migracja starej pamieci tylko gdy nie ma nowej kopii",
            "Pamiec Library Unity dla Androida - sprawdzenie i rozpakowanie", "Kompilacja aplikacji Android (APK) z tej samej sceny")]
        self.assertEqual(kolejnosc, sorted(kolejnosc))
        self.assertIn("steps.durable_migracja.outputs.downloaded != 'true'", krok("Migracja starej pamieci tylko gdy nie ma nowej kopii"))

    def test_rozpakowanie_przyjmuje_tylko_jawnie_wskazany_profil(self):
        k = krok("Pamiec Library Unity dla Androida - sprawdzenie i rozpakowanie")
        self.assertIn("MIGRACJA: ${{ steps.durable_migracja.outputs.downloaded }}", k)
        self.assertIn("POPRZEDNI_PROFIL: " + self.KLUCZ, k)
        self.assertIn('EXTRA+=(--accept-key "$POPRZEDNI_PROFIL")', k)
        self.assertIn('if [ "$MIGRACJA" = true ]; then', k)

    def test_narzedzia_nie_mieszaja_rejonu_unity_ani_wariantu(self):
        import sys
        sys.path.insert(0, str(ROOT / "unity-tools/updates"))
        import unity_cache
        dzis = unity_cache.profile(ROOT / "unity-city", "miasto", "6000.0.58f2")
        stary = {**dzis, "inputs": {"Packages/manifest.json": "4a1926f5cbe4b0a4515f2146090e2922cfcb1d4403c0dc3001574bfec59303ac"}, "key": self.KLUCZ}
        self.assertTrue(unity_cache.compatible(stary, dzis, self.KLUCZ))
        self.assertFalse(unity_cache.compatible(stary, dzis))                                   # bez wskazania — nie
        self.assertFalse(unity_cache.compatible({**stary, "zone": "osiedle"}, dzis, self.KLUCZ))
        self.assertFalse(unity_cache.compatible({**stary, "unity": "6000.0.59f1"}, dzis, self.KLUCZ))
        self.assertFalse(unity_cache.compatible({**stary, "variant": "x86"}, dzis, self.KLUCZ))


if __name__ == "__main__":
    unittest.main()
