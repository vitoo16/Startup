from pathlib import Path
import sys, subprocess, json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'video-tools'))
import imageio_ffmpeg
from video_metadata import inspect

path=ROOT/'android-final/lifecycle-emulator.mp4'
meta=inspect(path)
decoder=imageio_ffmpeg.get_ffmpeg_exe()
if (path.parent/'video-decode.log').exists():
    (path.parent/'video-decode-vfr-muxer.log').write_bytes((path.parent/'video-decode.log').read_bytes())
# Raw output validates decoding without quantizing screenrecord's variable timestamps to a null muxer time base.
result=subprocess.run([decoder,'-v','error','-i',str(path),'-vf','setpts=N/(60*TB)','-enc_time_base','1/60','-fps_mode','passthrough','-f','rawvideo','-pix_fmt','yuv420p','-'],stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
(path.parent/'video-decode.log').write_bytes(result.stderr)
assert result.returncode==0 and not result.stderr
for label,seconds in [('begin',3),('middle',meta['durationSeconds']/2),('end',meta['durationSeconds']-0.2)]:
    subprocess.run([decoder,'-v','error','-ss',str(seconds),'-i',str(path),'-frames:v','1','-y',str(path.parent/f'video-frame-{label}.png')],check=True)
meta['fullDecodeExit']=result.returncode
meta['decodeMethod']='Every input frame decoded; output timestamps normalized only in discarded raw output to avoid VFR/null-muxer quantization. Original MP4 unchanged.'
meta['decoderPackage']='imageio-ffmpeg 0.6.0, installed only in task scratch'
path.with_suffix('.metadata.json').write_text(json.dumps(meta,indent=2))
print(json.dumps(meta,indent=2))
