"""Create the owner-approved NOVA edit without altering the supplied original.

StreamingAssets is fetched by Unity automatically on approaching NOVA; it is not an
AudioClip reference included in the city's initial compressed data archive.
"""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'asset-library/sources/audio/Industrial-Pulse.original.mp3'
DEST = ROOT / 'unity-city/Assets/StreamingAssets/NOVA'
EXPECTED = 'e373ea0c75c6d0dd5a1cdee6e481b97c11bf79ef0afd6253f2ead00834ae9455'
CUT = 27.70


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def probe(path):
    return json.loads(subprocess.check_output([
        'ffprobe', '-v', 'error', '-show_entries',
        'format=duration:stream=sample_rate,channels', '-of', 'json', str(path)
    ], text=True))


def main():
    if sha(SOURCE) != EXPECTED:
        raise RuntimeError('The owner-supplied original Industrial Pulse changed')
    original = probe(SOURCE)
    duration = float(original['format']['duration'])
    if not 207 < duration < 208:
        raise RuntimeError('Unexpected source duration; review the edit')
    DEST.mkdir(parents=True, exist_ok=True)
    target = DEST / 'IndustrialPulseClub.mp3'
    subprocess.run([
        'ffmpeg', '-v', 'error', '-y', '-ss', str(CUT), '-i', str(SOURCE),
        '-vn', '-map_metadata', '-1', '-af',
        'afade=t=in:st=0:d=0.03,afade=t=out:st=179.60:d=0.25',
        '-c:a', 'libmp3lame', '-q:a', '2', '-ar', '48000', '-ac', '2', str(target)
    ], check=True)
    edited = probe(target)
    length = float(edited['format']['duration'])
    if not 179.7 < length < 180.1 or target.stat().st_size > 5 * 1024 * 1024:
        raise RuntimeError('Unexpected club edit duration or payload')
    if sha(SOURCE) != EXPECTED:
        raise RuntimeError('Original audio must remain untouched')
    receipt = {
        'track': 'Industrial Pulse', 'ownerApprovedFor': 'NOVA',
        'sourcePath': str(SOURCE.relative_to(ROOT)), 'sourceSha256': EXPECTED,
        'sourceUnchanged': True, 'sourceDurationSeconds': duration,
        'removedIntroSeconds': CUT, 'fadeInSeconds': 0.03,
        'fadeOutSeconds': 0.25, 'runtimeDurationSeconds': length,
        'runtimePath': 'StreamingAssets/NOVA/IndustrialPulseClub.mp3',
        'runtimeSha256': sha(target), 'runtimeBytes': target.stat().st_size,
        'fetchedOnApproachToNOVA': True, 'automaticPlayback': True,
        'quietlyAudibleOutsideEntrance': True, 'continuousPartyTimeline': True,
        'otherTracksSelected': False,
        'runtimeAudioVerification': 'PENDING_UNITY_WEBGL_TEST'
    }
    (DEST / 'provenance.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print('NOVA_OWNER_AUDIO_EDIT_PREPARED', json.dumps(receipt))


if __name__ == '__main__':
    main()
