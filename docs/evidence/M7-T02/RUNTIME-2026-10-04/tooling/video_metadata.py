from pathlib import Path
import struct, json, hashlib

def boxes(data,start=0,end=None):
    end=len(data) if end is None else end
    while start+8<=end:
        size,kind=struct.unpack_from('>I4s',data,start); header=8
        if size==1: size=struct.unpack_from('>Q',data,start+8)[0]; header=16
        if size==0: size=end-start
        if size<header or start+size>end: raise ValueError('Invalid MP4 box')
        yield kind,start+header,start+size
        start+=size

def inspect(path):
    data=path.read_bytes(); result={'file':path.name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    def walk(start,end):
        for kind,a,b in boxes(data,start,end):
            if kind in (b'moov',b'trak',b'mdia'): walk(a,b)
            elif kind==b'mvhd':
                version=data[a]; off=a+(20 if version==1 else 12)
                scale=struct.unpack_from('>I',data,off)[0]
                duration=struct.unpack_from('>Q' if version==1 else '>I',data,off+4)[0]
                result['durationSeconds']=duration/scale
            elif kind==b'tkhd':
                width,height=struct.unpack_from('>II',data,b-8)
                if width and height: result['width']=width/65536; result['height']=height/65536
    walk(0,len(data))
    assert result['durationSeconds']>30 and result['width']==1080 and result['height']==1920
    path.with_suffix('.metadata.json').write_text(json.dumps(result,indent=2))
    return result

if __name__=='__main__':
    import sys
    print(json.dumps(inspect(Path(sys.argv[1])),indent=2))
