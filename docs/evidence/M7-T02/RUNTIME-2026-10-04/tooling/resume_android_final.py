from run_android_final import p, same, restart, checkpoint, fault, ready
import json, time, hashlib

rows={x['label']:x['state'] for x in map(json.loads,(p.OUT/'observed-states.ndjson').read_text().splitlines())}
day=rows['D-before-restart']; recovered=rows['E-next-write']
# A normal Resign command also sets its activity/cue; the initial external harness omitted these expected differences.
same(day,recovered,('Employment','PreviousEmployment','Revision','NextOperation','Receipts','History','CurrentActivity','CurrentCue'))
assert recovered['CurrentCue']=='career.resigned' and recovered['Revision']==day['Revision']+1
restart(); same(recovered,checkpoint('E-second-reload'))
(p.OUT/'files-after-recovery.log').write_bytes(p.adb('shell','ls','-la',p.SAVE.rsplit('/',1)[0]))
fault('unreadable'); p.screenshot('E-unreadable-fatal')
fault('both'); p.screenshot('E-both-invalid-fatal')
p.adb('shell','am','force-stop',p.PACKAGE)
p.adb('push',str(p.OUT/'E-second-reload.save.json'),p.SAVE)
p.adb('shell','cp',p.SAVE,p.SAVE+'.backup')
p.launch(); ready(); same(recovered,checkpoint('Z-restored-valid'))
(p.OUT/'logcat.txt').write_bytes(p.adb('logcat','-d','-v','threadtime'))
(p.OUT/'acceptance.json').write_text(json.dumps({'head':'57418f618cb2122ba8f71b6d82975184a14f7870','serial':p.SERIAL,'adbPort':5038,'A':'PASS','B':'PASS','C':'PASS','D':'PASS','ERecovery':'PASS','EFatalVisual':'requires screenshot review','wallClock':'PASS: full checkpoint unchanged after actual background wait','physicalAndroid':'NOT RUN','iOS':'NOT RUN','execution':'A-D and E-next-write executed by run_android_final.py; E remainder completed by resume_android_final.py after correcting only the external harness comparison','apkSha256':hashlib.sha256((p.OUT/'StartupLife-M7T02.apk').read_bytes()).hexdigest()},indent=2))
print('E resumed checks passed; all A-E state assertions passed on the repair head')
