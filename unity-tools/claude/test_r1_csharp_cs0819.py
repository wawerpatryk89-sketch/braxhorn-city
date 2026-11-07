"""MIASTO R1: prevent C# CS0819 from invalid multi-declarator var statements.

Fast source-only check. It does not build APK, run Unity, or modify assets.
The checkout for android-build-tools-check.yml includes Assets/Editor.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city" / "Assets"

# Preserve offsets and newlines while ignoring strings and comments.
LITERALS = re.compile(
    r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/'
)
IMPLICIT_DECL = re.compile(r'\bvar\s+[_A-Za-z][_A-Za-z0-9]*\s*=')
NEXT_DECL = re.compile(r'\s*[_A-Za-z][_A-Za-z0-9]*\s*=')


def implicit_multi_declarations(source):
    clean = LITERALS.sub(lambda m: re.sub(r'[^\n]', ' ', m.group()), source)
    errors = []
    for match in IMPLICIT_DECL.finditer(clean):
        start = match.end()
        depth = {"(": 0, "[": 0, "{": 0}
        for index in range(start, len(clean)):
            ch = clean[index]
            if ch in depth:
                depth[ch] += 1
            elif ch in ")]}":
                opener = {")": "(", "]": "[", "}": "{"}[ch]
                if depth[opener]:
                    depth[opener] -= 1
            if all(value == 0 for value in depth.values()):
                if ch == ";":
                    break
                if ch == "," and NEXT_DECL.match(clean, index + 1):
                    line = clean.count("\n", 0, match.start()) + 1
                    errors.append(line)
                    break
    return errors


class R1CSharpCS0819Regression(unittest.TestCase):
    def test_known_build_109_errors_stay_fixed(self):
        filenames = (
            "Editor/Claude/Region/BraxSupermarket.cs",
            "Editor/Claude/Region/BraxMiastoWyspaDomki.cs",
            "Editor/Claude/Region/BraxRegionRuchMiasta.cs",
        )
        for name in filenames:
            with self.subTest(file=name):
                self.assertEqual(
                    implicit_multi_declarations((ASSETS / name).read_text(encoding="utf-8")),
                    [],
                    msg=f"CS0819 in {name}",
                )

    def test_scan_all_available_editor_sources(self):
        root = ASSETS / "Editor"
        self.assertTrue(root.is_dir(), "Editor C# sources were not checked out")
        failures = []
        count = 0
        for file in sorted(root.rglob("*.cs")):
            count += 1
            for line in implicit_multi_declarations(file.read_text(encoding="utf-8-sig")):
                failures.append(f"{file.relative_to(ASSETS)}:{line}: CS0819 (split var declarations)")
        self.assertGreater(count, 0)
        self.assertFalse(failures, "\n".join(failures[:50]))


if __name__ == "__main__":
    unittest.main()
