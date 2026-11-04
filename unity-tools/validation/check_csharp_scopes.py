#!/usr/bin/env python3
"""Early C# syntax/local-name check; NOT a Unity/API/type compiler.

Checks ordinary local declarations, method parameters, foreach and catch names.
Ignores fields, pattern/out/deconstruction variables and declarations inside
preprocessor conditionals (both alternatives are parsed, not compiled).
Lambda/local-function declaration spaces are independent, as in Unity 6 C#.
"""
import argparse
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
