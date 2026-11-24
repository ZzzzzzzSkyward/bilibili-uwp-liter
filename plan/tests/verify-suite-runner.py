#!/usr/bin/env python3
"""External orchestration controls; these never count toward the legacy 50/51 suite."""
import argparse
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import tempfile
import time

REPO = Path(__file__).resolve().parents[2]
RUNNER = REPO / 'plan/tests/run-all.sh'
LIMIT = 1024 * 1024


def new_output(path):
    path = path.resolve()
    if path.exists() and (not path.is_dir() or any(path.iterdir())):
        raise ValueError('--output must be missing or empty')
    path.mkdir(parents=True, exist_ok=True)
    return path


def run_capture(argv, cwd, env=None, timeout=120):
    child = subprocess.Popen(argv, cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                             start_new_session=True)
    try:
        data, _ = child.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        os.killpg(child.pid, signal.SIGKILL)
        child.communicate()
        raise AssertionError('control command exceeded external deadline')
    return child.returncode, data[-LIMIT:].decode('utf-8', 'replace')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tools-root', default=os.environ.get('UPSTREAM_UNIT_TOOLS', '/workspace/work/upstream-unit-tools'))
    parser.add_argument('--output')
    args = parser.parse_args()
    try:
        output = new_output(Path(args.output)) if args.output else Path(tempfile.mkdtemp(prefix='bili-runner-controls-'))
    except (ValueError, OSError) as exc:
        parser.error(str(exc))
    tools = Path(args.tools_root).resolve()
    # Temporary source and cache trees never enter report/upload trees, even on a failed control.
    scratch_parent = Path(tempfile.gettempdir()).resolve()
    if scratch_parent == output or output in scratch_parent.parents or tools == output or output in tools.parents or REPO == output or output in REPO.parents:
        parser.error('fixture/source/cache location must be outside output tree')
    results = []
    env = os.environ.copy()
    env.update(UPSTREAM_UNIT_TOOLS=str(tools), DOTNET_ROOT=str(tools / 'dotnet'),
               DOTNET_CLI_HOME=str(tools / 'cache/dotnet-cli'), NUGET_PACKAGES=str(tools / 'cache/nuget'),
               XDG_CACHE_HOME=str(tools / 'cache/xdg'), XDG_DATA_HOME=str(tools / 'cache/xdg-data'))
    with tempfile.TemporaryDirectory(prefix='bili-offline-fixtures-', dir=scratch_parent) as temporary:
        scratch = Path(temporary)

        def control(name, function):
            started = time.monotonic()
            try:
                detail = function()
                result = dict(name=name, status='PASS', detail=detail)
            except Exception as exc:
                result = dict(name=name, status='FAIL', detail=str(exc)[:4000])
            result['duration_seconds'] = round(time.monotonic() - started, 3)
            results.append(result)
            (output / 'controls.json').write_text(json.dumps({'schema_version': 1, 'separate_from_legacy_totals': True, 'controls': results}, indent=2) + '\n')
            (output / 'summary.md').write_text('# Runner contract controls\n\nSeparate from legacy 50 logical / 51 executable totals.\n\n' + '\n'.join('- %s: %s — %s' % (r['name'], r['status'], r['detail']) for r in results) + '\n')
            print(name + ': ' + result['status'], flush=True)

        def fixture(name, first='exit 7\n'):
            root = scratch / name
            (root / 'src').mkdir(parents=True)
            for batch, project, body in [('firstrun', 'FirstRunProbe', first), ('focus', 'FocusProbe', 'echo CONTROL_LATER\ntouch later-started\n')]:
                folder = root / 'plan/tests' / batch
                folder.mkdir(parents=True)
                (folder / 'run.sh').write_text('#!/usr/bin/env bash\n' + body)
                (folder / (project + '.csproj')).write_text('<Project />\n')
            return root

        def command(root, name, extra=()):
            return ['bash', str(RUNNER), '--repo-root', str(root), '--tools-root', str(tools),
                    '--only', 'batch:firstrun', '--only', 'batch:focus', '--output', str(output / name)] + list(extra)

        def check_report(name, code, expected, reasons=None, expected_exit=1):
            assert code == expected_exit, (code, expected_exit)
            report_dir = output / name
            report = json.loads((report_dir / 'results.json').read_text())
            assert report['historical_baseline'] == {'logical': 50, 'executable': 51}
            assert report['current_manifest'] == {'logical': 50, 'executable': 51}
            assert report['selected_totals'] == {'logical': 2, 'executable': 2}
            assert report['finished'] and not report['complete_required']
            assert len(report['stages']) == 51
            rows = [r for r in report['stages'] if r['selected']]
            assert [r['status'] for r in rows] == expected, [(r['id'], r['status'], r['reason']) for r in rows]
            if reasons:
                assert [r['reason'] for r in rows] == reasons
            assert (report_dir / 'summary.md').is_file()
            assert all(p.stat().st_size <= LIMIT for p in (report_dir / 'logs').glob('*.log'))
            assert sum(p.stat().st_size for p in report_dir.rglob('*') if p.is_file()) < 60 * LIMIT
            return rows

        def slow_metadata(name, signum=None):
            root = fixture(name, 'touch should-not-run\n')
            (root / '.git').mkdir()
            wrappers = root / 'metadata-tools'; wrappers.mkdir()
            real_git, real_bash = shutil.which('git'), shutil.which('bash')
            assert real_git and real_bash
            # Make failed controls reap adopted descendants too; no test process is leaked.
            if sys.platform == 'linux':
                assert ctypes.CDLL(None, use_errno=True).prctl(36, 1, 0, 0, 0) == 0
            wrapper = r"""#!PYTHON
import json
import os
from pathlib import Path
import subprocess
import sys
import time
name = Path(sys.argv[0]).name
metadata = name == 'git' or sys.argv[1:] == ['--version']
if metadata:
    calls = Path('metadata-calls')
    first = not calls.exists()
    with calls.open('a') as stream:
        stream.write(name + ' ' + ' '.join(sys.argv[1:]) + '\n')
    if first:
        child = subprocess.Popen([sys.executable, '-c', "import time; from pathlib import Path; time.sleep(5); Path('metadata-late').write_text('survived')"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        Path('metadata-child').write_text(str(child.pid))
        Path('metadata-ready').write_text('ready')
    time.sleep(3)
os.execv(REAL_COMMAND, [REAL_COMMAND] + sys.argv[1:])
"""
            for executable, real in [('git', real_git), ('bash', real_bash)]:
                path = wrappers / executable
                path.write_text(wrapper.replace('#!PYTHON', '#!' + sys.executable).replace('REAL_COMMAND', repr(real)))
                path.chmod(0o755)
            wrapped_env = dict(env, PATH=str(wrappers) + os.pathsep + env.get('PATH', ''))
            argv = command(root, name, ['--suite-timeout', '30' if signum else '1'])
            argv[0] = real_bash
            child = subprocess.Popen(argv, cwd=REPO, env=wrapped_env, stdout=subprocess.PIPE,
                                     stderr=subprocess.STDOUT, start_new_session=True)
            start = time.monotonic()
            observed_pid = None
            try:
                until = start + 15
                while not (root / 'metadata-ready').exists():
                    assert child.poll() is None and time.monotonic() < until, 'metadata wrapper never became ready'
                    time.sleep(.02)
                observed_pid = int((root / 'metadata-child').read_text())
                initialized = (output / name / 'results.json').is_file()
                if initialized:
                    initial = json.loads((output / name / 'results.json').read_text())
                    assert len(initial['stages']) == 51 and not initial['finished']
                if signum:
                    start = time.monotonic()
                    child.send_signal(signum)
                child.communicate(timeout=18)
                elapsed = time.monotonic() - start
                reason = 'cancelled' if signum else 'suite_budget'
                check_report(name, child.returncode, ['BLOCKED', 'BLOCKED'], [reason, reason], 128 + signum if signum else 1)
                problems = []
                if not initialized:
                    problems.append('no initialized report while first metadata child was active')
                if elapsed > (5.5 if signum else 6):
                    problems.append('metadata exceeded budget/signal cleanup allowance: %.3fs' % elapsed)
                calls = (root / 'metadata-calls').read_text().splitlines()
                if calls != ['git rev-parse HEAD']:
                    problems.append('suffix metadata launched: ' + repr(calls))
                try:
                    os.kill(observed_pid, 0)
                except ProcessLookupError:
                    pass
                else:
                    problems.append('metadata descendant still exists')
                # Past this bounded descendant's planned write, including fast cleanup cases.
                time.sleep(max(0, 5.2 - (time.monotonic() - start)))
                if (root / 'metadata-late').exists():
                    problems.append('metadata delayed descendant marker survived')
                assert not (root / 'should-not-run').exists() and not (root / 'later-started').exists()
                assert not problems, '; '.join(problems)
                return 'initial report retained; active metadata cancelled/budgeted; no suffix metadata/descendant; %.3fs' % elapsed
            finally:
                if child.poll() is None:
                    os.killpg(child.pid, signal.SIGKILL)
                    child.communicate()
                if observed_pid:
                    try:
                        os.kill(observed_pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                    try:
                        os.waitpid(observed_pid, 0)
                    except ChildProcessError:
                        pass
        control('metadata-suite-deadline', lambda: slow_metadata('metadata-suite-deadline'))
        control('metadata-sigint', lambda: slow_metadata('metadata-sigint', signal.SIGINT))
        control('metadata-sigterm', lambda: slow_metadata('metadata-sigterm', signal.SIGTERM))

        def failure():
            root = fixture('failure')
            code, _ = run_capture(command(root, 'failure'), REPO)
            rows = check_report('failure', code, ['FAIL', 'PASS'])
            assert rows[0]['exit_code'] == 7 and (root / 'later-started').exists()
            return 'actual exit 7 retained; later actual control passed; 50/51 denominator retained'
        control('failure-propagation', failure)

        def missing():
            root = fixture('missing')
            (root / 'plan/tests/firstrun/run.sh').unlink()
            code, _ = run_capture(command(root, 'missing'), REPO)
            check_report('missing', code, ['MISSING', 'PASS'])
            assert (root / 'later-started').exists()
            return 'missing runner retained as MISSING; later control passed'
        control('missing-required-stage', missing)

        process_body = '''python3 - <<'CONTROL'
import os
from pathlib import Path
import subprocess
import sys
import time
child = subprocess.Popen([sys.executable, '-c', "import time; from pathlib import Path; time.sleep(5); Path('delayed-marker').write_text('survived')"])
Path('child-pid').write_text(str(child.pid))
Path('ready').write_text('ready')
print('CONTROL_READY', flush=True)
time.sleep(60)
CONTROL
'''
        def no_descendant(root):
            pid = int((root / 'child-pid').read_text())
            try:
                os.kill(pid, 0)
            except ProcessLookupError:
                pass
            else:
                raise AssertionError('owned descendant still exists: %s' % pid)
            # Past the bounded child's planned marker deadline, with a finite control wait.
            time.sleep(5.1)
            assert not (root / 'delayed-marker').exists()

        def deadline(name, suite=False):
            root = fixture(name, process_body)
            started = time.monotonic()
            code, _ = run_capture(command(root, name, ['--stage-timeout', '30' if suite else '1', '--suite-timeout', '2' if suite else '30']), REPO, timeout=12)
            assert time.monotonic() - started < 8, 'earlier deadline/cleanup exceeded bound'
            check_report(name, code, ['TIMEOUT', 'BLOCKED' if suite else 'PASS'], ['suite_budget', 'suite_budget'] if suite else ['stage_budget', None])
            assert (root / 'later-started').exists() != suite
            no_descendant(root)
            return 'earlier deadline classified; group killed/reaped; no delayed child marker'
        control('stage-timeout-cleanup', lambda: deadline('stage-timeout'))
        control('active-suite-deadline', lambda: deadline('suite-timeout', True))

        def interruption(signum):
            name = signal.Signals(signum).name
            root = fixture(name, process_body)
            child = subprocess.Popen(command(root, name), cwd=REPO, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, start_new_session=True)
            try:
                until = time.monotonic() + 15
                while not (root / 'ready').exists():
                    assert child.poll() is None and time.monotonic() < until, 'no readiness before aggregate exited/deadline'
                    time.sleep(.02)
                child.send_signal(signum)
                data, _ = child.communicate(timeout=8)
                check_report(name, child.returncode, ['CANCELLED', 'BLOCKED'], ['signal:' + name, 'cancelled'], 128 + signum)
                assert not (root / 'later-started').exists()
                no_descendant(root)
            finally:
                if child.poll() is None:
                    os.killpg(child.pid, signal.SIGKILL)
                    child.communicate()
            return 'readiness-coordinated aggregate signal; exact exit/reasons; no surviving child'
        control('sigint-finalization', lambda: interruption(signal.SIGINT))
        control('sigterm-finalization', lambda: interruption(signal.SIGTERM))

        def bounded():
            root = fixture('bounded', "python3 -c \"import sys; sys.stdout.write('H' * 2097152); sys.stdout.write('TAIL\\\\n'); sys.exit(7)\"\n")
            code, _ = run_capture(command(root, 'bounded'), REPO)
            rows = check_report('bounded', code, ['FAIL', 'PASS'])
            assert rows[0]['exit_code'] == 7 and rows[0]['log_truncated'] and rows[0]['log_bytes_seen'] > 2 * LIMIT
            log = (output / 'bounded' / rows[0]['log_path']).read_bytes()
            assert len(log) <= LIMIT and log.startswith(b'HHHH') and b'TAIL' in log[-100:]
            return 'head/tail retained <=1 MiB after >2 MiB output; exit 7 preserved'
        control('bounded-log', bounded)

        def interfaces():
            root = fixture('interfaces', 'touch should-not-run\n')
            empty = scratch / 'no-tools'
            code, listing = run_capture(['bash', str(RUNNER), '--list'], root, dict(env, UPSTREAM_UNIT_TOOLS=str(empty)))
            assert code == 0 and 'AUTH01B.loginvm' in listing and '50 logical / 51 executable' in listing
            for bad in [['--only', ''], ['--only', 'bad'], ['--stage-timeout', 'nan'], ['--suite-timeout', '0'], ['--list', '--only', 'LM01']]:
                code, _ = run_capture(['bash', str(RUNNER)] + bad, root, env)
                assert code == 2
            code, _ = run_capture(command(root, 'no-sdk') + ['--tools-root', str(empty)], REPO)
            check_report('no-sdk', code, ['BLOCKED', 'BLOCKED'])
            assert not (root / 'should-not-run').exists()
            stab = root / 'plan/tests/stab'; stab.mkdir()
            (stab / 'run.sh').write_text('echo CONTROL_STAB\n')
            code, _ = run_capture(['bash', str(RUNNER), '--repo-root', str(root), '--tools-root', str(empty), '--only', 'batch:firstrun', '--only', 'batch:stab', '--output', str(output / 'independent-stab')], REPO)
            check_report('independent-stab', code, ['BLOCKED', 'PASS'])
            (root / 'global.json').write_text('{"sdk":{"version":"99.0.999","rollForward":"disable"}}')
            code, _ = run_capture(command(root, 'wrong-sdk'), REPO)
            check_report('wrong-sdk', code, ['BLOCKED', 'BLOCKED'])
            assert not (root / 'should-not-run').exists()
            (root / 'global.json').unlink()
            # Output preservation and report-write failure are invocation errors, never PASS.
            old = output / 'already-used'; old.mkdir(); (old / 'sentinel').write_text('keep')
            code, _ = run_capture(command(root, 'already-used'), REPO)
            assert code == 2 and (old / 'sentinel').read_text() == 'keep'
            file = output / 'output-file'; file.write_text('keep')
            code, _ = run_capture(command(root, 'output-file'), REPO)
            assert code == 2 and file.read_text() == 'keep'
            code, _ = run_capture(command(root, 'nested-source') + ['--output', str(scratch)], REPO)
            assert code == 2
            for script in ['run.sh', 'run-auth01b.sh']:
                for options in [['--stage', 'bad'], ['--bogus'], ['--help']]:
                    code, _ = run_capture(['bash', str(REPO / 'plan/tests' / script)] + options, root, dict(env, UPSTREAM_UNIT_TOOLS=str(empty)))
                    assert code == (0 if options == ['--help'] else 2)
            return 'list/invalid/output preservation/missing exact SDK/wrong exact SDK/help paths validated'
        control('interfaces-and-preflight', interfaces)

        def inventory():
            root = fixture('inventory', 'exit 0\n')
            folder = root / 'plan/tests/unreviewed'; folder.mkdir(); (folder / 'run.sh').write_text('exit 0\n')
            code, _ = run_capture(command(root, 'inventory'), REPO)
            rows = check_report('inventory', code, ['PASS', 'PASS'])
            report = json.loads((output / 'inventory/results.json').read_text())
            assert report['inventory_errors'] and report['suite_status'] == 'FAIL'
            return 'unexpected runnable directory fails inventory while known stages execute'
        control('inventory-drift', inventory)

        def protocol():
            root = fixture('protocol', 'exit 0\n')
            (root / 'plan/tests/run.sh').write_text('exit 0\n')
            (root / 'plan/tests/LM05.cs').write_text('// orchestration fixture only\n')
            code, _ = run_capture(['bash', str(RUNNER), '--repo-root', str(root), '--tools-root', str(tools), '--only', 'LM05', '--output', str(output / 'protocol')], REPO)
            report = json.loads((output / 'protocol/results.json').read_text())
            row = next(r for r in report['stages'] if r['id'] == 'LM05')
            assert code == 1 and row['status'] == 'ERROR' and row['reason'] == 'missing_completion_marker'
            assert not report['complete_required'] and report['status_counts']['NOT_SELECTED'] == 50
            return 'zero-exit selector without actual completion marker fails protocol'
        control('completion-marker-protocol', protocol)

        def selectors():
            root = scratch / 'real-snapshot'; root.mkdir()
            code, files = run_capture(['git', 'ls-files', '-z', 'src', 'plan/tests'], REPO)
            assert code == 0
            paths = files.split('\0')
            # git ls-files output can exceed the bounded generic diagnostic capture.
            paths = subprocess.check_output(['git', 'ls-files', '-z', 'src', 'plan/tests'], cwd=REPO).decode().split('\0')
            integrity = {}
            for item in paths:
                if not item:
                    continue
                source = REPO / item
                if source.is_file():
                    destination = root / item; destination.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(source, destination)
                    integrity[item] = hashlib.sha256(source.read_bytes()).hexdigest()
                    assert hashlib.sha256(destination.read_bytes()).hexdigest() == integrity[item]
            # New owned files need not yet be tracked during local acceptance.
            for item in ['run.sh', 'run-auth01b.sh']:
                shutil.copy2(REPO / 'plan/tests' / item, root / 'plan/tests' / item)
            (output / 'selector-input-hashes.json').write_text(json.dumps(integrity, sort_keys=True, indent=2) + '\n')
            details = []
            for name, broken, script, selected, marker in [('lm', 'LM01.cs', 'run.sh', 'LM05', 'LM05'), ('auth', 'AUTH01B.cs', 'run-auth01b.sh', 'loginvm', 'AUTH01B.loginvm')]:
                path = root / 'plan/tests' / broken; original = path.read_bytes()
                path.write_bytes(b'#error deliberate scratch-only prefix failure\n' + original)
                code, log = run_capture(['bash', str(root / 'plan/tests' / script)], root, env)
                (output / (name + '-default-negative.log')).write_text(log)
                assert code != 0 and 'deliberate scratch-only prefix failure' in log
                assert 'PROBE_PHASE ' + marker + ' complete' not in log
                assert ('LM-05 actual-source parser' not in log if name == 'lm' else 'LoginVM lifecycle probes passed:' not in log), 'default ran a suffix after its failed prefix'
                code, log = run_capture(['bash', str(root / 'plan/tests' / script), '--stage', selected], root, env)
                (output / (name + '-independent.log')).write_text(log)
                assert code == 0 and 'PROBE_PHASE ' + marker + ' complete' in log, log[-4000:]
                assert ('checks passed: 22' in log if name == 'lm' else 'LoginVM lifecycle probes passed: 39' in log), log[-4000:]
                details.append(log[-1500:])
                path.write_bytes(original)
            partial = ['bash', str(RUNNER), '--repo-root', str(root), '--tools-root', str(tools),
                       '--only', 'AUTH01B.loginvm', '--only', 'AUTH01B.loginvm', '--output', str(output / 'auth-partial')]
            code, _ = run_capture(partial, REPO, env)
            report = json.loads((output / 'auth-partial/results.json').read_text())
            assert code == 0 and report['suite_status'] == 'PARTIAL_PASS' and not report['complete_required']
            assert report['selected_totals'] == {'logical': 1, 'executable': 1}
            assert report['logical_status']['AUTH01B'] == 'PARTIAL' and report['logical_pass'] == 0
            assert report['status_counts']['NOT_SELECTED'] == 50 and report['status_counts']['PASS'] == 1
            assert not any(value for key, value in report['status_counts'].items() if key not in ('NOT_SELECTED', 'PASS'))
            assert all(hashlib.sha256((root / p).read_bytes()).hexdigest() == digest for p, digest in integrity.items())
            return 'byte-identical source hashes retained; broken LM01/Account defaults fail, real LM05/LoginVM independently PASS: ' + repr(details)
        control('real-selector-independence', selectors)
    assert not any(p.name in ('bin', 'obj', 'src', 'cache') for p in output.rglob('*'))
    return 0 if results and all(r['status'] == 'PASS' for r in results) else 1


if __name__ == '__main__':
    sys.exit(main())
