"""Build #46 (17.11.2026, run 44079266370) died in Unity's import: "No space left on device". Before Unity it had 46 GB
free (54 GB used after the cleanup, 99 GB after the source preparation and the restored Library); #43's clean import had
taken 54 GB. The runner now drops what the host never uses and, below 85 GB free, the restored Library (its GUID sidecars
are already in Assets) - a clean import as #43.
#47 (17/18.11, run 44175616635) lost its runner 1.5 h into Unity, mid-generation, with no error: ubuntu-latest had moved to
Ubuntu 26.04 (#46 too, on /dev/root; #47 on /dev/mapper/cow-root). #43, the last APK built, ran on ubuntu-24.04: the APK
job is pinned there again, and the cleanup also drops Node's global modules, Google Cloud SDK, vcpkg, Firefox, the AWS
CLIs, pipx, Rust and the Android SDK's extras (the job uses none of them)."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
CACHE = (ROOT / "unity-tools/mistrzowie/library_cache.sh").read_text(encoding="utf-8")
DELIVERY = (ROOT / "unity-tools/mistrzowie/apk_delivery.py").read_text(encoding="utf-8")
PICKER = (ROOT / "unity-tools/mistrzowie/build_content_picker.sh").read_text(encoding="utf-8")


class DyskBuildu(unittest.TestCase):
    def test_the_host_keeps_only_what_the_apk_checks_use(self):
        self.assertIn('"$sdk/ndk" "$sdk/ndk-bundle" "$sdk/cmake" "$sdk/emulator" "$sdk/system-images"', WF)
        self.assertIn('test -d "$sdk/build-tools" && test -d "$sdk/platforms"', WF)
        # what stays is what apk_delivery uses: build-tools (aapt, apksigner) and platforms (android.jar)
        self.assertIn('(Path(root) / "build-tools").glob', DELIVERY)
        self.assertIn('(Path(root) / "platforms").glob("android-*/android.jar")', DELIVERY)
        # the picker compiles with the Unity image's JDK and SDK, not the host's
        self.assertIn("android_root=/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer", PICKER)
        self.assertIn("sudo rm -rf /usr/share/dotnet /opt/ghc /usr/local/.ghcup /opt/hostedtoolcache /usr/share/swift", WF)
        self.assertNotIn("/opt/hostedtoolcache/CodeQL", WF)
        self.assertNotIn("setup-python", WF)                                   # python3 is the system one

    def test_the_apk_job_uses_a_pinned_alternate_runner_image(self):
        job = WF[WF.index("  exploration-apk:"):]
        job = job[:job.index("    steps:")]
        self.assertIn("runs-on: ubuntu-22.04", job)
        self.assertNotIn("runs-on: ubuntu-latest", job)
        for gone in ("/usr/local/lib/node_modules", "/usr/lib/google-cloud-sdk", "/usr/local/share/vcpkg", "/usr/lib/firefox",
                     "/usr/local/aws-cli", "/usr/local/aws-sam-cli", "/opt/pipx", "/home/runner/.rustup", "/home/runner/.cargo",
                     '"$sdk/extras"'):
            self.assertIn(gone, WF)
        # apksigner needs the host's Java: it stays
        self.assertNotIn("/usr/lib/jvm", WF[WF.index("sudo rm -rf /usr/share/dotnet"):WF.index("sudo apt-get clean")])

    def test_the_swap_goes_to_the_temporary_disk_when_it_has_room(self):
        self.assertIn('if mountpoint -q /mnt && [ "$(df --output=avail -BG /mnt | tail -1 | tr -dc 0-9)" -ge 32 ]; then swap=/mnt/swapfile-mistrzowie; fi', WF)
        self.assertIn('sudo fallocate -l 24G "$swap"', WF)                     # 24.11: #54 killed for memory with the 12 GiB one
        self.assertIn('sudo swapon "$swap"', WF)

    def test_a_tight_disk_drops_the_restored_library(self):
        self.assertIn('if [ "$avail" -lt 85 ] && [ -d unity-city/Library ]; then', WF)
        self.assertIn('echo "BRAX_LIBRARY_DROPPED free=${avail}G < 85G: clean import"', WF)
        # the GUID sidecars are copied into Assets before the Library is moved in, so they survive its removal
        self.assertLess(CACHE.index('echo "Champions cached GUID sidecars'), CACHE.index('mv "$tmp/Library" "$project/Library"'))
        # the guard runs after the restore and before Unity
        restore = WF.index("library_store.sh restore unity-city")
        guard = WF.index("BRAX_LIBRARY_DROPPED")
        unity = WF.index("- name: Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania")
        self.assertLess(restore, guard)
        self.assertLess(guard, unity)
        # #43's clean import: 76 -> 130 GB used of 145
        self.assertGreater(85, 130 - 76)


if __name__ == "__main__":
    unittest.main()
