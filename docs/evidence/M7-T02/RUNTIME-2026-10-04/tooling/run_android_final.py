import android_probe as p
import json, time, threading, hashlib
from PIL import Image
from io import BytesIO
import base64

p.OUT = p.ROOT / 'android-final'
p.OUT.mkdir(exist_ok=True)
def wait(): time.sleep(2)
def tap(x,y): p.adb('shell','input','tap',str(x),str(y)); wait()
def checkpoint(name):
    s=p.save(name); p.screenshot(name); return s
def ready():
    # monkey returns before Android switches away from the launcher; do not mistake it for the app.
    time.sleep(8)
    deadline=time.monotonic()+40
    while time.monotonic()<deadline:
        frame=p.adb('exec-out','screencap','-p')
        r,g,b,*_=Image.open(BytesIO(frame)).getpixel((100,100))
        if r>170 and g>160 and b>150:
            time.sleep(1); return
        time.sleep(.5)
    raise AssertionError('App did not leave splash for its authored screen')
def restart():
    p.adb('shell','am','force-stop',p.PACKAGE); p.launch(); ready()
def same(a,b,ignored=()):
    assert {k:v for k,v in a.items() if k not in ignored} == {k:v for k,v in b.items() if k not in ignored}
def fault(kind):
    p.adb('shell','am','force-stop',p.PACKAGE)
    s=p.SAVE
    commands={
      'corrupt': f'cp {s} {s}.backup && printf corrupt-primary > {s}',
      'unreadable': f'cp {s} {s}.backup && rm {s} && mkdir {s}',
      'both': f'rmdir {s} && printf corrupt-primary > {s} && printf corrupt-backup > {s}.backup'}
    p.adb('shell','sh','-c',"'"+commands[kind]+"'")
    p.launch(); ready()

if __name__=='__main__':
    print(p.adb('install','-r',str(p.OUT/'StartupLife-M7T02.apk')).decode())
    # Reset only this freshly created emulator's synthetic test app; baseline saves retained outside it.
    p.adb('shell','am','force-stop',p.PACKAGE)
    p.adb('shell','pm','clear',p.PACKAGE)
    p.adb('logcat','-c')
    p.launch(); ready(); p.screenshot('A-creation')
    recorder=threading.Thread(target=lambda:p.adb('shell','screenrecord','--bit-rate','2000000','--time-limit','180','/sdcard/m7-t02-final.mp4'))
    recorder.start(); time.sleep(1)
    p.adb('shell','input','tap','540','285'); time.sleep(.8)
    p.adb('shell','input','text','M7FinalEmulator'); p.adb('shell','input','keyevent','KEYCODE_BACK'); wait()
    tap(540,780); created=checkpoint('A-created'); assert created['Revision']==1
    tap(540,1115); employed=checkpoint('A-employed'); assert employed['Employment']['Xp']==0
    before=p.save('B-before-work'); p.adb('shell','input','tap','540','1225')
    deadline=time.monotonic()+15
    while time.monotonic()<deadline:
      raw=p.adb('exec-out','cat',p.SAVE)
      s=json.loads(base64.b64decode(json.loads(raw)['PayloadBase64']))
      if s.get('Employment',{}).get('Xp',0)>before['Employment']['Xp'] and s['PlaybackCursor']==0:
        p.adb('shell','input','keyevent','KEYCODE_HOME'); break
    else: raise AssertionError('Unacknowledged work cue was not captured')
    paused=checkpoint('B-background'); time.sleep(2); frozen=p.save('B-after-wait'); same(paused,frozen)
    restart(); acknowledged=checkpoint('B-restored-acknowledged')
    same(paused,acknowledged,('NextOperation','PlaybackCursor','Receipts','Revision'))
    assert paused['PlaybackCursor']==0 and acknowledged['PlaybackCursor']==1
    tap(540,1225); time.sleep(4); complete=checkpoint('B-shift-complete')
    assert complete['Minute']==1020 and complete['Employment']['Xp']==40 and len(complete['Claims'])==1
    restart(); same(complete,checkpoint('B-after-ack-restart'))
    tap(540,1335); tap(540,1440); course=checkpoint('C-before-restart')
    assert course['Course']['ProgressUnits']==600000
    p.adb('shell','input','keyevent','KEYCODE_HOME'); time.sleep(1)
    restart(); same(course,checkpoint('C-after-restore'))
    tap(540,1550); day=checkpoint('D-before-restart'); assert day['DateIso']=='2026-09-02' and day['Minute']==0
    restart(); same(day,checkpoint('D-after-restore'))
    p.adb('shell','pkill','-2','screenrecord',check=False); recorder.join(timeout=10)
    assert not recorder.is_alive()
    p.adb('pull','/sdcard/m7-t02-final.mp4',str(p.OUT/'lifecycle-emulator.mp4'))
    fault('corrupt'); p.screenshot('E-recovered-before-write')
    tap(540,1655); recovered=checkpoint('E-next-write')
    assert recovered['RunId']==day['RunId'] and recovered['Revision']==day['Revision']+1 and recovered['Employment'] is None
    same(day,recovered,('Employment','PreviousEmployment','Revision','NextOperation','Receipts','History','CurrentActivity','CurrentCue'))
    restart(); same(recovered,checkpoint('E-second-reload'))
    (p.OUT/'files-after-recovery.log').write_bytes(p.adb('shell','ls','-la',p.SAVE.rsplit('/',1)[0]))
    fault('unreadable'); p.screenshot('E-unreadable-fatal')
    fault('both'); p.screenshot('E-both-invalid-fatal')
    # Restore the retained valid synthetic save, leaving the dedicated emulator usable.
    p.adb('shell','am','force-stop',p.PACKAGE)
    p.adb('push',str(p.OUT/'E-second-reload.save.json'),p.SAVE)
    p.adb('shell','cp',p.SAVE,p.SAVE+'.backup')
    p.launch(); ready(); same(recovered,checkpoint('Z-restored-valid'))
    (p.OUT/'logcat.txt').write_bytes(p.adb('logcat','-d','-v','threadtime'))
    (p.OUT/'acceptance.json').write_text(json.dumps({'head':'57418f618cb2122ba8f71b6d82975184a14f7870','serial':p.SERIAL,'adbPort':5038,'A':'PASS','B':'PASS','C':'PASS','D':'PASS','ERecovery':'PASS','EFatalVisual':'requires screenshot review','wallClock':'PASS: full checkpoint unchanged after actual background wait','physicalAndroid':'NOT RUN','iOS':'NOT RUN','apkSha256':hashlib.sha256((p.OUT/'StartupLife-M7T02.apk').read_bytes()).hexdigest()},indent=2))
    print('Android emulator state assertions passed')
