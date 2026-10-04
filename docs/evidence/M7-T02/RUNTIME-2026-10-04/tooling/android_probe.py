import base64, datetime, hashlib, json, subprocess, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'android'
OUT.mkdir(exist_ok=True)
ADB = ROOT / 'android-sdk/platform-tools/adb.exe'
SERIAL = 'emulator-5560'
PACKAGE = 'com.DefaultCompany.urp_2d'
SAVE = '/storage/emulated/0/Android/data/' + PACKAGE + '/files/startup-life.json'

def adb(*args, check=True):
    result = subprocess.run([str(ADB), '-P', '5038', '-s', SERIAL, *args], capture_output=True, timeout=210 if 'screenrecord' in args else 30)
    with (OUT / 'commands.ndjson').open('a', encoding='utf-8') as log:
        log.write(json.dumps({'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'serial': SERIAL, 'port': 5038, 'args': args, 'exit': result.returncode, 'stderr': result.stderr.decode(errors='replace')}) + '\n')
    if check and result.returncode:
        raise RuntimeError(result.stderr.decode(errors='replace') + result.stdout.decode(errors='replace'))
    return result.stdout

def save(label):
    raw = adb('exec-out', 'cat', SAVE)
    env = json.loads(raw)
    payload = base64.b64decode(env['PayloadBase64'])
    assert hashlib.sha256(payload).hexdigest() == env['Checksum']
    state = json.loads(payload)
    (OUT / (label + '.save.json')).write_bytes(raw)
    row = {'label': label, 'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'sha256': hashlib.sha256(raw).hexdigest(), 'schema': env.get('SchemaVersion'), 'state': state}
    with (OUT / 'observed-states.ndjson').open('a', encoding='utf-8') as log:
        log.write(json.dumps(row, ensure_ascii=False) + '\n')
    print(json.dumps({'label': label, 'run': state['RunId'], 'revision': state['Revision'], 'date': state['DateIso'], 'minute': state['Minute'], 'cursor': state['PlaybackCursor'], 'activity': state['CurrentActivity'], 'xp': (state.get('Employment') or {}).get('Xp'), 'course': state.get('Course'), 'cash': state['Cash'], 'claims': len(state['Claims'])}, ensure_ascii=False))
    return state

def screenshot(label):
    (OUT / (label + '.png')).write_bytes(adb('exec-out', 'screencap', '-p'))

def launch():
    print(adb('shell', 'monkey', '-p', PACKAGE, '-c', 'android.intent.category.LAUNCHER', '1').decode(errors='replace'))

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'info':
        print(adb('shell', 'getprop').decode())
    elif mode == 'install':
        print(adb('install', '-r', str(OUT / 'StartupLife-M7T02.apk')).decode())
    elif mode == 'launch':
        launch()
    elif mode == 'state':
        save(sys.argv[2])
    elif mode == 'screenshot':
        screenshot(sys.argv[2])
    elif mode == 'tap':
        adb('shell', 'input', 'tap', sys.argv[2], sys.argv[3])
    elif mode == 'kill':
        adb('shell', 'am', 'force-stop', PACKAGE)
    elif mode == 'home':
        adb('shell', 'input', 'keyevent', 'KEYCODE_HOME')
    elif mode == 'workpause':
        baseline = save('B-before-work')
        adb('shell', 'input', 'tap', sys.argv[2], sys.argv[3])
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            raw = adb('exec-out', 'cat', SAVE)
            s = json.loads(base64.b64decode(json.loads(raw)['PayloadBase64']))
            if s.get('Employment', {}).get('Xp', 0) > baseline['Employment']['Xp'] and s['PlaybackCursor'] == 0:
                adb('shell', 'input', 'keyevent', 'KEYCODE_HOME')
                save('B-background')
                break
        else:
            raise RuntimeError('Did not capture a committed unacknowledged cue; do not claim B PASS.')
    elif mode == 'files':
        print(adb('shell', 'ls', '-la', SAVE.rsplit('/', 1)[0]).decode())
    elif mode == 'record':
        adb('shell', 'screenrecord', '--bit-rate', '2000000', '--time-limit', '180', '/sdcard/m7-t02-lifecycle.mp4')
    elif mode == 'recordstop':
        adb('shell', 'pkill', '-2', 'screenrecord', check=False)
    elif mode == 'recordpull':
        print(adb('pull', '/sdcard/m7-t02-lifecycle.mp4', str(OUT / 'lifecycle-emulator.mp4')).decode())
    elif mode == 'fault':
        # Exclusively the disposable emulator's test-app files, with the app stopped.
        kind = sys.argv[2]
        if kind == 'corrupt':
            command = 'cp ' + SAVE + ' ' + SAVE + '.backup && printf corrupt-primary > ' + SAVE
        elif kind == 'unreadable':
            command = 'cp ' + SAVE + ' ' + SAVE + '.backup && rm ' + SAVE + ' && mkdir ' + SAVE
        elif kind == 'both':
            command = 'rmdir ' + SAVE + ' && printf corrupt-primary > ' + SAVE + ' && printf corrupt-backup > ' + SAVE + '.backup'
        else:
            raise ValueError(kind)
        print(adb('shell', 'sh', '-c', "'" + command + "'").decode())
