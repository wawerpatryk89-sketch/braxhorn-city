"""Verify an existing Unity artifact, then optionally create a test-only branch.

No Unity build or production deployment is performed here. Without --publish,
the command only downloads/verifies artifacts and writes a validation receipt.
--publish is accepted only in the dedicated GitHub Actions workflow.

The reviewed request file (authored separately) has this exact schema:
{
  "run_id": 123456789,
  "source_sha": "<40 lowercase hexadecimal characters>",
  "source_branch": "test/braxhorn-kickboxing-reference-20261029",
  "target_branch": "deployment/braxhorn-kickboxing-<source_sha[:12]>",
  "target_path": "public/unity-gym",
  "publication_mode": "qa-passed"
}

This automated path has no incomplete-QA override. A deliberately incomplete
review must be handled separately and must retain its actual failed/pending QA.
An unapproved, uninstalled billboard does not prevent this test publication.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile
import zlib
from datetime import datetime, timezone


REPOSITORY = 'wawerpatryk89-sketch/braxhorn-city'
SOURCE_BRANCH = 'test/braxhorn-kickboxing-reference-20261029'
BUILD_WORKFLOW = '.github/workflows/unity-kickboxing-reference-test.yml'
PUBLISH_WORKFLOW = '.github/workflows/unity-kickboxing-publish-test.yml'
TARGET_PATH = 'public/unity-gym'
SCOPE = 'isolated-kickboxing-room-and-annex-test'
API = 'https://api.github.com/repos/' + REPOSITORY
REMOTE = 'https://github.com/' + REPOSITORY + '.git'
HEX40 = re.compile(r'[0-9a-f]{40}')
HEX64 = re.compile(r'[0-9a-f]{64}')
MAX_ARCHIVE_BYTES = 4 * 1024**3
CORE_CHECKS = {
    'Actual compiled Unity player initializes',
    'Exact compiled source commit',
    'Original animated Brax bones remain present',
    'Existing iOS streaming algorithm retained',
    'Room references are assigned',
    'Two real active touches move and orbit independently',
    'Original jump lifts and lands without penetration',
    'Original Punch moves character bones',
    'Original Kick moves character bones',
    'Ring reached by physical stairs without jumping',
    'Directed punch swings the hanging bag',
    'Kick also contacts the original training system',
    'Bag oscillation loses energy after contact',
    'Real reflection renders animated player layer',
    'Door opens through existing interaction: gym-backrooms',
    'Door opens through existing interaction: gym-locker',
    'Door opens through existing interaction: gym-wc',
    'Locker room entered through its separate door',
    'WC entered through corridor and separate door',
    'No sampled training viewpoint sees WC with all doors open',
    'WC fixture blocks sustained character movement',
    'Backrooms return to main hall',
    'Physical exit reaches existing city',
    'Physical city to gym return remains playable',
    'Explicit reload restarts same Unity build',
    'No Unity runtime, shader or page exceptions',
    'Local frame timing and memory measurements recorded',
    'Actual recorded Unity walkthrough video',
}


class PublicationError(RuntimeError):
    pass


def require(condition, message):
    if not condition:
        raise PublicationError(message)


def utc_now():
    return datetime.now(timezone.utc).isoformat()


def read_json(path):
    with Path(path).open(encoding='utf-8') as stream:
        return json.load(stream)


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2, sort_keys=True) + '\n', encoding='utf-8')
    temporary.replace(path)


def sha256(path):
    value = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024**2), b''):
            value.update(chunk)
    return value.hexdigest()


def safe_relative(name):
    require(isinstance(name, str) and name and '\\' not in name and '\x00' not in name,
            'Invalid artifact relative path')
    path = PurePosixPath(name)
    require(not path.is_absolute() and path.as_posix() == name
            and not any(part in ('..', '.', '.git') or ':' in part for part in path.parts),
            'Unsafe artifact relative path: ' + name)
    return path


def validate_request(request):
    expected = {'run_id', 'source_sha', 'source_branch', 'target_branch', 'target_path', 'publication_mode'}
    require(isinstance(request, dict) and set(request) == expected,
            'Publication request must use the exact documented schema')
    require(type(request['run_id']) is int and request['run_id'] > 0, 'run_id must be a positive integer')
    source = request['source_sha']
    require(isinstance(source, str) and HEX40.fullmatch(source), 'source_sha must be an exact full lowercase SHA')
    require(request['source_branch'] == SOURCE_BRANCH, 'Only the existing kickboxing test source branch is allowed')
    require(request['target_branch'] == 'deployment/braxhorn-kickboxing-' + source[:12],
            'Target must be the exact source-specific deployment/braxhorn-kickboxing namespace')
    require(request['target_path'] == TARGET_PATH, 'Only public/unity-gym may be copied or committed')
    require(request['publication_mode'] == 'qa-passed', 'This automated workflow requires completed passing QA')
    return request


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, file, code, message, headers, new_url):
        return None


class Github:
    def __init__(self, token):
        require(bool(token), 'GH_TOKEN is required for read-only artifact access and the optional test-branch push')
        self.token = token
        self.opener = urllib.request.build_opener(NoRedirect)

    def request(self, suffix):
        # The API origin is fixed. Credentials are never sent to artifact storage.
        return urllib.request.Request(API + suffix, headers={
            'Authorization': 'Bearer ' + self.token,
            'Accept': 'application/vnd.github+json',
            'X-GitHub-Api-Version': '2022-11-28',
            'User-Agent': 'braxhorn-kickboxing-test-publication',
        })

    def json(self, suffix):
        try:
            with self.opener.open(self.request(suffix), timeout=45) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            raise PublicationError('GitHub metadata request failed with HTTP ' + str(error.code)) from None
        except urllib.error.URLError:
            raise PublicationError('GitHub metadata request failed; no credentials or response body logged') from None

    def download(self, artifact, output):
        try:
            response = self.opener.open(self.request('/actions/artifacts/' + str(artifact['id']) + '/zip'), timeout=60)
        except urllib.error.HTTPError as error:
            require(error.code in (301, 302, 303, 307, 308),
                    'Artifact download failed with HTTP ' + str(error.code))
            location = error.headers.get('Location', '')
            target = urllib.parse.urlsplit(location)
            require(target.scheme == 'https' and target.hostname and not target.username and not target.password
                    and target.port in (None, 443) and not target.fragment
                    and any(target.hostname.endswith(suffix) for suffix in (
                        '.blob.core.windows.net', '.actions.githubusercontent.com', '.githubusercontent.com')),
                    'Artifact redirect did not point to approved HTTPS artifact storage')
            # A fresh request has no Authorization header. Signed URLs are not logged.
            try:
                response = self.opener.open(urllib.request.Request(location, headers={'Accept-Encoding': 'identity'}), timeout=90)
            except (urllib.error.HTTPError, urllib.error.URLError):
                raise PublicationError('Artifact storage download failed; signed URL omitted') from None
        except urllib.error.URLError:
            raise PublicationError('Artifact download failed; credentials omitted') from None
        total = 0
        with response, Path(output).open('wb') as stream:
            while chunk := response.read(1024**2):
                total += len(chunk)
                require(total <= MAX_ARCHIVE_BYTES, 'Artifact exceeds the publication archive limit')
                stream.write(chunk)
        digest = sha256(output)
        api_digest = artifact.get('digest')
        if api_digest is not None:
            require(api_digest == 'sha256:' + digest, 'Downloaded artifact digest differs from GitHub metadata')
        return {'id': artifact['id'], 'name': artifact['name'], 'zipBytes': total,
                'zipSha256': digest, 'githubDigestVerified': api_digest is not None}


def verified_run_and_artifacts(github, request):
    run_id, source = request['run_id'], request['source_sha']
    run = github.json('/actions/runs/' + str(run_id))
    require(run.get('status') == 'completed' and run.get('conclusion') == 'success',
            'The exact build and QA run must be completed successfully')
    require(run.get('head_sha') == source and run.get('head_branch') == SOURCE_BRANCH,
            'Build run SHA or source branch differs from the reviewed request')
    require(run.get('path') == BUILD_WORKFLOW and run.get('event') == 'push'
            and run.get('repository', {}).get('full_name') == REPOSITORY,
            'Build run did not originate from the expected repository and Unity test workflow')
    artifacts = []
    for page in range(1, 101):
        result = github.json('/actions/runs/' + str(run_id) + '/artifacts?per_page=100&page=' + str(page))
        batch = result.get('artifacts', [])
        require(isinstance(batch, list), 'Malformed GitHub artifact listing')
        artifacts.extend(batch)
        if len(batch) < 100:
            break
    else:
        raise PublicationError('Unexpectedly large artifact listing')
    selected = {}
    for kind in ('player', 'review'):
        name = 'unity-kickboxing-' + kind + '-' + source
        matches = [item for item in artifacts if item.get('name') == name]
        require(len(matches) == 1 and matches[0].get('expired') is False,
                'Expected exactly one unexpired artifact: ' + name)
        item = matches[0]
        identity = item.get('workflow_run', {})
        require(not identity or (identity.get('id') == run_id and identity.get('head_sha') == source),
                'Artifact workflow identity does not match the requested run')
        selected[kind] = item
    return run, selected


def extract_zip(archive, destination):
    destination.mkdir(parents=True, exist_ok=False)
    seen, total = set(), 0
    with zipfile.ZipFile(archive) as zipped:
        for entry in zipped.infolist():
            name = entry.filename.rstrip('/') if entry.is_dir() else entry.filename
            relative = safe_relative(name)
            require(name not in seen, 'Duplicate artifact ZIP path: ' + name)
            seen.add(name)
            mode = entry.external_attr >> 16
            require(not stat.S_ISLNK(mode) and (stat.S_IFMT(mode) in (0, stat.S_IFREG, stat.S_IFDIR)),
                    'Artifact ZIP contains a link or special file')
            require(not entry.flag_bits & 1, 'Encrypted artifacts are not supported')
            total += entry.file_size
            require(total <= MAX_ARCHIVE_BYTES, 'Expanded artifact exceeds the publication size limit')
            target = destination.joinpath(*relative.parts)
            if entry.is_dir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with zipped.open(entry) as source, target.open('xb') as output:
                    shutil.copyfileobj(source, output, 1024**2)
                target.chmod(0o644)


def artifact_files(root):
    require(root.is_dir() and not root.is_symlink(), 'Artifact root must be an ordinary directory')
    names = set()
    for item in root.rglob('*'):
        require(not item.is_symlink(), 'Artifact contains a symlink')
        if item.is_file():
            name = item.relative_to(root).as_posix()
            safe_relative(name)
            names.add(name)
        else:
            require(item.is_dir(), 'Artifact contains a special file')
    return names


def verify_transport(root, release, source):
    transport = read_json(root / 'transport.json')
    require(transport == release.get('transport'), 'External and embedded release transport manifests differ')
    require(transport.get('sourceCommit') == source, 'Transport source commit differs from the requested SHA')
    data_path = transport.get('dataPath')
    safe_relative(data_path)
    require(re.fullmatch(r'Build/' + source[:12] + r'_[^/]+\.data(?:\.unityweb|\.gz)?', data_path),
            'Unexpected compiled data path or version prefix')
    require(not (root / data_path).exists(), 'Unsliced data duplicates the iOS streaming payload')
    parts = transport.get('parts')
    require(isinstance(parts, list) and len(parts) > 0, 'Transport has no data parts')
    for field in ('sha256', 'uncompressedSha256'):
        require(isinstance(transport.get(field), str) and HEX64.fullmatch(transport[field]), 'Invalid transport digest')
    for field in ('bytes', 'uncompressedBytes'):
        require(type(transport.get(field)) is int and 0 < transport[field] <= MAX_ARCHIVE_BYTES, 'Invalid transport byte count')
    compressed, expanded = hashlib.sha256(), hashlib.sha256()
    compressed_bytes = expanded_bytes = 0
    inflater = zlib.decompressobj(31)
    for number, part in enumerate(parts):
        expected = data_path + '.part' + str(number).zfill(3)
        require(part.get('path') == expected, 'Transport parts must be contiguous and ordered')
        require(type(part.get('bytes')) is int and 0 < part['bytes'] <= 32 * 1024**2, 'Unexpected transport part size')
        path = root / expected
        require(path.is_file() and path.stat().st_size == part['bytes'], 'Missing or truncated transport part: ' + expected)
        require(part.get('sha256') == release['files'].get(expected) == sha256(path), 'Transport part hash mismatch: ' + expected)
        with path.open('rb') as stream:
            while chunk := stream.read(65536):
                compressed.update(chunk)
                compressed_bytes += len(chunk)
                while chunk:
                    decoded = inflater.decompress(chunk, 1024**2)
                    expanded.update(decoded)
                    expanded_bytes += len(decoded)
                    require(expanded_bytes <= transport['uncompressedBytes'], 'Gzip data exceeds its declared expanded length')
                    require(not inflater.unused_data, 'Transport includes bytes after the single gzip stream')
                    chunk = inflater.unconsumed_tail
    tail = inflater.flush()
    expanded.update(tail)
    expanded_bytes += len(tail)
    require(inflater.eof and not inflater.unused_data, 'Transport is not one complete gzip stream')
    require(compressed_bytes == transport['bytes'] and compressed.hexdigest() == transport['sha256'],
            'Concatenated compressed transport hash or byte count mismatch')
    require(expanded_bytes == transport['uncompressedBytes'] and expanded.hexdigest() == transport['uncompressedSha256'],
            'Expanded Unity data hash or byte count mismatch')
    loader = (root / 'city-transport.js').read_text(encoding='utf-8')
    embedded = re.findall(r'^\s*const manifest = (.+);\s*$', loader, flags=re.MULTILINE)
    require(len(embedded) == 1 and json.loads(embedded[0]) == transport, 'JavaScript transport manifest differs from verified bytes')
    require("const revision = 'ios-stream-01'" in loader and 'DecompressionStream' in loader, 'Existing streaming transport was not retained')
    fix = release.get('loadingFix', {})
    require(fix.get('revision') == 'ios-stream-01' and fix.get('algorithmPreserved') is True
            and fix.get('nativeStreamingGzip') is True and fix.get('maxInputChunkBytes') == 65536
            and fix.get('compressedBytes') == compressed_bytes and fix.get('uncompressedBytes') == expanded_bytes,
            'Release does not retain the verified iOS streaming contract')
    return {'parts': len(parts), 'compressedBytes': compressed_bytes, 'uncompressedBytes': expanded_bytes,
            'compressedSha256': compressed.hexdigest(), 'uncompressedSha256': expanded.hexdigest()}


def verify_player(root, source):
    names = artifact_files(root)
    release = read_json(root / 'release.json')
    require(release.get('scope') == SCOPE and release.get('sourceCommit') == source, 'Unexpected player scope or source commit')
    require(release.get('productionChanged') is False and release.get('physicalIPhoneValidated') is False,
            'The artifact must retain its test-only and non-device-validated status')
    require(release.get('billboardArtworkInstalled') is False,
            'This request covers the pending-artwork test mount, not an installed final billboard')
    files = release.get('files')
    require(isinstance(files, dict) and files and names == set(files) | {'release.json'},
            'Artifact file inventory differs from release.files; no unhashed extra files are accepted')
    require('release.json' not in files, 'A release manifest cannot authenticate its own hash recursively')
    required = {'index.html', 'city-transport.js', 'transport.json', 'playable.json',
                'kickboxing-layout.json', 'kickboxing-assets.json', 'bag-provenance.json', 'source-integrity.json'}
    require(required <= set(files), 'Compiled player is missing required manifests or startup files')
    for name, digest in files.items():
        safe_relative(name)
        require(isinstance(digest, str) and HEX64.fullmatch(digest), 'Invalid release file hash: ' + name)
        require((root / name).stat().st_size < 100 * 1024**2, 'File exceeds the GitHub individual-blob limit: ' + name)
        require(sha256(root / name) == digest, 'Release file hash mismatch: ' + name)
    for suffix in ('.loader.js', '.framework.js.unityweb', '.wasm.unityweb'):
        require(sum(name.startswith('Build/' + source[:12] + '_') and name.endswith(suffix) for name in files) == 1,
                'Expected one exact-version compiled asset ending in ' + suffix)
    game = read_json(root / 'playable.json')
    require(game.get('commit') == source and game.get('unity') == release.get('unityVersion')
            and game.get('originalBrax') is True, 'Compiled-player identity differs from release metadata')
    layout = read_json(root / 'kickboxing-layout.json')
    require(all(layout.get(key) is True for key in ('originalEntrancePreserved', 'originalRingPositionPreserved', 'privacyWithAllDoorsOpen')),
            'Required entrance, ring or architectural privacy check did not pass')
    require(type(layout.get('privacyRays')) is int and layout['privacyRays'] > 0
            and layout['privacyBlocked'] == layout['privacyRays'], 'Missing architectural privacy ray evidence')
    return release, verify_transport(root, release, source)


def verify_qa(root, source):
    artifact_files(root)
    candidates = [path for path in (root / 'report.json', root / 'kickboxing-review/report.json') if path.is_file()]
    require(len(candidates) == 1, 'Expected one report.json in the known review artifact layout')
    report_path = candidates[0]
    report = read_json(report_path)
    require(report.get('commit') == source and report.get('finished') is True
            and report.get('passed') is True and not report.get('error'),
            'QA is incomplete, failed or belongs to another commit; no automatic review bypass exists')
    checks = report.get('checks')
    require(isinstance(checks, list) and checks and all(check.get('passed') is True for check in checks), 'QA contains missing or failed checks')
    names = {check.get('name') for check in checks}
    require(CORE_CHECKS <= names, 'QA did not execute every required core check: ' + ', '.join(sorted(CORE_CHECKS - names)))
    require(report.get('physicalIPhoneValidated') is False, 'Desktop QA must not claim a physical iPhone test')
    require(report.get('walkthroughContinuous') is True, 'QA walkthrough contains recovery cuts after a failed traversal')
    evidence_root = report_path.parent
    evidence = {}
    for name in ('walkthrough.webm', 'browser.log', 'room-diagnostics.json', 'walkthrough-trace.json'):
        file = evidence_root / name
        require(file.is_file() and file.stat().st_size > 0, 'Missing runtime evidence: ' + name)
        evidence[name] = {'bytes': file.stat().st_size, 'sha256': sha256(file)}
    require(evidence['walkthrough.webm']['bytes'] > 4096, 'Recorded Unity walkthrough is empty')
    captures = sorted(evidence_root.glob('*.png'))
    require(len(captures) >= 10, 'Insufficient actual runtime screenshot evidence')
    evidence['screenshots'] = [{'path': path.name, 'bytes': path.stat().st_size, 'sha256': sha256(path)} for path in captures]
    return report, report_path, evidence


def git(args, cwd, token=None):
    env = {key: value for key, value in os.environ.items()
           if key not in ('GH_TOKEN', 'GITHUB_TOKEN') and not key.startswith(('GIT_CONFIG_', 'GIT_TRACE'))}
    configs = [('core.hooksPath', '/dev/null'), ('credential.helper', ''), ('commit.gpgsign', 'false')]
    if token:
        encoded = base64.b64encode(('x-access-token:' + token).encode()).decode()
        configs.append(('http.https://github.com/.extraheader', 'AUTHORIZATION: basic ' + encoded))
    env['GIT_CONFIG_COUNT'] = str(len(configs))
    for number, (key, value) in enumerate(configs):
        env['GIT_CONFIG_KEY_' + str(number)] = key
        env['GIT_CONFIG_VALUE_' + str(number)] = value
    env['GIT_TERMINAL_PROMPT'] = '0'
    result = subprocess.run(['git', *args], cwd=cwd, env=env, capture_output=True, text=True, timeout=600)
    require(result.returncode == 0, 'Git operation failed (' + args[0] + '); subprocess output omitted to protect credentials')
    return result.stdout.strip()


def publish_branch(player, workspace, request, token, receipt, receipt_path):
    require(os.environ.get('GITHUB_ACTIONS') == 'true'
            and os.environ.get('GITHUB_REPOSITORY') == REPOSITORY
            and os.environ.get('GITHUB_REF') == 'refs/heads/' + SOURCE_BRANCH
            and os.environ.get('GITHUB_EVENT_NAME') == 'push'
            and os.environ.get('GITHUB_WORKFLOW_REF') == REPOSITORY + '/' + PUBLISH_WORKFLOW + '@refs/heads/' + SOURCE_BRANCH,
            '--publish is restricted to the dedicated workflow on the existing test branch')
    source, branch = request['source_sha'], request['target_branch']
    ref = 'refs/heads/' + branch
    repository = workspace / 'publication-repository'
    repository.mkdir()
    git(['init', '--quiet', '--initial-branch=' + branch], repository)
    git(['remote', 'add', 'origin', REMOTE], repository)
    require(git(['remote', 'get-url', 'origin'], repository) == REMOTE, 'Unexpected push remote')
    require(not git(['ls-remote', '--heads', 'origin', ref], repository, token), 'Target test branch already exists; it will not be overwritten')
    git(['config', 'remote.origin.promisor', 'true'], repository)
    git(['config', 'remote.origin.partialclonefilter', 'blob:none'], repository)
    git(['fetch', '--no-tags', '--depth=1', '--filter=blob:none', 'origin', source], repository, token)
    require(git(['rev-parse', 'FETCH_HEAD'], repository) == source, 'Fetched base differs from the requested source commit')
    # Populate the source index without checking out/downloading its old player.
    # Every other source-tree entry survives byte-for-byte in the new commit.
    git(['update-ref', ref, source], repository)
    git(['read-tree', source], repository)
    git(['rm', '--cached', '-r', '--ignore-unmatch', '--', TARGET_PATH], repository)
    destination = repository / TARGET_PATH
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(player, destination)
    git(['add', '--force', '--all', '--', TARGET_PATH], repository)
    changed = [name for name in git(['diff', '--cached', '--no-renames', '--name-only', '-z'], repository).split('\0') if name]
    require(changed and all(name.startswith(TARGET_PATH + '/') for name in changed), 'Staged paths escaped public/unity-gym')
    git(['config', 'user.name', 'github-actions[bot]'], repository)
    git(['config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com'], repository)
    git(['commit', '--quiet', '-m', 'test(unity-gym): publish verified kickboxing artifact ' + source[:12]], repository)
    commit = git(['rev-parse', 'HEAD'], repository)
    require(git(['rev-parse', 'HEAD^'], repository) == source, 'Publication commit must have the exact source as its parent')
    committed = [name for name in git(['diff-tree', '--no-commit-id', '--no-renames', '--name-only', '-r', source, commit], repository).splitlines() if name]
    require(set(committed) == set(changed), 'Committed paths differ from the verified target-only index')
    tree = git(['ls-tree', '-r', '-z', 'HEAD', '--', TARGET_PATH], repository)
    blobs = {}
    for record in filter(None, tree.split('\0')):
        identity, name = record.split('\t', 1)
        mode, kind, oid = identity.split()
        require(kind == 'blob' and mode == '100644', 'Unexpected mode or non-file in published player')
        blobs[name.removeprefix(TARGET_PATH + '/')] = oid
    require(set(blobs) == artifact_files(player), 'Committed player inventory differs from the downloaded artifact')
    for name, oid in blobs.items():
        require(git(['hash-object', '--no-filters', str(player / name)], repository) == oid,
                'Git would publish transformed bytes instead of the verified artifact: ' + name)
    receipt.update(status='pushing', publicationAttempted=True, proposedCommit=commit, changedPaths=changed,
                   sourceParentVerified=True, committedArtifactBytesVerified=True)
    write_json(receipt_path, receipt)
    # An empty expected old ref is an atomic create-only lease. Even a concurrent
    # branch creation is rejected; this can never replace an existing branch.
    git(['push', '--porcelain', '--force-with-lease=' + ref + ':', 'origin', 'HEAD:' + ref], repository, token)
    receipt['branchCreated'] = True
    write_json(receipt_path, receipt)
    observed = git(['ls-remote', '--heads', 'origin', ref], repository, token).split()
    require(len(observed) == 2 and observed[0] == commit and observed[1] == ref, 'Created branch could not be verified remotely')
    receipt.update(status='published-test-branch', publishedCommit=commit, remoteCommitVerified=True,
                   publishedUtc=utc_now(), branchUrl='https://github.com/' + REPOSITORY + '/tree/' + branch)
    write_json(receipt_path, receipt)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--request', type=Path, required=True)
    parser.add_argument('--proof-dir', type=Path, required=True)
    parser.add_argument('--publish', action='store_true', help='Create the new deployment branch; dedicated CI workflow only')
    arguments = parser.parse_args()
    proof = arguments.proof_dir.resolve()
    proof.mkdir(parents=True, exist_ok=True)
    receipt_path = proof / 'publication-receipt.json'
    receipt = {'status': 'validating', 'startedUtc': utc_now(), 'repository': REPOSITORY,
               'productionChanged': False, 'publicationAttempted': False, 'branchCreated': False,
               'remoteCommitVerified': False, 'liveVerified': False, 'previewUrl': None,
               'physicalIPhoneValidated': False, 'note': 'Only a separate source-specific test branch may be created. Hosting is not verified by this workflow.'}
    write_json(receipt_path, receipt)
    try:
        request = validate_request(read_json(arguments.request))
        receipt.update(sourceRun=request['run_id'], sourceCommit=request['source_sha'],
                       targetBranch=request['target_branch'], targetPath=TARGET_PATH,
                       publicationMode=request['publication_mode'])
        write_json(proof / 'publication-request.json', request)
        token = os.environ.get('GH_TOKEN', '')
        github = Github(token)
        run, artifacts = verified_run_and_artifacts(github, request)
        receipt['sourceRunUrl'] = 'https://github.com/' + REPOSITORY + '/actions/runs/' + str(request['run_id'])
        receipt['runConclusion'] = run['conclusion']
        with tempfile.TemporaryDirectory(prefix='kickboxing-test-publication-') as temporary:
            workspace = Path(temporary)
            receipt['artifacts'] = {}
            for kind, metadata in artifacts.items():
                archive = workspace / (kind + '.zip')
                receipt['artifacts'][kind] = github.download(metadata, archive)
                extract_zip(archive, workspace / kind)
            release, transport = verify_player(workspace / 'player', request['source_sha'])
            qa, qa_path, evidence = verify_qa(workspace / 'review', request['source_sha'])
            shutil.copyfile(workspace / 'player/release.json', proof / 'verified-release.json')
            shutil.copyfile(workspace / 'player/transport.json', proof / 'verified-transport.json')
            shutil.copyfile(qa_path, proof / 'original-qa-report.json')
            write_json(proof / 'runtime-evidence-inventory.json', evidence)
            receipt.update(status='validated', qaPassed=True, qaChecks=len(qa['checks']),
                           qaPlatform=qa.get('platform'), qaReportSha256=sha256(qa_path),
                           releaseSha256=sha256(workspace / 'player/release.json'),
                           verifiedFileCount=len(release['files']) + 1, transportVerification=transport,
                           billboardArtworkInstalled=False, visualAcceptance=qa.get('visualAcceptance'))
            write_json(receipt_path, receipt)
            if arguments.publish:
                publish_branch(workspace / 'player', workspace, request, token, receipt, receipt_path)
            print('KICKBOXING_TEST_PUBLICATION', receipt['status'], request['source_sha'], request['target_branch'])
            if os.environ.get('GITHUB_OUTPUT'):
                with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
                    output.write('source_sha=' + request['source_sha'] + '\ntarget_branch=' + request['target_branch'] + '\n')
                    output.write('published_commit=' + receipt.get('publishedCommit', '') + '\n')
            if os.environ.get('GITHUB_STEP_SUMMARY'):
                with open(os.environ['GITHUB_STEP_SUMMARY'], 'a', encoding='utf-8') as summary:
                    summary.write('## BRAXHORN isolated test publication\n\nStatus: `' + receipt['status'] + '`.\n\n'
                                  'Source: `' + request['source_sha'] + '`.\n\nTest branch: `' + request['target_branch'] + '`.\n\n'
                                  'Changed path: `public/unity-gym/` only. Existing production branch and host were not targeted.\n\n'
                                  'Original desktop QA report and byte-verification receipt are attached. Physical iPhone performance, visual acceptance, '
                                  'final billboard approval and the hosting preview URL remain separately unverified.\n')
        return 0
    except Exception as error:
        receipt['status'] = 'publication-unverified' if receipt['publicationAttempted'] else 'rejected'
        # Do not serialize arbitrary network/subprocess exception text or signed URLs.
        receipt['error'] = str(error) if isinstance(error, PublicationError) else 'Validation failed: ' + type(error).__name__
        receipt['finishedUtc'] = utc_now()
        write_json(receipt_path, receipt)
        print('KICKBOXING_TEST_PUBLICATION_REJECTED', receipt['error'], file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
