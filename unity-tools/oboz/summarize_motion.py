"""Summarize real Unity motion-review output; never accept a fix automatically."""
import argparse
import json
import math
from pathlib import Path


def summarize(root, expected_sha):
    report = json.loads((root / 'motion-review.json').read_text())
    if report.get('commit') != expected_sha or not report.get('complete'):
        raise ValueError('Incomplete report or unexpected source commit')
    if report.get('graphics') == 'Null' or report.get('phoneTested'):
        raise ValueError('Invalid diagnostic provenance')
    records = []
    for case in report['cases']:
        feet, knees = [], []
        for frame in case['frames']:
            for side in ('left', 'right'):
                if frame[side + 'FootMeasurable']:
                    feet.append(frame[side + 'FootYaw'])
                knees.append(frame[side + 'KneePlane'])
        if not feet or not all(math.isfinite(v) for v in feet + knees):
            raise ValueError('Missing or nonfinite joint measurements')
        def stats(values):
            values = sorted(values)
            return dict(samples=len(values), median=round(values[len(values)//2], 2),
                        p95=round(values[math.ceil(.95*len(values))-1], 2),
                        maximum=round(values[-1], 2))
        records.append(dict(clip=case['clip'], variant=case['variant'],
                            feet=stats(feet), knees=stats(knees)))
    return dict(commit=expected_sha, acceptedFix=False, phoneTested=False, cases=records)


def contact_sheets(root, output, cases):
    from PIL import Image, ImageDraw
    output.mkdir(parents=True, exist_ok=True)
    for clip in dict.fromkeys(c['clip'] for c in cases):
        variants=[c['variant'] for c in cases if c['clip']==clip]
        for view in ('front', 'back', 'side'):
            sheet=Image.new('RGB', (6*192, len(variants)*266), '#20242b')
            draw=ImageDraw.Draw(sheet)
            for row, variant in enumerate(variants):
                for column, frame in enumerate(range(0, 60, 10)):
                    path=root/f'{variant}-{clip}-{view}-{frame:02}.png'
                    with Image.open(path) as original:
                        image=original.convert('RGB').resize((192, 240))
                        sheet.paste(image, (column*192, row*266+26))
                    draw.text((column*192+4, row*266+6), f'{variant} f{frame:02}', fill='white')
            sheet.save(output/f'{clip}-{view}.jpg', quality=94)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('--sha', required=True)
    parser.add_argument('--sheets', type=Path)
    args=parser.parse_args()
    result=summarize(args.root, args.sha)
    print(json.dumps(result, indent=2))
    if args.sheets:
        contact_sheets(args.root, args.sheets, result['cases'])
