import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch

import proba_apk_osiedla as qa


class ProbaAPK(unittest.TestCase):
    def test_kazda_proba_ma_nowy_plik_nawet_po_powtorzeniu(self):
        with patch.object(qa.subprocess, "Popen"):
            _, first = qa.start_recording("bar")
            _, second = qa.start_recording("bar")
            _, third = qa.start_recording("BX10001")
        self.assertEqual(len({first, second, third}), 3)

    def test_przedwczesny_koniec_nagrania_nie_zalicza_proby(self):
        log = ["BX_QA_START bar", "BX_QA_PASS bar", "BX_QA_GOTOWE"]
        with tempfile.TemporaryDirectory() as tmp, patch.object(qa, "run"), \
                patch.object(qa, "start_recording", return_value=(Mock(returncode=1), "/sdcard/test.mp4")), \
                patch.object(qa, "collect") as collect:
            with self.assertRaisesRegex(RuntimeError, "przed końcem"):
                qa.capture(log, Path(tmp), {"bar"}, "pkg", "activity")
            collect.assert_not_called()

    def test_przerwany_log_wymaga_potwierdzenia_gry(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(qa, "run"):
            with self.assertRaisesRegex(RuntimeError, "BX_QA_GOTOWE"):
                qa.capture(io.StringIO(""), Path(tmp), set(), "pkg", "activity")

    def test_blad_pobierania_filmu_po_pass_nie_ginie(self):
        process = Mock()
        lines = ["BX_QA_START bar", "BX_QA_PASS bar", "BX_QA_GOTOWE"]
        with tempfile.TemporaryDirectory() as tmp, patch.object(qa, "run"), \
                patch.object(qa, "start_recording", return_value=(process, "remote")), \
                patch.object(qa, "stop_recording") as stop, \
                patch.object(qa, "collect", side_effect=OSError("brak filmu")):
            with self.assertRaisesRegex(OSError, "brak filmu"):
                qa.capture(lines, Path(tmp), {"bar"}, "pkg", "activity")
            stop.assert_called_once_with(process)

    def test_jedna_klatka_nie_jest_nagraniem_proby(self):
        with patch.object(qa, "run", return_value='{"streams":[{"duration":"10","nb_read_packets":"1"}]}') as run:
            with self.assertRaises(ValueError):
                qa.verify_video(Path("bar.mp4"))
            self.assertEqual(run.call_count, 1)

    def test_uszkodzony_film_nie_trafia_do_gotowych(self):
        def pull(*args):
            Path(args[-1]).write_bytes(b"broken")
        with tempfile.TemporaryDirectory() as tmp, patch.object(qa, "run", side_effect=pull), \
                patch.object(qa, "verify_video", side_effect=subprocess.CalledProcessError(1, "ffmpeg")):
            out = Path(tmp) / "proba-apk"
            out.mkdir()
            with self.assertRaises(subprocess.CalledProcessError):
                qa.collect("remote", out / "bar.mp4")
            self.assertEqual(list(out.iterdir()), [])
