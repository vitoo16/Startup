"""Audit the published artifacts without executing Unity or altering game state."""
from pathlib import Path
import base64, hashlib, json, sys, xml.etree.ElementTree as ET

B = Path(__file__).resolve().parents[2]
repair = '--repair-xml' in sys.argv
summary = {'xml': {}, 'saveEnvelopes': 0, 'portraitCaptures': 0}
for path in sorted(B.glob('*.xml')):
    raw = path.read_bytes()
    corrected = raw.replace(b'<USER>', b'[USER]').replace(b'<PROJECT>', b'[PROJECT]')
    if repair and corrected != raw:
        path.write_bytes(corrected)
    root = ET.fromstring(corrected if repair else raw)
    cases = root.findall('.//test-case')
    project = [c for c in cases if c.get('fullname', '').startswith('StartupLife.')]
    summary['xml'][path.name] = {
        'runnerTotal': int(root.get('total')),
        'projectTotal': len(project),
        'projectPassed': sum(c.get('result') == 'Passed' for c in project),
        'projectFailed': sum(c.get('result') == 'Failed' for c in project),
        'projectSkipped': sum(c.get('result') == 'Skipped' for c in project),
        'cases': [{'name': c.get('fullname'), 'result': c.get('result'), 'duration': c.get('duration')} for c in project]
    }
for filename, expected in [('editmode-final-results.xml', 8), ('playmode-final-results.xml', 11)]:
    result = summary['xml'][filename]
    assert result['projectTotal'] == result['projectPassed'] == expected
    assert result['projectFailed'] == result['projectSkipped'] == 0
assert summary['xml']['fatal-failing.xml']['projectFailed'] == 2
assert summary['xml']['nextday-visual-failing.xml']['projectFailed'] == 1

for path in list(B.rglob('*.save.json')) + list((B/'shared-observed-saves').glob('*.json')):
    envelope = json.loads(path.read_bytes())
    payload = base64.b64decode(envelope['PayloadBase64'])
    assert hashlib.sha256(payload).hexdigest() == envelope['Checksum'], path
    assert envelope['SchemaVersion'] == 1, path
    summary['saveEnvelopes'] += 1

rows = {row['label']: row['state'] for row in map(json.loads, (B/'android-final/observed-states.ndjson').read_text().splitlines())}
def equal(a, b, ignored=()):
    assert {k:v for k,v in a.items() if k not in ignored} == {k:v for k,v in b.items() if k not in ignored}
equal(rows['B-background'], rows['B-after-wait'])
equal(rows['B-background'], rows['B-restored-acknowledged'], ('Revision','NextOperation','PlaybackCursor','Receipts'))
assert rows['B-background']['PlaybackCursor'] == 0 and rows['B-restored-acknowledged']['PlaybackCursor'] == 1
equal(rows['B-shift-complete'], rows['B-after-ack-restart'])
equal(rows['C-before-restart'], rows['C-after-restore'])
equal(rows['D-before-restart'], rows['D-after-restore'])
equal(rows['E-next-write'], rows['E-second-reload'])
equal(rows['E-second-reload'], rows['Z-restored-valid'])
assert rows['E-next-write']['Revision'] == rows['D-before-restart']['Revision'] + 1
summary['androidPayloadComparisons'] = 'PASS'

for path in sorted((B/'visuals').glob('0[1-8]-*.png')):
    raw = path.read_bytes()
    assert raw[:8] == b'\x89PNG\r\n\x1a\n'
    assert int.from_bytes(raw[16:20], 'big') == 1080
    assert int.from_bytes(raw[20:24], 'big') == 1920
    summary['portraitCaptures'] += 1
assert summary['portraitCaptures'] == 8
video = json.loads((B/'android-final/lifecycle-emulator.metadata.json').read_text())
assert hashlib.sha256((B/'android-final/lifecycle-emulator.mp4').read_bytes()).hexdigest() == video['sha256']
assert video['fullDecodeExit'] == 0 and video['durationSeconds'] > 60
summary['recording'] = {'seconds': video['durationSeconds'], 'sha256': video['sha256'], 'decodeReceipt': 'PASS'}
acceptance = json.loads((B/'android-final/acceptance.json').read_text())
assert acceptance['physicalAndroid'] == acceptance['iOS'] == 'NOT RUN'
summary['externalBlockers'] = {'physicalAndroid':'NOT RUN','iOS':'NOT RUN'}

if '--output' in sys.argv:
    destination = Path(sys.argv[sys.argv.index('--output') + 1])
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k != 'xml'}))
print(json.dumps({k:{x:y for x,y in v.items() if x!='cases'} for k,v in summary['xml'].items()}))
