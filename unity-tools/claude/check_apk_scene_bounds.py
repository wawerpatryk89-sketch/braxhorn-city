#!/usr/bin/env python3
"""Check ZIP CRC and Unity 6 serialized object ranges; this is NOT a runtime test.

Format reference: AssetStudio/SerializedFile.cs (Perfare/AssetStudio).
Only format 22 is supported deliberately: unknown formats fail explicitly.
No extraction or changes to the APK, and no model/texture contents in the report.
"""
import argparse
import collections
import hashlib
import json
import pathlib
import re
import struct
import zipfile


class Reader:
    def __init__(self, data, endian='<'):
        self.data, self.pos, self.endian = data, 0, endian

    def take(self, size):
        if size < 0 or self.pos + size > len(self.data):
            raise ValueError(f'metadata read outside file at {self.pos}, size {size}')
        out = self.data[self.pos:self.pos + size]
        self.pos += size
        return out

    def number(self, fmt):
        return struct.unpack(self.endian + fmt, self.take(struct.calcsize(fmt)))[0]

    def count(self):
        n = self.number('i')
        if not 0 <= n <= len(self.data):
            raise ValueError(f'invalid metadata count {n}')
        return n

    def string(self):
        end = self.data.find(b'\0', self.pos)
        if end < 0:
            raise ValueError('unterminated metadata string')
        return self.take(end - self.pos + 1)[:-1].decode('utf-8')


def inspect_scene(data, actual_size):
    r = Reader(data, '>')
    r.take(8)
    version = r.number('I')
    r.take(4)
    endian = r.number('B')
    r.take(3)
    if version != 22 or endian not in (0, 1):
        raise ValueError(f'unsupported serialized format/endian: {version}/{endian}')
    metadata_size, file_size, offset = r.number('I'), r.number('q'), r.number('q')
    r.take(8)
    if file_size != actual_size or not 48 <= offset <= file_size or metadata_size > offset:
        raise ValueError(f'invalid header: file={file_size}, actual={actual_size}, data={offset}, metadata={metadata_size}')
    r.endian = '<' if endian == 0 else '>'
    unity, platform, trees = r.string(), r.number('i'), r.number('B')
    if trees not in (0, 1):
        raise ValueError('invalid type tree flag')
    types = []
    for _ in range(r.count()):
        class_id = r.number('i')
        r.take(3)  # stripped flag, script index
        if class_id == 114:
            r.take(16)  # script hash
        r.take(16)  # type hash
        if trees:
            nodes, strings = r.count(), r.count()
            r.take(nodes * 32 + strings)
            r.take(r.count() * 4)
        types.append(class_id)
    objects = r.count()
    by_type, ranges, ids = collections.Counter(), [], set()
    for _ in range(objects):
        r.take((-r.pos) % 4)
        path_id, start, size, type_id = r.number('q'), r.number('q'), r.number('I'), r.number('i')
        if path_id in ids or not 0 <= type_id < len(types):
            raise ValueError(f'invalid/duplicate object {path_id}, type {type_id}')
        ids.add(path_id)
        if start < 0 or offset + start + size > file_size:
            raise ValueError(f'object {path_id} extends outside file: start={offset + start}, size={size}, file={file_size}')
        ranges.append((start, start + size, path_id))
        by_type[types[type_id]] += 1
    if r.pos > offset:
        raise ValueError('object table overlaps data')
    end = 0
    for start, stop, path_id in sorted(ranges):
        if start < end:
            raise ValueError(f'overlapping object {path_id}')
        end = stop
    return dict(unity=unity, platform=platform, format=version, bytes=file_size,
                type_trees=bool(trees), types=len(types), objects=objects,
                objects_by_class=dict(sorted(by_type.items())))


def inspect_apk(path):
    report = {'apk': path.name, 'sha256': '', 'scenes': {}, 'errors': [],
              'scope': 'ZIP CRC and serialized object ranges only; runtime deserialization NOT tested'}
    with path.open('rb') as stream:
        report['sha256'] = hashlib.file_digest(stream, 'sha256').hexdigest()
    with zipfile.ZipFile(path) as apk:
        bad = apk.testzip()
        if bad:
            report['errors'].append('ZIP CRC failure: ' + bad)
        groups = collections.defaultdict(list)
        for entry in apk.infolist():
            match = re.fullmatch(r'(assets/bin/Data/(?:level\d+|sharedassets\d+\.assets|resources\.assets|globalgamemanagers(?:\.assets)?))(?:\.split(\d+))?', entry.filename)
            if not match:
                continue
            groups[match[1]].append((int(match[2]) if match[2] is not None else -1, entry))
        for name, parts in sorted(groups.items()):
            try:
                parts.sort(key=lambda p: p[0])
                indices = [p[0] for p in parts]
                if indices != [-1] and indices != list(range(len(parts))):
                    raise ValueError(f'missing, duplicated or mixed split segments: {indices}')
                actual_size = sum(entry.file_size for _, entry in parts)
                data, need = bytearray(), 48
                for _, entry in parts:
                    with apk.open(entry) as stream:
                        while len(data) < need:
                            block = stream.read(need - len(data))
                            if not block:
                                break
                            data.extend(block)
                            if len(data) == 48:
                                need = struct.unpack_from('>q', data, 32)[0]
                                if not 48 <= need <= min(actual_size, 128 * 1024 * 1024):
                                    raise ValueError(f'invalid/unsupported metadata offset {need}')
                    if len(data) == need and need > 48:
                        break
                report['scenes'][name] = inspect_scene(bytes(data), actual_size)
                report['scenes'][name]['segments'] = len(parts)
            except (ValueError, UnicodeError, struct.error) as exc:
                report['errors'].append(name + ': ' + str(exc))
        if 'assets/bin/Data/level1' not in report['scenes']:
            report['errors'].append('level1 not successfully inspected')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('apk', type=pathlib.Path)
    args = parser.parse_args()
    report = inspect_apk(args.apk)
    print(json.dumps(report, indent=2))
    return int(bool(report['errors']))


if __name__ == '__main__':
    raise SystemExit(main())
