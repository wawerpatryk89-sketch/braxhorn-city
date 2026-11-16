from array import array
import math
import unittest

from osiedle_mowa import mouth_track


class FacialSpeech(unittest.TestCase):
    def test_pause_closes_mouth_and_releases_rounding(self):
        silence = array("h", [0] * 1600)
        track = mouth_track(silence.tobytes())
        self.assertEqual(track, bytes(20))

    def test_equal_volume_vowels_have_different_rounding(self):
        def track(frequency):
            pcm = array(
                "h",
                [
                    round(7000 * math.sin(2 * math.pi * frequency * i / 8000))
                    for i in range(1600)
                ],
            )
            return mouth_track(pcm.tobytes())

        back, front = track(900), track(1800)
        self.assertGreater(max(front[::2]), 180)
        self.assertGreater(max(back[::2]), 180)
        self.assertGreater(sum(back[1::2]), sum(front[1::2]) + 400)
