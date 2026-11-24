#!/usr/bin/env python3
"""Sequential, bounded offline probe runner. Only the reviewed fixed argv are executable."""
import argparse
import collections
import ctypes
import datetime
import hashlib
import json
import math
import os
from pathlib import Path
import platform
import re
import selectors
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

MANIFEST = [{"argv":["bash","plan/tests/api02/run.sh"],"id":"batch:api02","kind":"batch","logical_id":"batch:api02","required_paths":["plan/tests/api02/run.sh","plan/tests/api02/API02.csproj"]},{"argv":["bash","plan/tests/auth1/run.sh"],"id":"batch:auth1","kind":"batch","logical_id":"batch:auth1","required_paths":["plan/tests/auth1/run.sh","plan/tests/auth1/AUTH1.csproj"]},{"argv":["bash","plan/tests/auth2/run.sh"],"id":"batch:auth2","kind":"batch","logical_id":"batch:auth2","required_paths":["plan/tests/auth2/run.sh","plan/tests/auth2/AUTH2.csproj"]},{"argv":["bash","plan/tests/auth3/run.sh"],"id":"batch:auth3","kind":"batch","logical_id":"batch:auth3","required_paths":["plan/tests/auth3/run.sh","plan/tests/auth3/AUTH3.csproj"]},{"argv":["bash","plan/tests/auth4/run.sh"],"id":"batch:auth4","kind":"batch","logical_id":"batch:auth4","required_paths":["plan/tests/auth4/run.sh","plan/tests/auth4/AUTH4.csproj"]},{"argv":["bash","plan/tests/c1/run.sh"],"id":"batch:c1","kind":"batch","logical_id":"batch:c1","required_paths":["plan/tests/c1/run.sh","plan/tests/c1/C1Probe.csproj"]},{"argv":["bash","plan/tests/c2/run.sh"],"id":"batch:c2","kind":"batch","logical_id":"batch:c2","required_paths":["plan/tests/c2/run.sh","plan/tests/c2/C2Probe.csproj"]},{"argv":["bash","plan/tests/c3/run.sh"],"id":"batch:c3","kind":"batch","logical_id":"batch:c3","required_paths":["plan/tests/c3/run.sh","plan/tests/c3/C3Probe.csproj"]},{"argv":["bash","plan/tests/cap/run.sh"],"id":"batch:cap","kind":"batch","logical_id":"batch:cap","required_paths":["plan/tests/cap/run.sh","plan/tests/cap/CapProbe.csproj"]},{"argv":["bash","plan/tests/cardlayout/run.sh"],"id":"batch:cardlayout","kind":"batch","logical_id":"batch:cardlayout","required_paths":["plan/tests/cardlayout/run.sh","plan/tests/cardlayout/CardLayoutProbe.csproj"]},{"argv":["bash","plan/tests/cm1/run.sh"],"id":"batch:cm1","kind":"batch","logical_id":"batch:cm1","required_paths":["plan/tests/cm1/run.sh","plan/tests/cm1/Cm1Probe.csproj"]},{"argv":["bash","plan/tests/comments/run.sh"],"id":"batch:comments","kind":"batch","logical_id":"batch:comments","required_paths":["plan/tests/comments/run.sh","plan/tests/comments/CommentsProbe.csproj"]},{"argv":["bash","plan/tests/d1/run.sh"],"id":"batch:d1","kind":"batch","logical_id":"batch:d1","required_paths":["plan/tests/d1/run.sh","plan/tests/d1/D1Probe.csproj"]},{"argv":["bash","plan/tests/d2/run.sh"],"id":"batch:d2","kind":"batch","logical_id":"batch:d2","required_paths":["plan/tests/d2/run.sh","plan/tests/d2/D2Probe.csproj"]},{"argv":["bash","plan/tests/dirpress/run.sh"],"id":"batch:dirpress","kind":"batch","logical_id":"batch:dirpress","required_paths":["plan/tests/dirpress/run.sh","plan/tests/dirpress/DirPressProbe.csproj"]},{"argv":["bash","plan/tests/firstrun/run.sh"],"id":"batch:firstrun","kind":"batch","logical_id":"batch:firstrun","required_paths":["plan/tests/firstrun/run.sh","plan/tests/firstrun/FirstRunProbe.csproj"]},{"argv":["bash","plan/tests/focus/run.sh"],"id":"batch:focus","kind":"batch","logical_id":"batch:focus","required_paths":["plan/tests/focus/run.sh","plan/tests/focus/FocusProbe.csproj"]},{"argv":["bash","plan/tests/run-g1.sh"],"id":"batch:g1","kind":"batch","logical_id":"batch:g1","required_paths":["plan/tests/run-g1.sh","plan/tests/g1/G1Probe.csproj"]},{"argv":["bash","plan/tests/g2/run.sh"],"id":"batch:g2","kind":"batch","logical_id":"batch:g2","required_paths":["plan/tests/g2/run.sh","plan/tests/g2/G2Probe.csproj"]},{"argv":["bash","plan/tests/g3/run.sh"],"id":"batch:g3","kind":"batch","logical_id":"batch:g3","required_paths":["plan/tests/g3/run.sh","plan/tests/g3/G3Probe.csproj"]},{"argv":["bash","plan/tests/g4/run.sh"],"id":"batch:g4","kind":"batch","logical_id":"batch:g4","required_paths":["plan/tests/g4/run.sh","plan/tests/g4/G4Probe.csproj"]},{"argv":["bash","plan/tests/g5/run.sh"],"id":"batch:g5","kind":"batch","logical_id":"batch:g5","required_paths":["plan/tests/g5/run.sh","plan/tests/g5/G5Probe.csproj"]},{"argv":["bash","plan/tests/localdir/run.sh"],"id":"batch:localdir","kind":"batch","logical_id":"batch:localdir","required_paths":["plan/tests/localdir/run.sh","plan/tests/localdir/LocalDirProbe.csproj"]},{"argv":["bash","plan/tests/ov/run.sh"],"id":"batch:ov","kind":"batch","logical_id":"batch:ov","required_paths":["plan/tests/ov/run.sh","plan/tests/ov/OVProbe.csproj"]},{"argv":["bash","plan/tests/playersess/run.sh"],"id":"batch:playersess","kind":"batch","logical_id":"batch:playersess","required_paths":["plan/tests/playersess/run.sh","plan/tests/playersess/PlayersessProbe.csproj"]},{"argv":["bash","plan/tests/playertoolbar/run.sh"],"id":"batch:playertoolbar","kind":"batch","logical_id":"batch:playertoolbar","required_paths":["plan/tests/playertoolbar/run.sh","plan/tests/playertoolbar/PlayerToolbarProbe.csproj"]},{"argv":["bash","plan/tests/season1/run.sh"],"id":"batch:season1","kind":"batch","logical_id":"batch:season1","required_paths":["plan/tests/season1/run.sh","plan/tests/season1/Season1Probe.csproj"]},{"argv":["bash","plan/tests/settings1/run.sh"],"id":"batch:settings1","kind":"batch","logical_id":"batch:settings1","required_paths":["plan/tests/settings1/run.sh","plan/tests/settings1/SETTINGS1.csproj"]},{"argv":["bash","plan/tests/smart1/run.sh"],"id":"batch:smart1","kind":"batch","logical_id":"batch:smart1","required_paths":["plan/tests/smart1/run.sh","plan/tests/smart1/Smart1Probe.csproj"]},{"argv":["bash","plan/tests/stab/run.sh"],"id":"batch:stab","kind":"batch","logical_id":"batch:stab","required_paths":["plan/tests/stab/run.sh"]},{"argv":["bash","plan/tests/toastalign/run.sh"],"id":"batch:toastalign","kind":"batch","logical_id":"batch:toastalign","required_paths":["plan/tests/toastalign/run.sh","plan/tests/toastalign/ToastAlignProbe.csproj"]},{"argv":["bash","plan/tests/ux0/run.sh"],"id":"batch:ux0","kind":"batch","logical_id":"batch:ux0","required_paths":["plan/tests/ux0/run.sh","plan/tests/ux0/Ux0Probe.csproj"]},{"argv":["bash","plan/tests/ux1a/run.sh"],"id":"batch:ux1a","kind":"batch","logical_id":"batch:ux1a","required_paths":["plan/tests/ux1a/run.sh","plan/tests/ux1a/Ux1aProbe.csproj"]},{"argv":["bash","plan/tests/ux1b/run.sh"],"id":"batch:ux1b","kind":"batch","logical_id":"batch:ux1b","required_paths":["plan/tests/ux1b/run.sh","plan/tests/ux1b/Ux1bProbe.csproj"]},{"argv":["bash","plan/tests/ux1c/run.sh"],"id":"batch:ux1c","kind":"batch","logical_id":"batch:ux1c","required_paths":["plan/tests/ux1c/run.sh","plan/tests/ux1c/Ux1cProbe.csproj"]},{"argv":["bash","plan/tests/ux1d/run.sh"],"id":"batch:ux1d","kind":"batch","logical_id":"batch:ux1d","required_paths":["plan/tests/ux1d/run.sh","plan/tests/ux1d/Ux1dProbe.csproj"]},{"argv":["bash","plan/tests/ux2a/run.sh"],"id":"batch:ux2a","kind":"batch","logical_id":"batch:ux2a","required_paths":["plan/tests/ux2a/run.sh","plan/tests/ux2a/Ux2aProbe.csproj"]},{"argv":["bash","plan/tests/ux2b/run.sh"],"id":"batch:ux2b","kind":"batch","logical_id":"batch:ux2b","required_paths":["plan/tests/ux2b/run.sh","plan/tests/ux2b/Ux2bProbe.csproj"]},{"argv":["bash","plan/tests/ux3/run.sh"],"id":"batch:ux3","kind":"batch","logical_id":"batch:ux3","required_paths":["plan/tests/ux3/run.sh","plan/tests/ux3/Ux3Probe.csproj"]},{"argv":["bash","plan/tests/ux4/run.sh"],"id":"batch:ux4","kind":"batch","logical_id":"batch:ux4","required_paths":["plan/tests/ux4/run.sh","plan/tests/ux4/Ux4Probe.csproj"]},{"argv":["bash","plan/tests/ux5/run.sh"],"id":"batch:ux5","kind":"batch","logical_id":"batch:ux5","required_paths":["plan/tests/ux5/run.sh","plan/tests/ux5/Ux5Probe.csproj"]},{"argv":["bash","plan/tests/uxfix/run.sh"],"id":"batch:uxfix","kind":"batch","logical_id":"batch:uxfix","required_paths":["plan/tests/uxfix/run.sh","plan/tests/uxfix/UxfixProbe.csproj"]},{"argv":["bash","plan/tests/webrefresh/run.sh"],"id":"batch:webrefresh","kind":"batch","logical_id":"batch:webrefresh","required_paths":["plan/tests/webrefresh/run.sh","plan/tests/webrefresh/WEBREFRESH.csproj"]},{"argv":["bash","plan/tests/run.sh","--stage","LM01"],"id":"LM01","kind":"single","logical_id":"LM01","required_paths":["plan/tests/run.sh","plan/tests/LM01.cs"]},{"argv":["bash","plan/tests/run.sh","--stage","LM02"],"id":"LM02","kind":"single","logical_id":"LM02","required_paths":["plan/tests/run.sh","plan/tests/LM02.cs"]},{"argv":["bash","plan/tests/run.sh","--stage","LM03"],"id":"LM03","kind":"single","logical_id":"LM03","required_paths":["plan/tests/run.sh","plan/tests/LM03.cs"]},{"argv":["bash","plan/tests/run.sh","--stage","LM04"],"id":"LM04","kind":"single","logical_id":"LM04","required_paths":["plan/tests/run.sh","plan/tests/LM04.cs"]},{"argv":["bash","plan/tests/run.sh","--stage","LM05"],"id":"LM05","kind":"single","logical_id":"LM05","required_paths":["plan/tests/run.sh","plan/tests/LM05.cs"]},{"argv":["bash","plan/tests/run-api01.sh"],"id":"API01","kind":"single","logical_id":"API01","required_paths":["plan/tests/run-api01.sh","plan/tests/API01.cs"]},{"argv":["bash","plan/tests/run-auth01b.sh","--stage","account"],"id":"AUTH01B.account","kind":"single","logical_id":"AUTH01B","required_paths":["plan/tests/run-auth01b.sh","plan/tests/AUTH01B.cs"]},{"argv":["bash","plan/tests/run-auth01b.sh","--stage","loginvm"],"id":"AUTH01B.loginvm","kind":"single","logical_id":"AUTH01B","required_paths":["plan/tests/run-auth01b.sh","plan/tests/AUTH01BLoginVM.cs"]}]

LOG_LIMIT = 1024 * 1024
CANCEL = None


def utc():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def positive(value):
    if not re.fullmatch(r'[0-9]+', value) or int(value) <= 0:
        raise argparse.ArgumentTypeError('must be a positive finite integer')
    try:
        if not math.isfinite(float(value)):
            raise ValueError
    except (OverflowError, ValueError):
        raise argparse.ArgumentTypeError('must be a positive finite integer')
    return int(value)


def capture(argv, cwd, env=None, timeout=5):
    """Bound even metadata commands; never inherit a source snapshot's parent git repo."""
    child = subprocess.Popen(argv, cwd=cwd, env=env, stdout=subprocess.PIPE,
                             stderr=subprocess.STDOUT, start_new_session=True)
    try:
        data, _ = child.communicate(timeout=max(.01, timeout))
        return child.returncode, data[:65536].decode('utf-8', 'replace').strip()
    except subprocess.TimeoutExpired:
        os.killpg(child.pid, signal.SIGKILL)
        child.communicate()
        return -9, 'metadata/preflight command timeout'


class BoundedLog:
    def __init__(self):
        self.head = bytearray()
        self.tail = bytearray()
        self.seen = 0
        self.scan = b''
        self.phase = None
        self.markers = set()
        self.hints = []

    def feed(self, data):
        self.seen += len(data)
        needed = LOG_LIMIT // 2 - len(self.head)
        self.head.extend(data[:max(0, needed)])
        self.tail.extend(data[max(0, needed):])
        self.tail = self.tail[-LOG_LIMIT // 2:]
        lines = (self.scan + data).split(b'\n')
        self.scan = lines.pop()[-4096:]
        for line in lines:
            text = line.decode('utf-8', 'replace')
            match = re.fullmatch(r'PROBE_PHASE (\S+) (compile|run|complete)', text.strip())
            if match:
                self.phase = match.group(2)
                self.markers.add((match.group(1), match.group(2)))
            if len(self.hints) < 8 and re.search(r'error (CS|MSB|NETSDK)|not found|No such file', text):
                self.hints.append(text[:500])

    def save(self, path):
        # A truncation marker is report metadata, so retained raw bytes stay <=1 MiB.
        path.write_bytes(bytes(self.head + self.tail))


def group_alive(pid):
    try:
        os.killpg(pid, 0)
        return True
    except ProcessLookupError:
        return False


def drain_cleanup(child, reader, log):
    """Own/reap the entire group, including orphaned grandchildren on Linux."""
    try:
        os.killpg(child.pid, signal.SIGTERM)
    except ProcessLookupError:
        pass
    until = time.monotonic() + .5
    while time.monotonic() < until and group_alive(child.pid):
        drain(reader, log, .02)
    try:
        os.killpg(child.pid, signal.SIGKILL)
    except ProcessLookupError:
        pass
    child.wait(timeout=2)
    until = time.monotonic() + 1
    while time.monotonic() < until:
        drain(reader, log, .01)
        reap_group(child.pid)
        if not group_alive(child.pid):
            break
    drain(reader, log, 0)
    reader.close()
    child.stdout.close()


def reap_group(pid):
    # Called after Popen.wait() has reaped the direct child. Negative waitpid
    # targets only this owned process group; no /proc filesystem is required.
    while True:
        try:
            reaped, _ = os.waitpid(-pid, os.WNOHANG)
            if reaped == 0:
                return
        except ChildProcessError:
            return


def drain(reader, log, timeout):
    for key, _ in reader.select(timeout):
        data = os.read(key.fd, 65536)
        if data:
            log.feed(data)
        else:
            reader.unregister(key.fd)


def execute(row, root, env, stage_timeout, suite_end, output):
    start = time.monotonic()
    deadline = min(start + stage_timeout, suite_end)
    budget = 'suite_budget' if suite_end <= start + stage_timeout else 'stage_budget'
    log = BoundedLog()
    path = output / row['log_path']
    child = None
    try:
        child = subprocess.Popen(row['argv'], cwd=root, env=env, stdout=subprocess.PIPE,
                                 stderr=subprocess.STDOUT, start_new_session=True)
        row['pid'] = child.pid
        reader = selectors.DefaultSelector()
        os.set_blocking(child.stdout.fileno(), False)
        reader.register(child.stdout, selectors.EVENT_READ)
        while True:
            drain(reader, log, .03)
            if CANCEL:
                row.update(status='CANCELLED', reason='signal:' + signal.Signals(CANCEL).name,
                           failure_kind='cancelled')
                break
            if time.monotonic() >= deadline:
                row.update(status='TIMEOUT', reason=budget, failure_kind='timeout')
                break
            if child.poll() is not None and not reader.get_map():
                row.update(status='PASS' if child.returncode == 0 else 'FAIL',
                           reason=None if child.returncode == 0 else 'command_exit',
                           failure_kind=None if child.returncode == 0 else 'command')
                break
        drain_cleanup(child, reader, log)
        row['exit_code'] = child.returncode
        marker_id = row['id'] if row['id'].startswith(('LM', 'AUTH01B.')) else None
        if row['status'] == 'PASS' and marker_id and (marker_id, 'complete') not in log.markers:
            row.update(status='ERROR', reason='missing_completion_marker', failure_kind='protocol')
        elif row['status'] == 'FAIL' and marker_id and log.phase in ('compile', 'run'):
            row['failure_kind'] = 'compile' if log.phase == 'compile' else 'executable'
    except (OSError, subprocess.SubprocessError) as exc:
        if child and child.poll() is None:
            os.killpg(child.pid, signal.SIGKILL)
            child.wait()
        row.update(status='ERROR', reason=str(exc)[:1000], failure_kind='launch')
    log.save(path)
    row.update(duration_seconds=round(time.monotonic() - start, 3), phase=log.phase,
               log_bytes_seen=log.seen, log_truncated=log.seen > LOG_LIMIT,
               diagnostic_hints=log.hints)


def main():
    if sys.version_info < (3, 9):
        print('run-all requires Python >=3.9', file=sys.stderr)
        return 2
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--list', action='store_true')
    parser.add_argument('--only', action='append', default=[])
    parser.add_argument('--tools-root', default=os.environ.get('UPSTREAM_UNIT_TOOLS', '/workspace/work/upstream-unit-tools'))
    parser.add_argument('--output')
    parser.add_argument('--stage-timeout', type=positive, default=300)
    parser.add_argument('--suite-timeout', type=positive, default=1800)
    parser.add_argument('--repo-root', default=str(Path(__file__).resolve().parents[2]))
    args = parser.parse_args()
    if args.list:
        if len(sys.argv) != 2:
            parser.error('--list is mutually exclusive with execution/selection flags')
        print('Historical baseline: 50 logical / 51 executable; current: 50 logical / 51 executable')
        for stage in MANIFEST:
            print('%s [%s] %s' % (stage['id'], stage['logical_id'], json.dumps(stage['argv'])))
        return 0
    known = {s['id'] for s in MANIFEST} | {'AUTH01B'}
    if any(item not in known for item in args.only):
        parser.error('unknown/empty stage ID: ' + repr([i for i in args.only if i not in known]))
    root, tools = Path(args.repo_root).resolve(), Path(args.tools_root).resolve()
    if not (root / 'src').is_dir() or not (root / 'plan/tests').is_dir():
        parser.error('--repo-root must contain src/ and plan/tests/')
    output = Path(args.output).resolve() if args.output else Path(tempfile.mkdtemp(prefix='bili-offline-' + datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ-') + str(os.getpid()) + '-'))
    try:
        if output.exists() and (not output.is_dir() or any(output.iterdir())):
            parser.error('--output must be a missing or empty directory')
        if root == output or output in root.parents or tools == output or output in tools.parents:
            parser.error('source/tools root must not be inside report output')
        output.mkdir(parents=True, exist_ok=True)
        (output / 'logs').mkdir()
    except OSError as exc:
        print('report output unavailable: ' + str(exc), file=sys.stderr)
        return 2
    print('Reports: ' + str(output), flush=True)
    suite_start = time.monotonic()
    suite_end = suite_start + args.suite_timeout
    def on_signal(signum, _frame):
        global CANCEL
        CANCEL = CANCEL or signum
    signal.signal(signal.SIGINT, on_signal)
    signal.signal(signal.SIGTERM, on_signal)
    if sys.platform == 'linux':
        # PR_SET_CHILD_SUBREAPER: orphaned descendants can be killed and reaped here.
        if ctypes.CDLL(None, use_errno=True).prctl(36, 1, 0, 0, 0) != 0:
            print('cannot establish process-tree reaper', file=sys.stderr)
            return 2
    selected = {s['id'] for s in MANIFEST if not args.only or s['id'] in args.only or (s['logical_id'] == 'AUTH01B' and 'AUTH01B' in args.only)}
    rows = []
    for stage in MANIFEST:
        row = dict(stage, selected=stage['id'] in selected, cwd=str(root), status='PENDING' if stage['id'] in selected else 'NOT_SELECTED',
                   exit_code=None, reason=None, phase=None, duration_seconds=0,
                   log_path='logs/' + stage['id'].replace(':', '_').replace('.', '_') + '.log',
                   log_truncated=False, log_bytes_seen=0)
        row['missing_paths'] = [p for p in stage['required_paths'] if not (root / p).is_file()]
        # Explicit linked, non-generated sources must not vanish from partial snapshots.
        for project in [p for p in stage['required_paths'] if p.endswith('.csproj') and (root / p).is_file()]:
            try:
                for element in ET.parse(root / project).iter():
                    if element.tag.rsplit('}', 1)[-1] == 'Compile' and element.get('Include'):
                        include = element.get('Include').replace('\\', '/')
                        if not any(x in include for x in ('obj/', 'bin/', '$', '*')):
                            path = ((root / project).parent / include).resolve()
                            if not path.is_file():
                                row['missing_paths'].append(os.path.relpath(path, root))
            except ET.ParseError:
                pass  # Preserve the real compiler's invalid-project diagnosis.
        row['available'] = not row['missing_paths']
        rows.append(row)
    expected = {s['id'].split(':')[1] for s in MANIFEST if s['kind'] == 'batch'}
    observed = {p.name for p in (root / 'plan/tests').iterdir() if p.is_dir() and ((p / 'run.sh').is_file() or any(p.glob('*.csproj')))}
    drift = sorted(observed - expected)
    env = os.environ.copy()
    dotnet = tools / 'dotnet'
    env.update(UPSTREAM_UNIT_TOOLS=str(tools), DOTNET_ROOT=str(dotnet), PATH=str(dotnet) + os.pathsep + env.get('PATH', ''),
               DOTNET_CLI_HOME=str(tools / 'cache/dotnet-cli'), NUGET_PACKAGES=str(tools / 'cache/nuget'),
               XDG_CACHE_HOME=str(tools / 'cache/xdg'), XDG_DATA_HOME=str(tools / 'cache/xdg-data'),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1',
               DOTNET_MULTILEVEL_LOOKUP='0')
    report = dict(schema_version=1, started_utc=utc(), source_root=str(root), tools_root=str(tools),
                  manifest_sha256=hashlib.sha256(json.dumps(MANIFEST, sort_keys=True, separators=(',', ':')).encode()).hexdigest(),
                  historical_baseline=dict(logical=50, executable=51), current_manifest=dict(logical=50, executable=51),
                  platform=platform.platform(), python=sys.version.split()[0],
                  resolved_inputs={'dotnet': str(dotnet / 'dotnet'), 'sdk': str(dotnet / 'sdk/8.0.427'),
                                   'references': str(dotnet / 'packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0')},
                  source_provenance={'head': None, 'dirty': None, 'verified': False},
                  inventory_errors=['unexpected runnable directory: ' + item for item in drift], stages=rows,
                  extra_gates={'package_layout': {'status': 'NOT_CONFIGURED', 'reason': 'separately-owned, excluded'}},
                  stage_timeout=args.stage_timeout, suite_timeout=args.suite_timeout)
    if (root / '.git').exists():
        code, head = capture(['git', 'rev-parse', 'HEAD'], root)
        dirty_code, dirty = capture(['git', 'status', '--porcelain', '--untracked-files=normal'], root)
        report['source_provenance'] = {'head': head if code == 0 else None, 'dirty': bool(dirty) if dirty_code == 0 else None, 'verified': code == dirty_code == 0}
    _, report['bash'] = capture(['bash', '--version'], root)

    def checkpoint(final=False):
        counts = collections.Counter({status: 0 for status in ('PASS', 'FAIL', 'MISSING', 'BLOCKED', 'TIMEOUT', 'ERROR', 'CANCELLED', 'NOT_SELECTED', 'PENDING')})
        counts.update(row['status'] for row in rows)
        logical = {}
        for lid in dict.fromkeys(row['logical_id'] for row in rows):
            members = [r for r in rows if r['logical_id'] == lid]
            chosen = [r for r in members if r['selected']]
            logical[lid] = ('NOT_SELECTED' if not chosen else 'FAIL' if any(r['status'] not in ('PASS', 'PENDING') for r in chosen)
                            else 'PASS' if len(chosen) == len(members) and all(r['status'] == 'PASS' for r in chosen) else 'PARTIAL')
        passed = all(r['status'] == 'PASS' for r in rows if r['selected']) and not drift and not CANCEL
        complete = len(selected) == 51 and passed
        report.update(finished=final, complete_required=complete if final else False,
                      suite_status=('PASS' if complete else 'PARTIAL_PASS') if final and passed else 'FAIL' if final else 'RUNNING',
                      selected_totals={'logical': len({r['logical_id'] for r in rows if r['selected']}), 'executable': len(selected)},
                      status_counts=dict(counts), logical_status=logical, logical_status_counts=dict(collections.Counter(logical.values())),
                      logical_pass=sum(s == 'PASS' for s in logical.values()), executable_pass=counts['PASS'],
                      duration_seconds=round(time.monotonic() - suite_start, 3), updated_utc=utc())
        body = '# Offline probes: ' + report['suite_status'] + '\n\nHistorical baseline: 50 logical / 51 executable. Current: 50 logical / 51 executable.\n\n'
        body += 'Selected: %s logical / %s executable; PASS: %s logical / %s executable; complete_required: %s.\n\n' % (report['selected_totals']['logical'], len(selected), report['logical_pass'], counts['PASS'], report['complete_required'])
        body += 'Stage counts: ' + ', '.join('%s=%s' % (key, value) for key, value in counts.items()) + '.\n\n'
        body += '| Stage | Status | Exit | Reason | Log |\n| --- | --- | --- | --- | --- |\n'
        for r in rows:
            body += '| %s | %s | %s | %s | [%s](%s) |\n' % (r['id'], r['status'], r['exit_code'], str(r['reason']).replace('|', '/').replace('\n', ' ')[:250], r['log_path'], r['log_path'])
        body += '\nPackage layout: NOT_CONFIGURED (separately-owned, excluded). Windows/UWP/MSIX/device/session gates: BLOCKED; excluded from this suite.\n'
        for name, content in [('results.json', json.dumps(report, indent=2) + '\n'), ('summary.md', body)]:
            temporary = output / (name + '.tmp')
            temporary.write_text(content)
            temporary.replace(output / name)

    checkpoint()
    prerequisites = []
    required = ['dotnet', 'sdk/8.0.427/Roslyn/bincore/csc.dll', 'sdk/8.0.427/Containers/tasks/net8.0/Newtonsoft.Json.dll',
                'packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0/System.Runtime.dll', 'shared/Microsoft.NETCore.App/8.0.33/libcoreclr.so']
    for item in required:
        if not (dotnet / item).is_file():
            prerequisites.append('missing exact toolchain input: ' + str(dotnet / item))
    if (dotnet / 'dotnet').is_file() and not os.access(dotnet / 'dotnet', os.X_OK):
        prerequisites.append('dotnet executable is not executable: ' + str(dotnet / 'dotnet'))
    if not prerequisites and any(r['selected'] and r['id'] != 'batch:stab' for r in rows):
        for flag, key in [('--version', 'dotnet_version'), ('--list-runtimes', 'dotnet_runtimes')]:
            if CANCEL or time.monotonic() >= suite_end:
                break
            code, value = capture([str(dotnet / 'dotnet'), flag], root, env, min(5, suite_end - time.monotonic()))
            report[key] = value
            if code or (flag == '--version' and value != '8.0.427') or (flag == '--list-runtimes' and not re.search(r'^Microsoft.NETCore.App 8\.0\.33 \[', value, re.M)):
                prerequisites.append('exact toolchain mismatch %s: exit=%s %s' % (flag, code, value))
    report['prerequisite_errors'] = prerequisites
    checkpoint()
    for row in rows:
        if not row['selected']:
            continue
        if CANCEL:
            row.update(status='BLOCKED', reason='cancelled', failure_kind='cancelled')
        elif time.monotonic() >= suite_end:
            row.update(status='BLOCKED', reason='suite_budget', failure_kind='timeout')
        elif row['missing_paths']:
            row.update(status='MISSING', reason='missing required paths: ' + ', '.join(row['missing_paths']), failure_kind='missing_source')
        elif row['id'] != 'batch:stab' and prerequisites:
            row.update(status='BLOCKED', reason='; '.join(prerequisites), failure_kind='prerequisite')
        elif any(not shutil.which(command, path=env['PATH']) for command in (['bash', 'awk', 'sed', 'grep', 'cut'] if row['id'] == 'batch:stab' else ['bash', 'awk', 'sed', 'cp', 'mktemp'])):
            row.update(status='BLOCKED', reason='required shell utility unavailable', failure_kind='prerequisite')
        else:
            execute(row, root, env, args.stage_timeout, suite_end, output)
        if not (output / row['log_path']).exists():
            (output / row['log_path']).write_text((row['reason'] or '')[:4096] + '\n')
        print('%-22s %-12s %s' % (row['id'], row['status'], row['exit_code']), flush=True)
        checkpoint()
    checkpoint(final=True)
    if CANCEL:
        return 128 + CANCEL
    return 0 if report['suite_status'] in ('PASS', 'PARTIAL_PASS') else 1


if __name__ == '__main__':
    try:
        sys.exit(main())
    except OSError as exc:
        print('runner/report error: ' + str(exc), file=sys.stderr)
        sys.exit(2)
