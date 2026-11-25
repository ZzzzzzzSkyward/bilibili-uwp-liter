#!/usr/bin/env python3
"""Portable checks of actual tool code. No native measurement or fake-host bypass.

python3 tools/tests/video-memory/run.py --powershell /path/to/pwsh --output-directory /fresh/scratch
All generated metadata/artifacts/process identities are explicitly synthetic.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--powershell', required=True)
    parser.add_argument('--output-directory', type=Path, required=True)
    args = parser.parse_args()
    out = args.output_directory.resolve()
    out.mkdir(parents=True, exist_ok=False)
    tool = Path(__file__).resolve().parents[2] / 'measure-video-memory.ps1'
    records = []

    def command(label, tail, expect=0, contains=None):
        cmd = [args.powershell, '-NoLogo', '-NoProfile', '-File', *map(str, tail)]
        result = subprocess.run(cmd, text=True, capture_output=True, timeout=60)
        records.append(dict(label=label, command=cmd, exit=result.returncode,
                            stdout=result.stdout, stderr=result.stderr))
        (out / 'commands.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
        assert (result.returncode == 0) == (expect == 0), (label, result.stdout, result.stderr)
        if contains:
            assert contains in result.stdout + result.stderr, (label, result.stdout, result.stderr)
        return result

    artifact = out / 'SYNTHETIC-NOT-A-PACKAGE.bin'
    artifact.write_bytes(b'Synthetic artifact binding fixture. Not a Windows package.\n')
    media = [f'SYNTHETIC-media-{n}:part1' for n in range(1, 51)]
    fixture = {name: 'SYNTHETIC-controlled-label' for name in (
        'comparisonGroup', 'shell', 'accountLabel', 'network', 'requestedEngine',
        'actualEngine', 'requestedQuality', 'actualQuality', 'codec', 'resolution',
        'frameRate', 'playbackRate', 'volume', 'subtitles', 'danmaku', 'autoplay',
        'apiCapture', 'entryRoute', 'startingPosition', 'cacheState')}
    fixture.update(journey='S', replicate=1, repeatOrder=1, mediaOrder=media,
                   mediaListSha256=hashlib.sha256(json.dumps(media, separators=(',', ':')).encode()).hexdigest(),
                   expectedCycles=list(range(1, 51)), playbackSeconds=30,
                   betweenCloseSeconds=10, timeoutSeconds=60, warmupCycles=5, warmupIdleSeconds=60)
    meta = dict(schemaVersion=1, runId='SYNTHETIC-portable-tests', evidenceLevel='E2',
                createdUtc='2026-11-25T12:00:00.0000000Z', sourceCommit='unknown', sourceDirty=True,
                artifactPath=str(artifact), artifactSha256=hashlib.sha256(artifact.read_bytes()).hexdigest(),
                appVersion='SYNTHETIC', configuration='SYNTHETIC', architecture='SYNTHETIC', runtime='SYNTHETIC',
                package=dict(fullName='SYNTHETIC', familyName='SYNTHETIC', appId='SYNTHETIC', executable='SYNTHETIC'),
                process=dict(id=424242, startUtc='2026-11-25T12:00:00.0000000Z', path='SYNTHETIC',
                             verificationMethod='Synthetic metadata only; never a native PID observation.'),
                device={key: dict(status='unavailable', reason='Synthetic portable fixture.')
                        for key in ('osBuild', 'ram', 'cpu', 'gpu', 'driver', 'power', 'display', 'scaling')},
                fixture=fixture, profiler=dict(mode='none-synthetic', version='none-synthetic'),
                units=dict(privateBytes='bytes', workingSetBytes='bytes', handleCount='count',
                           threadCount='count', cpuTotalMs='milliseconds'),
                outputs=dict(samples='samples.ndjson', checkpoints='checkpoints.ndjson', events='events.ndjson'),
                deviations=['All inputs synthetic. No Windows/native measurement.'])
    metadata = out / 'synthetic-metadata.json'
    metadata.write_text(json.dumps(meta), encoding='utf-8')
    base = [tool, '-MetadataPath', metadata]
    command('metadata-valid', [*base, '-Mode', 'ValidateMetadata'], contains='Metadata valid')
    bad_cases = {
        'unit': lambda m: m['units'].update(privateBytes='MB'),
        'unit-array': lambda m: m['units'].update(privateBytes=['bytes']),
        'schema-string': lambda m: m.update(schemaVersion='1'),
        'missing': lambda m: m.pop('runtime'),
        'artifact': lambda m: m.update(artifactSha256='0' * 64),
        'list-hash': lambda m: m['fixture'].update(mediaListSha256='0' * 64),
        'cycle-order': lambda m: m['fixture']['expectedCycles'].reverse(),
        'cycle-string': lambda m: m['fixture']['expectedCycles'].__setitem__(0, '1'),
        'date-local': lambda m: m['process'].update(startUtc='2026-11-25T12:00:00'),
        'pid-string': lambda m: m['process'].update(id='424242'),
        'nonfinite': lambda m: m['fixture'].update(playbackSeconds=float('inf')),
        'path-traversal': lambda m: m['outputs'].update(events='../events.ndjson'),
        'unavailable-no-reason': lambda m: m['device'].update(gpu=dict(status='unavailable', reason='')),
    }
    for name, mutate in bad_cases.items():
        bad = copy.deepcopy(meta)
        mutate(bad)
        path = out / f'bad-{name}.json'
        path.write_text(json.dumps(bad), encoding='utf-8')
        command(f'metadata-reject-{name}', [tool, '-Mode', 'ValidateMetadata', '-MetadataPath', path], expect=1)
    broken = out / 'broken.json'
    broken.write_text('{', encoding='utf-8')
    command('metadata-invalid-json', [tool, '-Mode', 'ValidateMetadata', '-MetadataPath', broken], expect=1)
    target = ['-TargetProcessId', '424242', '-ExpectedProcessStartUtc', meta['process']['startUtc']]
    for phase in ('prewarm', 'baseline', 'opened', 'post-close', 'settled', 'idle60', 'idle300',
                  'background', 'minimized', 'restored', 'feed-step'):
        path = out / f'host-reject-{phase}'
        command(f'checkpoint-native-host-{phase}', [*base, '-Mode', 'Checkpoint', '-RunDirectory', path,
                                                   *target, '-Phase', phase], expect=1, contains='Windows process measurement required')
        assert not path.exists()
    command('collect-native-host', [*base, '-Mode', 'Collect', '-RunDirectory', out / 'host-collect', *target],
            expect=1, contains='Windows process measurement required')
    assert not (out / 'host-collect').exists()
    command('invalid-phase-before-metadata-read', [tool, '-Mode', 'Checkpoint', '-MetadataPath', out / 'absent',
                                                 '-Phase', 'not-a-phase'], expect=1, contains='ValidateSet')
    for flag, value in (('-IntervalSeconds', '0'), ('-DurationSeconds', '-1'), ('-IntervalSeconds', 'NaN')):
        command(f'bad-{flag}-{value}', [*base, '-Mode', 'Collect', *target, flag, value], expect=1)
    run = out / 'marker-run'
    mark = [*base, '-Mode', 'Mark', '-RunDirectory', run, *target]
    command('mark-first', [*mark, '-Phase', 'prewarm', '-Action', 'observation', '-Note', 'SYNTHETIC manual marker'])
    for file in ('metadata.json', 'binding.json', 'events.ndjson'):
        assert (run / file).is_file()
    assert not (run / 'samples.ndjson').exists()
    before = (run / 'events.ndjson').read_bytes()
    command('duplicate-marker', [*mark, '-Phase', 'prewarm', '-Action', 'observation'], expect=1, contains='Duplicate observation')
    command('case-variant-duplicate-marker', [*mark, '-Phase', 'PREWARM', '-Action', 'OBSERVATION'], expect=1, contains='Duplicate observation')
    command('incomplete-run-completion', [*mark, '-Cycle', '50', '-Phase', 'idle300', '-Action', 'run-complete',
                                         '-CompletedCycles', '50', '-Status', 'ok'], expect=1, contains='Incomplete run')
    command('target-mismatch', [*base, '-Mode', 'Mark', '-RunDirectory', run, '-TargetProcessId', '1',
                                '-ExpectedProcessStartUtc', meta['process']['startUtc'], '-Phase', 'baseline',
                                '-Action', 'observation'], expect=1, contains='does not match metadata')
    assert before == (run / 'events.ndjson').read_bytes()
    (run / 'events.ndjson.lock').write_text('Synthetic abandoned owner; must not be overwritten.')
    command('occupied-lock', [*mark, '-Cycle', '1', '-Phase', 'opened', '-Action', 'open-request'], expect=1)
    assert (run / 'events.ndjson.lock').exists()
    (run / 'events.ndjson.lock').unlink()
    command('mark-next', [*mark, '-Cycle', '1', '-Phase', 'settled', '-Action', 'cycle-complete',
                          '-CompletedCycles', '1', '-Status', 'ok'])
    rows = [json.loads(line) for line in (run / 'events.ndjson').read_text().splitlines()]
    assert [r['sequence'] for r in rows] == [1, 2]
    assert rows[0]['visibleTabs'] is None and rows[0]['observedPlayers'] is None
    assert rows[1]['completedCycles'] == 1
    command('backward-cycle', [*mark, '-Phase', 'baseline', '-Action', 'observation'], expect=1, contains='Cycle moved backwards')
    command('completed-backwards', [*mark, '-Cycle', '2', '-Phase', 'opened', '-Action', 'open-request'],
            expect=1, contains='CompletedCycles moved backwards')
    fresh = out / 'preexisting-unbound'
    fresh.mkdir()
    command('unbound-existing-directory', [*base, '-Mode', 'Mark', '-RunDirectory', fresh, *target,
                                          '-Phase', 'prewarm', '-Action', 'observation'], expect=1)
    assert list(fresh.iterdir()) == []
    # Tampered metadata and malformed prior output are rejected without changing observations.
    bound_metadata = run / 'metadata.json'
    prior = bound_metadata.read_bytes()
    bound_metadata.write_bytes(prior + b' ')
    command('metadata-binding-tamper', [*mark, '-Cycle', '2', '-Phase', 'opened', '-Action', 'open-request',
                                       '-CompletedCycles', '1'], expect=1, contains='Run binding changed')
    bound_metadata.write_bytes(prior)
    stream = run / 'events.ndjson'
    prior = stream.read_bytes()
    stream.write_bytes(prior + b'{')
    corrupt = stream.read_bytes()
    command('partial-line-preservation', [*mark, '-Cycle', '2', '-Phase', 'opened', '-Action', 'open-request',
                                         '-CompletedCycles', '1'], expect=1)
    assert stream.read_bytes() == corrupt
    stream.write_bytes(prior)
    marker_counter = out / 'marker-public-call-count.txt'
    marker_forwarder = out / 'count-marker-forwarder.ps1'
    ps_quote = lambda value: "'" + str(value).replace("'", "''") + "'"
    marker_forwarder.write_text(
        '[IO.File]::AppendAllText(' + ps_quote(marker_counter) + ',"1`n")\n& ' + ps_quote(tool) + ' @args\n',
        encoding='utf-8')
    command('complete-synthetic-marker-sequence', [Path(__file__).with_name('markers.ps1'), '-Tool', marker_forwarder,
                                                  '-MetadataPath', metadata, '-RunDirectory', out / 'complete-marker-run'],
            contains='PASS')
    marker_helper_calls = len(marker_counter.read_text().splitlines())
    assert marker_helper_calls == 55
    complete_rows = [json.loads(line) for line in (out / 'complete-marker-run/events.ndjson').read_text().splitlines()]
    assert len(complete_rows) == 52
    assert [r['cycle'] for r in complete_rows if r['action'] == 'cycle-complete'] == list(range(1, 51))
    assert complete_rows[1]['visibleTabs'] == 0 and complete_rows[1]['observedPlayers'] == 0
    assert (out / 'complete-marker-run/STOPPED.json').is_file()
    controlled = out / 'controlled'
    controlled.mkdir()
    command('actual-private-functions-controlled', [Path(__file__).with_name('controlled.ps1'), '-Tool', tool,
                                                   '-OutputDirectory', controlled], contains='PASS')
    revisions = out / 'revisions'
    command('t1-t3-actual-code-regressions', [Path(__file__).with_name('revisions.ps1'), '-Tool', tool,
                                           '-MetadataPath', metadata, '-OutputDirectory', revisions],
            contains='failures: 0')
    revision_results = json.loads((revisions / 'revision-results.json').read_text(encoding='utf-8-sig'))
    assert revision_results['status'] == 'PASS' and revision_results['failedCount'] == 0
    # Every command and source hash is retained outside the repository.
    (out / 'commands.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    result = dict(status='PASS', cliCommands=len(records), controlledGroups=json.loads(
        (controlled / 'controlled-results.json').read_text(encoding='utf-8-sig')),
        revisionChecks=len(revision_results['checks']), markerHelperCalls=marker_helper_calls,
        acceptedMarkerRows=len(complete_rows),
        toolSha256=hashlib.sha256(tool.read_bytes()).hexdigest(), native='BLOCKED: Linux/Core; Windows 5.1 not executed')
    (out / 'results.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(f'PASS {len(records)} CLI/controlled invocations; native Windows checks BLOCKED. Results: {out}')


if __name__ == '__main__':
    main()
