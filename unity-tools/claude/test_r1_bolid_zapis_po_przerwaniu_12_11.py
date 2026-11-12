"""MIASTO R1 (12.11.2026): bolid 43195552389 ucięty limitem zadania 20 s po starcie Gradle — nic się nie zapisało.

Start bez pamięci podręcznej: import 40 min, baza siłowni 70 min, generowanie miasta 113 min, obróbka tekstur 82 min,
kompilacja 29 min = 5 h 40 min przy limicie zadania 355 min. Po ucięciu kroki zapisu (checkpoint miasta, Library) zostały
pominięte, więc następny przebieg znów ruszyłby od zera. Poprawka: krok kompilacji ma własny, krótszy limit, a zapis
wykonuje się także po przerwanej kompilacji, o ile miasto zostało w całości wygenerowane.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")


def krok(nazwa):
    i = WF.index("    - name: " + nazwa)
    j = WF.find("\n    - name: ", i + 10)
    return WF[i:j if j > 0 else len(WF)]


class ZapisPoPrzerwaniu(unittest.TestCase):
    def test_kompilacja_ma_wlasny_limit_krotszy_niz_zadanie(self):
        k = krok("Kompilacja aplikacji Android (APK) z tej samej sceny")
        self.assertIn("timeout-minutes: 285", k)
        self.assertIn("timeout-minutes: 355", WF)          # limit zadania — zostaje ~55 min na zapis po przerwaniu kroku

    def test_kontener_unity_zatrzymany_przed_zapisem(self):
        k = krok("Zatrzymaj kontener Unity po przerwanej kompilacji")
        self.assertIn("steps.unity_android.outcome != 'success'", k)
        self.assertIn("docker kill", k)
        self.assertLess(WF.index("Zatrzymaj kontener Unity po przerwanej kompilacji"), WF.index("Pamiec Library Unity dla Androida - pelny zapis z Bee"))

    def test_library_zapisywana_takze_po_przerwanej_kompilacji(self):
        k = krok("Pamiec Library Unity dla Androida - pelny zapis z Bee")
        # po błędzie: gdy miasto gotowe ALBO gdy przebieg ruszał bez Library (zimny start) — patrz test_r1_build_etapy_12_11
        self.assertIn("steps.unity_android.outcome == 'failure' && (steps.generation_checkpoint.outputs.valid == 'true' || steps.library_restore.outputs.restored != 'true')", k)

    def test_checkpoint_miasta_nie_wymaga_udanej_kompilacji(self):
        k = krok("Zaszyfruj kompletny checkpoint miasta")
        self.assertIn("!cancelled() && steps.generation_checkpoint.outputs.valid == 'true'", k)
        self.assertNotIn("unity_android.outcome", k)


if __name__ == "__main__":
    unittest.main()
