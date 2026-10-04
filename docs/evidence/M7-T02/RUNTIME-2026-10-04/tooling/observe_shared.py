import base64, ctypes, hashlib, json, time
from ctypes import wintypes
from pathlib import Path

root = Path(__file__).resolve().parent
out = root / 'shared-observed-saves'
out.mkdir(exist_ok=True)
primary = Path.home() / 'AppData/LocalLow/Startup Life/Startup Life/startup-life.json'
kernel = ctypes.WinDLL('kernel32', use_last_error=True)
kernel.CreateFileW.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, ctypes.c_void_p, wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE]
kernel.CreateFileW.restype = wintypes.HANDLE
kernel.ReadFile.argtypes = [wintypes.HANDLE, ctypes.c_void_p, wintypes.DWORD, ctypes.POINTER(wintypes.DWORD), ctypes.c_void_p]
kernel.CloseHandle.argtypes = [wintypes.HANDLE]

def shared_read(path):
    # FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE. Never block an atomic replacement.
    handle = kernel.CreateFileW(str(path), 0x80000000, 7, None, 3, 0x80, None)
    if handle == ctypes.c_void_p(-1).value:
        raise OSError(ctypes.get_last_error())
    try:
        buf = ctypes.create_string_buffer(2 * 1024 * 1024)
        count = wintypes.DWORD()
        if not kernel.ReadFile(handle, buf, len(buf), ctypes.byref(count), None):
            raise OSError(ctypes.get_last_error())
        return buf.raw[:count.value]
    finally:
        kernel.CloseHandle(handle)

names = {'Pause Before Ack', 'Pause After Ack', 'Course Restore', 'Next Day', 'Backup Recovery', 'Unreadable Primary', 'Both Invalid', 'Wall Clock'}
seen = set()
with (root / 'shared-observed-states.ndjson').open('w', encoding='utf-8') as log:
    deadline = time.monotonic() + 180
    while time.monotonic() < deadline:
        try:
            raw = shared_read(primary)
            env = json.loads(raw)
            data = base64.b64decode(env['PayloadBase64'])
            s = json.loads(data)
            h = hashlib.sha256(raw).hexdigest()
            if s.get('Name') in names and h not in seen and hashlib.sha256(data).hexdigest() == env['Checksum']:
                seen.add(h)
                path = out / (s['RunId'] + '-' + str(s['Revision']) + '.json')
                path.write_bytes(raw)
                row = {'observedUtcNs': time.time_ns(), 'head': '79b3f0bc35ff9c32e45b51fe04715d253bd3abd5', 'sha256': h, 'file': path.name, 'name': s['Name'], 'runId': s['RunId'], 'revision': s['Revision'], 'date': s['DateIso'], 'minute': s['Minute'], 'activityId': s['CurrentActivity'], 'cursor': s['PlaybackCursor'], 'careerXp': (s.get('Employment') or {}).get('Xp', 0), 'cash': s['Cash'], 'skills': s['Skills'], 'claims': s['Claims'], 'course': s.get('Course'), 'history': s['History'], 'grants': s['Grants']}
                log.write(json.dumps(row, ensure_ascii=False) + '\n')
                log.flush()
        except (OSError, ValueError, KeyError):
            pass
        time.sleep(.001)
