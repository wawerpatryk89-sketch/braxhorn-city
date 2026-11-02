#!/usr/bin/env python3
"""Polskie kwestie osiedla (offline). Wymaga piper-tts==1.3.0 i ffmpeg (libvorbis, aac).

Modele: https://huggingface.co/rhasspy/piper-voices (pl/pl_PL/*) -> katalog --models:
  pl_PL-<glos>-<jakosc>.onnx + .onnx.json (+ MODEL_CARD jako pl_PL-<glos>-<jakosc>.MODEL_CARD).
Licencje (model cards, rewizja e7229f7d33a10e145e109b886d661984201ac3c4, 2.11.2026):
  bass-high        — Apache 2.0 (fine-tune en_US-lessac-high, ~10 tys. segmentów polskiej mowy)
  darkman-medium   — zbiór CC0 (github.com/OHF-Voice/voice-datasets), fine-tune en_US-lessac-medium
  gosia-medium     — zbiór CC0 (github.com/OHF-Voice/voice-datasets), fine-tune en_US-lessac-medium
  mc_speech-medium — zbiór CC0 (kaggle czyzi0/the-mc-speech-dataset), fine-tune en_US-lessac-medium
  mls_6892-low     — zbiór CC BY 4.0 (openslr.org/94, wymaga podania autorstwa), fine-tune en_US-ryan-low
  Uwaga: wszystkie są dostrajane z angielskich punktów startowych lessac/ryan — licencję bazowego zbioru
  (lessac: Blizzard 2011) właściciel powinien sprawdzić przed wydaniem komercyjnym.
Modele i Piper to narzędzia — nie trafiają do gry; do gry idą tylko nagrania Ogg.

Użycie:
  --probki PLIK.m4a           te same 4 kwestie każdym głosem z --models (zapowiedzi „głos 1…”) + obecne nagrania
  --glos-m X --glos-f Y       wygeneruj wszystkie kwestie (X, Y = np. darkman-medium, gosia-medium);
                              --nadpisz zastępuje istniejące nagrania (po wyborze głosu: jedno polecenie)
  --check                     pokrycie kwestii + dekodowanie każdego Ogg
Ustawienia na wyraźność: length_scale 0.9, noise_scale 0.5, noise_w 0.6; obróbka ffmpeg: highpass 80 Hz,
podbicie 2–4 kHz (+3 dB przy 3 kHz), loudnorm −16 LUFS (dwa przejścia, liniowo), Ogg Vorbis q6 (Unity: jakość 1.0
albo PCM dla krótkich — BraxGlosyImport, bez podwójnej mocnej kompresji).
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile
import uuid
import wave


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'unity-city/Assets/Resources/OsiedleGlosy'
WYRAZNOSC = {'length_scale': 0.9, 'noise_scale': 0.5, 'noise_w_scale': 0.6}
FILTR = 'highpass=f=80,equalizer=f=3000:t=o:w=1.2:g=3'
PROBKI = [
    'Dzień dobry! W czym mogę pomóc?',
    'Słyszałeś? Za kasynem jest siłownia BRAXHORN. Podobno sprzęt jak w zawodowym klubie.',
    'Na Poznańskiej zrobili rondo. W końcu nie stoi się w korku pod tunelem.',
    'Ej! Zostaw go!',
]
LICZEBNIKI = ['jeden', 'dwa', 'trzy', 'cztery', 'pięć', 'sześć', 'siedem', 'osiem', 'dziewięć', 'dziesięć']


def dialogue():
    runtime = ROOT / 'unity-city/Assets/Claude/Runtime'
    text = (runtime / 'BraxDymki.cs').read_text()
    parts = [text[text.index('public static readonly string[] Powitania'):]]
    text = (runtime / 'BraxLudzie.cs').read_text()
    parts.append(text[text.index('public static readonly string[] Okrzyki'):])
    text = (runtime / 'BraxMieszkaniec.cs').read_text()
    text = text[text.index('string Powitanie()'):text.index('// AKCJA:')]
    parts += re.findall(r'new\[\] \{([^}]+)\}', text)
    texts = sorted(set(re.findall(r'"([^"\n]+)"', '\n'.join(parts))))
    return [{'tekst': t, 'id': hashlib.sha256(t.encode()).hexdigest()[:16]} for t in texts]


def synteza(voice, tekst, wav_path, wyrazny=True):
    from piper import SynthesisConfig
    cfg = SynthesisConfig(**WYRAZNOSC) if wyrazny else None
    with wave.open(str(wav_path), 'wb') as wav:
        voice.synthesize_wav(tekst, wav, syn_config=cfg)


def glosnosc(wejscie, filtr):
    """Pierwsze przejście loudnorm: pomiar (json z ostatnich linii stderr)."""
    r = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', str(wejscie), '-af',
                        filtr + ',loudnorm=I=-16:TP=-1.5:LRA=11:print_format=json', '-f', 'null', '-'],
                       check=True, capture_output=True, text=True)
    return json.loads(r.stderr[r.stderr.rindex('{'):r.stderr.rindex('}') + 1])


def obrobka(wejscie, wyjscie, kodek, rate, filtr=FILTR):
    m = glosnosc(wejscie, filtr)
    ln = ('loudnorm=I=-16:TP=-1.5:LRA=11:linear=true:measured_I={input_i}:measured_TP={input_tp}:'
          'measured_LRA={input_lra}:measured_thresh={input_thresh}:offset={target_offset}').format(**m)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wejscie), '-af', filtr + ',' + ln,
                    '-ar', str(rate), '-ac', '1'] + kodek + [str(wyjscie)], check=True)


def glosy_w(models):
    return sorted(p.name[len('pl_PL-'):-len('.onnx')] for p in models.glob('pl_PL-*.onnx'))


def probki(models, cel):
    from piper import PiperVoice
    glosy = glosy_w(models)
    if not glosy:
        raise SystemExit('brak modeli pl_PL-*.onnx w ' + str(models))
    zapowiedz = PiperVoice.load(str(models / ('pl_PL-gosia-medium.onnx' if (models / 'pl_PL-gosia-medium.onnx').exists()
                                               else 'pl_PL-' + glosy[0] + '.onnx')))
    opis = []
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp); n = 0; lista = []

        def dodaj(wav, filtr=FILTR):
            nonlocal n
            out = tmp / f'k{n:03d}.wav'; n += 1
            obrobka(wav, out, ['-c:a', 'pcm_s16le'], 44100, filtr)
            lista.append(out)

        def cisza(sek):
            nonlocal n
            out = tmp / f'k{n:03d}.wav'; n += 1
            subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'lavfi', '-i', 'anullsrc=r=44100:cl=mono', '-t', str(sek),
                            '-c:a', 'pcm_s16le', str(out)], check=True)
            lista.append(out)

        for i, g in enumerate(glosy):
            voice = PiperVoice.load(str(models / ('pl_PL-' + g + '.onnx')))
            z = tmp / 'zapowiedz.wav'
            synteza(zapowiedz, f'Głos {LICZEBNIKI[i]}.', z, wyrazny=False)
            dodaj(z, 'anull'); cisza(.6)
            for t in PROBKI:
                w = tmp / 'kw.wav'
                synteza(voice, t, w)
                dodaj(w); cisza(.7)
            cisza(.8)
            opis.append(f'głos {i + 1} = {g}')
        # dla porównania: obecne nagrania z gry (darkman/gosia medium, ustawienia domyślne, Ogg q3)
        z = tmp / 'zapowiedz.wav'
        synteza(zapowiedz, 'Dla porównania: obecne nagrania z gry.', z, wyrazny=False)
        dodaj(z, 'anull'); cisza(.6)
        for t in PROBKI:
            for plec in ('m', 'f'):
                stary = OUTPUT / (hashlib.sha256(t.encode()).hexdigest()[:16] + '_' + plec + '.ogg')
                if stary.exists():
                    dodaj(stary, 'anull'); cisza(.5)
        opis.append('na końcu: obecne nagrania z gry (męski darkman, żeński gosia — bez obróbki)')
        spis = tmp / 'spis.txt'
        spis.write_text(''.join(f"file '{p}'\n" for p in lista))
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(spis),
                        '-c:a', 'aac', '-b:a', '128k', '-ar', '44100', '-ac', '1', str(cel)], check=True)
    print('PRÓBKI: ' + str(cel) + ' — ' + '; '.join(opis))


def generuj(models, glos_m, glos_f, nadpisz):
    from piper import PiperVoice
    entries = dialogue()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / 'indeks.json').write_text(json.dumps({'kwestie': entries}, ensure_ascii=False, indent=2))
    with tempfile.TemporaryDirectory() as tmp:
        wav_path = Path(tmp) / 'voice.wav'
        for gender, model in (('m', glos_m), ('f', glos_f)):
            voice = PiperVoice.load(str(models / f'pl_PL-{model}.onnx'))
            for e in entries:
                destination = OUTPUT / (e['id'] + '_' + gender + '.ogg')
                if not nadpisz and destination.exists() and destination.stat().st_size > 0:
                    continue
                synteza(voice, e['tekst'], wav_path)
                obrobka(wav_path, destination, ['-c:a', 'libvorbis', '-q:a', '6'], 22050)
    for path in OUTPUT.iterdir():
        if path.suffix in ('.ogg', '.json'):
            meta = Path(str(path) + '.meta')
            if not meta.exists():
                meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')
    print(f'GŁOSY: {len(entries)} kwestii × 2 (m={glos_m}, f={glos_f}, nadpisz={nadpisz})')


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--models', type=Path)
    parser.add_argument('--check', action='store_true', help='Check dialogue coverage and decode every Ogg')
    parser.add_argument('--probki', type=Path, help='plik .m4a/.mp3 z próbkami porównawczymi')
    parser.add_argument('--glos-m', default='darkman-medium')
    parser.add_argument('--glos-f', default='gosia-medium')
    parser.add_argument('--nadpisz', action='store_true', help='zastąp istniejące nagrania')
    args = parser.parse_args()
    entries = dialogue()
    if args.check:
        saved = json.loads((OUTPUT / 'indeks.json').read_text())['kwestie']
        assert saved == entries, 'Dialogue index is out of date'
        for e in entries:
            for gender in ('m', 'f'):
                path = OUTPUT / (e['id'] + '_' + gender + '.ogg')
                assert path.is_file() and Path(str(path) + '.meta').is_file(), path
                subprocess.run(['ffmpeg', '-v', 'error', '-xerror', '-i', str(path), '-f', 'null', '-'], check=True, capture_output=True)
        print(f'PASS: {len(entries)} phrases, {len(entries) * 2} decodable Polish recordings')
        return
    if not args.models:
        parser.error('--models is required for generation')
    if args.probki:
        probki(args.models, args.probki)
        return
    generuj(args.models, args.glos_m, args.glos_f, args.nadpisz)


if __name__ == '__main__':
    main()
