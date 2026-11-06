#!/usr/bin/env python3
"""Early C# syntax/local-name check; NOT a Unity/API/type compiler.

Checks ordinary local declarations, method parameters, foreach and catch names.
Ignores fields, pattern/out/deconstruction variables and declarations inside
preprocessor conditionals (both alternatives are parsed, not compiled).
Lambda/local-function declaration spaces are independent, as in Unity 6 C#.
"""
import argparse
import re
from collections import defaultdict
from pathlib import Path

from tree_sitter import Language, Parser
import tree_sitter_c_sharp

LANGUAGE = Language(tree_sitter_c_sharp.language())
PARSER = Parser(LANGUAGE)

FUNCTIONS = {
    'method_declaration', 'constructor_declaration', 'destructor_declaration',
    'operator_declaration', 'conversion_operator_declaration',
    'accessor_declaration', 'local_function_statement', 'lambda_expression',
    'anonymous_method_expression',
}
SPACES = {'block', 'for_statement', 'foreach_statement', 'using_statement',
          'fixed_statement', 'catch_clause', 'switch_body'}
LOCAL_PARENTS = {'local_declaration_statement', 'for_statement',
                 'using_statement', 'fixed_statement'}

# Minimal symbol-resolution preflight for the exact Unity 6 compilation
# failures observed in estate runs #42231496594 and #42236342801. The parser
# ignores comments and string literals; this is NOT a complete C# type checker.
USING_SYSTEM = re.compile(r'^\s*(?:global\s+)?using\s+System\s*;', re.MULTILINE)
USING_UNITY = re.compile(r'^\s*(?:global\s+)?using\s+UnityEngine\s*;', re.MULTILINE)
USING_DIAGNOSTICS = re.compile(r'^\s*(?:global\s+)?using\s+System\.Diagnostics\s*;', re.MULTILINE)
SYSTEM_TYPES = {
    'StringComparison': 'CS0103', 'InvalidOperationException': 'CS0246',
    'ArgumentException': 'CS0246', 'ArgumentNullException': 'CS0246',
    'NotSupportedException': 'CS0246', 'Exception': 'CS0246',
    'StringComparer': 'CS0103', 'StringSplitOptions': 'CS0103',
    'Guid': 'CS0246', 'TimeSpan': 'CS0246', 'DateTime': 'CS0246',
    'BitConverter': 'CS0103', 'Convert': 'CS0103',
}


def walk(node):
    yield node
    for child in node.named_children:
        yield from walk(child)


def parents(node):
    while node is not None:
        yield node
        node = node.parent


def key(node):
    return (node.start_byte, node.end_byte, node.type)


def check_source(source, filename='<source>'):
    data = source.encode('utf-8') if isinstance(source, str) else source
    tree = PARSER.parse(data)
    root = tree.root_node
    errors = []
    names = defaultdict(list)
    for node in walk(root):
        if node.type == 'ERROR' or node.is_missing:
            errors.append(f'{filename}:{node.start_point.row + 1}: CSHARP_SYNTAX ({node.type})')
        name = None
        if node.type == 'variable_declarator':
            declaration = node.parent
            if declaration and declaration.parent and declaration.parent.type in LOCAL_PARENTS:
                name = node.child_by_field_name('name')
        elif node.type in {'parameter', 'catch_declaration'}:
            name = node.child_by_field_name('name')
        elif node.type == 'foreach_statement':
            name = node.child_by_field_name('left')
        if name is None or name.type != 'identifier':
            continue
        ancestors = list(parents(node))
        if any(n.type in {'preproc_if', 'preproc_elif', 'preproc_else'} for n in ancestors):
            continue  # conditional compilation must be checked by Unity
        function = next((n for n in ancestors if n.type in FUNCTIONS), None)
        if function is None:
            continue  # fields, indexer parameters etc. are not local variables
        chain = []
        for n in ancestors:
            if n == function:
                chain.append(key(n))
                break
            if n.type in SPACES:
                chain.append(key(n))
        spelling = data[name.start_byte:name.end_byte].decode('utf-8').lstrip('@')
        names[(key(function), spelling)].append((chain, name.start_point.row + 1))
    for (_, spelling), declarations in names.items():
        for i, (left, line) in enumerate(declarations):
            for right, other_line in declarations[i + 1:]:
                if left[0] in right or right[0] in left:
                    code = 'CS0128/CS0136'
                    errors.append(f'{filename}:{line}: {code}: local {spelling!r} conflicts with declaration at line {other_line}')

    # Unqualified types used without their System namespace, or conflicting
    # UnityEngine / .NET names, previously escaped this preflight entirely.
    source_text = data.decode('utf-8')
    has_system = bool(USING_SYSTEM.search(source_text))
    has_unity = bool(USING_UNITY.search(source_text))
    has_diagnostics = bool(USING_DIAGNOSTICS.search(source_text))
    aliases = set(re.findall(
        r'^\s*(?:global\s+)?using\s+([A-Za-z_][A-Za-z_0-9]*)\s*=',
        source_text, re.MULTILINE))
    name_types = {'class_declaration', 'struct_declaration', 'enum_declaration',
                  'interface_declaration', 'record_declaration'}
    for node in walk(root):
        if node.type != 'identifier':
            continue
        symbol = data[node.start_byte:node.end_byte].decode('utf-8')
        if symbol in aliases:
            continue
        # E.g. System.InvalidOperationException and UnityEngine.Object do
        # not need an imported namespace and are never ambiguous.
        prefix = data[:node.start_byte].rstrip()
        if prefix.endswith(b'.') or prefix.endswith(b'::'):
            continue
        if any(p.type in {'using_directive', 'comment', 'string_literal'} for p in parents(node)):
            continue
        if node.parent and node.parent.type in name_types and node.parent.child_by_field_name('name') == node:
            continue
        line = node.start_point.row + 1
        if symbol in SYSTEM_TYPES and not has_system:
            errors.append(f'{filename}:{line}: {SYSTEM_TYPES[symbol]}: unqualified {symbol} requires using System; or System.{symbol}')
        if symbol in {'Object', 'Random'} and has_system and has_unity:
            errors.append(f'{filename}:{line}: CS0104: {symbol} ambiguous between System and UnityEngine; use an explicit alias or fully qualified name')
        if symbol == 'Debug' and has_diagnostics and has_unity:
            errors.append(f'{filename}:{line}: CS0104: Debug ambiguous between System.Diagnostics and UnityEngine; use an explicit alias')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', nargs='?', type=Path, default=Path('unity-city/Assets'))
    args = parser.parse_args()
    files = sorted(args.root.rglob('*.cs')) if args.root.is_dir() else []
    if not files:
        parser.error(f'No C# sources found under {args.root}; check checkout paths')
    errors = []
    for path in files:
        errors.extend(check_source(path.read_bytes(), str(path)))
    for error in errors:
        print('::error::' + error)
    print(f'CSHARP_SCOPE_CHECK files={len(files)} errors={len(errors)}; Unity compilation still required')
    return bool(errors)


if __name__ == '__main__':
    raise SystemExit(main())
