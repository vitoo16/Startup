#!/usr/bin/env python3
"""Hand-assembled C03 native 64x80 RGBA pixel draft. First-party geometry only.

No filtered photos, online sprites, gradients, AI-generated pseudo-pixel inputs,
runtime asset approval or Unity import. Three distinct everyday adult silhouettes,
four separately constructed views, independent semantic 21-part rig layers.
"""
import argparse
import json
from pathlib import Path
from PixelPng import png_rgba

W,H=64,80
ORDER=['hair_back','accessory_back','foot_l','foot_r','lower_leg_l','lower_leg_r','upper_leg_l','upper_leg_r','pelvis','torso','upper_arm_l','upper_arm_r','forearm_l','forearm_r','hand_l','hand_r','outfit_torso','head','face','hair_front','accessory_front']
STYLES=('sang_ngo_nho','tan_ca','quan_dau_pho')
VIEWS=('front','three_quarter_left','side','back')
EXPRESSIONS=('neutral','soft_smile','focused','delighted')
PALETTES=[
 dict(skin='#DEAD89',light='#F0C5A1',skin_shadow='#C99070',hair='#302C32',hair_light='#51434A',shirt='#86A69A',shirt_dark='#618476',pants='#5C7484',pants_dark='#465968',bag='#B47F55'),
 dict(skin='#B77C5F',light='#D49C78',skin_shadow='#985D4C',hair='#302D2B',hair_light='#52453E',shirt='#D6B48A',shirt_dark='#AE8A62',pants='#556A78',pants_dark='#394F5C',bag='#7F995A'),
 dict(skin='#E6B698',light='#F5CBB0',skin_shadow='#C98F77',hair='#392F35',hair_light='#665054',shirt='#90A4B2',shirt_dark='#6B8192',pants='#6C626D',pants_dark='#504D5A',bag='#C3936B'),
]
BASE={'outline':'#3A393B','shoe':'#46464B','shoe_tip':'#DDD6C5','blush':'#CA8E81','eye':'#302C32','eye_soft':'#544744','cream':'#F7F2E6','seam':'#D4C9B8'}

def rgb(color):
    if len(color)!=7 or color[0]!='#':raise ValueError('Invalid authored RGB')
    return bytes.fromhex(color[1:])+b'\xff'

class Sprite:
    def __init__(self,palette):
        self.colors={**BASE,**palette}
        self.parts={name:bytearray(W*H*4) for name in ORDER}
    def dot(self,p,x,y,tone):
        if p not in self.parts or tone not in self.colors:
            raise ValueError('Unknown part or palette key')
        if not(0<=x<W and 0<=y<H):raise ValueError('Pixel out of source grid')
        a=self.parts[p];i=((H-1-y)*W+x)*4;a[i:i+4]=rgb(self.colors[tone])
    def rect(self,p,x,y,w,h,tone):
        if w<=0 or h<=0 or x<0 or y<0 or x+w>W or y+h>H:
            raise ValueError('Out-of-bounds native pixel rect')
        for yy in range(y,y+h):
            for xx in range(x,x+w):self.dot(p,xx,yy,tone)
    def oval(self,p,cx,cy,rx,ry,tone):
        if min(rx,ry)<=0:raise ValueError('Invalid native ellipse')
        for yy in range(cy-ry,cy+ry+1):
            for xx in range(cx-rx,cx+rx+1):
                if ((xx-cx)/rx)**2+((yy-cy)/ry)**2<=1.02:
                    self.dot(p,xx,yy,tone)
    def merged(self):
        out=bytearray(W*H*4)
        for part in ORDER:
            a=self.parts[part]
            for index in range(0,len(out),4):
                if a[index+3]:out[index:index+4]=a[index:index+4]
        return out

def draw(style,view,tone=0,expression='soft_smile'):
    if style not in STYLES or view not in VIEWS or tone not in range(3) or expression not in EXPRESSIONS:
        raise ValueError('Unregistered C03 sprite choice')
    s=Sprite(PALETTES[tone])
    front=view=='front';three=view=='three_quarter_left';side=view=='side';back=view=='back'
    cx=31 if side or three else 32
    shoulders={'sang_ngo_nho':10,'tan_ca':10,'quan_dau_pho':13}[style]
    if style=='tan_ca':
        s.colors.update(shirt='#C37E65',shirt_dark='#9C5C50',pants='#555B65',pants_dark='#414750',bag='#7C9C7C',seam='#E9C4AA')
    elif style=='quan_dau_pho':
        s.colors.update(shirt='#D8B26A',shirt_dark='#A88145',pants='#4B6A67',pants_dark='#344D49',bag='#7190A0',seam='#EAD6A8')
    else:
        s.colors.update(shirt='#8CB0A6',shirt_dark='#648E84',pants='#6A7588',pants_dark='#4B5A6B',bag='#B57F60',seam='#E4D1B2')
    # Distinctly articulated thighs, knees, shins, shoes, elbows and hands.
    for leg_id,off in [('l',-6),('r',6)]:
        leg=cx+off if not side else cx+(-3 if leg_id=='l' else 3)
        lw=5 if style=='sang_ngo_nho' else 6 if style=='tan_ca' else 4
        s.rect('upper_leg_'+leg_id,leg-lw,22,lw*2,12,'pants')
        s.rect('upper_leg_'+leg_id,leg-3,23,2,8,'pants_dark')
        s.rect('lower_leg_'+leg_id,leg-lw,11,lw*2,13,'pants')
        s.rect('lower_leg_'+leg_id,leg+lw-2,11,2,10,'pants_dark')
        s.oval('foot_'+leg_id,leg,10,5,3,'outline')
        s.rect('foot_'+leg_id,leg-4,9,8,2,'shoe')
        s.rect('foot_'+leg_id,leg-4,8,6,1,'shoe_tip')
    s.oval('pelvis',cx,31,13 if style=='tan_ca' else 11,7,'pants')
    s.oval('torso',cx,41,shoulders+2,12,'shirt_dark')
    s.oval('outfit_torso',cx,41,shoulders,11 if style!='quan_dau_pho' else 12,'shirt')
    s.rect('outfit_torso',cx-3,45,6,3,'light')
    if style=='sang_ngo_nho':
        # Light commuting shirt with open collar and cross-body sling.
        s.rect('outfit_torso',cx-2,42,5,7,'cream')
        s.rect('outfit_torso',cx-2,44,2,4,'shirt_dark')
        s.rect('outfit_torso',cx+2,44,2,4,'shirt_dark')
        s.rect('outfit_torso',cx,35,1,11,'shirt_dark')
        s.rect('outfit_torso',cx+1,40,1,1,'seam')
    elif style=='tan_ca':
        # Practical soft cardigan, blouse and broader trousers.
        s.rect('outfit_torso',cx-3,40,6,10,'cream')
        s.rect('outfit_torso',cx-4,35,2,14,'shirt_dark')
        s.rect('outfit_torso',cx+3,35,2,14,'shirt_dark')
        s.rect('outfit_torso',cx-1,36,2,9,'seam')
    else:
        # Boxy overshirt and understated backpack, no symbols or trademarks.
        s.rect('outfit_torso',cx+1,33,2,15,'shirt_dark')
        s.rect('outfit_torso',cx-8,39,5,6,'shirt_dark')
        s.rect('outfit_torso',cx-7,41,3,3,'shirt')
        s.rect('outfit_torso',cx-5,45,3,2,'seam')
        s.rect('outfit_torso',cx+6,45,3,2,'seam')
    for arm_id,sgn in [('l',-1),('r',1)]:
        armx=cx+sgn*(shoulders+1)
        if side:armx=cx+sgn*8
        s.oval('upper_arm_'+arm_id,armx,40,4,7,'shirt')
        s.rect('upper_arm_'+arm_id,armx-3,35,6,3,'shirt_dark')
        s.oval('forearm_'+arm_id,armx,31,3,6,'skin')
        s.rect('forearm_'+arm_id,armx-1,33,2,5,'light')
        s.oval('hand_'+arm_id,armx,26,3,4,'skin')
    hc=cx-2 if side else cx-1 if three else cx
    rx=12 if side else 14
    s.oval('head',hc,60,rx,14,'skin')
    s.oval('head',hc-3,65,rx-5,6,'light')
    s.oval('head',hc,50,rx-6,3,'skin')
    s.rect('head',hc-3,47,6,5,'skin')
    if not back:
        s.oval('head',hc-rx+1 if side or three else hc+rx-1,58,3,4,'skin')
    if side:s.rect('head',hc-14,57,4,4,'skin')
    elif three:s.rect('head',hc-13,57,3,3,'skin')
    s.oval('hair_back',hc,69,rx+2,9,'hair')
    s.rect('hair_back',hc-rx-1,64,4,7,'hair')
    s.rect('hair_back',hc+rx-3,64,4,6,'hair')
    if style=='sang_ngo_nho':
        s.rect('hair_front',hc-rx+1,67,rx*2-3,5,'hair')
        s.rect('hair_front',hc-rx+3,62,6,8,'hair')
        s.rect('hair_front',hc-4,65,5,5,'hair_light')
        s.rect('hair_front',hc+7,66,6,4,'hair')
        s.rect('hair_front',hc+10,69,4,3,'hair')
    elif style=='tan_ca':
        s.oval('hair_back',hc+rx-1,59,7,14,'hair')
        s.oval('hair_back',hc+rx+3,54,6,8,'hair')
        s.rect('hair_back',hc+rx-3,46,8,16,'hair')
        s.rect('hair_back',hc-rx-2,54,5,15,'hair')
        s.oval('hair_back',hc+10,74,5,4,'hair')
        s.rect('hair_front',hc-rx+2,68,rx*2-3,5,'hair')
        s.rect('hair_front',hc-rx+3,61,5,11,'hair')
        s.rect('hair_front',hc+rx-6,61,4,9,'hair')
        s.rect('hair_front',hc-10,67,5,4,'hair_light')
    else:
        for bx,by in [(-13,69),(-9,74),(-4,75),(3,75),(9,74),(14,69)]:
            s.oval('hair_back',hc+bx,by,5,4,'hair')
        s.rect('hair_front',hc-rx+2,66,rx*2-3,5,'hair')
        s.oval('hair_front',hc-10,64,5,5,'hair')
        s.oval('hair_front',hc+6,65,5,4,'hair')
        s.rect('hair_front',hc+4,65,6,4,'hair_light')
        s.rect('hair_front',hc-9,63,5,5,'hair')
    if back:
        s.rect('hair_back',hc-10,57,20,10,'hair')
        if style=='tan_ca':s.oval('hair_back',hc+5,51,9,10,'hair')
    else:
        eye_y=60 if expression=='focused' else 59
        eyes=[hc-6,hc+5] if front else ([hc-7,hc+4] if three else [hc-8])
        for ex in eyes:
            s.rect('face',ex,eye_y,2,2 if expression=='delighted' else 3,'eye')
            if expression=='focused':s.rect('face',ex-1,eye_y+5,4,1,'eye_soft')
        if expression=='delighted':s.rect('face',hc-2,53,5,2,'skin_shadow')
        elif expression=='neutral':s.rect('face',hc-2,53,3,1,'skin_shadow')
        elif expression=='focused':s.rect('face',hc-1,53,2,1,'skin_shadow')
        else:
            s.rect('face',hc-2,54,4,1,'skin_shadow')
            s.dot('face',hc-1,53,'skin_shadow')
        if not side:
            s.rect('face',hc-11,55,2,1,'blush')
            if front:s.rect('face',hc+9,55,2,1,'blush')
    if style=='sang_ngo_nho':
        bagx=cx+11 if not back else cx-15
        s.rect('accessory_back',bagx,29,8,11,'bag')
        s.rect('accessory_back',bagx+1,30,6,3,'shirt_dark')
        if not back:
            for i in range(10):s.dot('accessory_front',cx+7-i//2,45-i,'bag')
    elif style=='tan_ca':
        bagx=cx-16 if side else cx-19
        s.rect('accessory_back',bagx,18,11,16,'bag')
        s.rect('accessory_back',bagx+2,32,7,5,'bag')
        s.rect('accessory_back',bagx+3,33,3,4,'cream')
    else:
        bagx=cx+12
        s.rect('accessory_back',bagx,31,8,16,'bag')
        s.rect('accessory_back',bagx+2,33,4,7,'shirt_dark')
    return s

def rgba_png(pixels):
    return png_rgba(W,H,(pixels[y*W*4:(y+1)*W*4] for y in range(H)))

def sheet(cards,columns,scale=4):
    if scale not in (1,2,4,6):raise ValueError('Integer NN scale only')
    pad=12
    width=pad+columns*(W*scale+pad)
    height=pad+((len(cards)+columns-1)//columns)*(H*scale+pad)
    pixels=bytearray(rgb('#F3EFE7')*(width*height))
    for i,art in enumerate(cards):
        px=pad+(i%columns)*(W*scale+pad)
        py=pad+(i//columns)*(H*scale+pad)
        frame=art.merged()
        for y in range(H):
            for x in range(W):
                j=(y*W+x)*4
                if not frame[j+3]:continue
                for dy in range(scale):
                    for dx in range(scale):
                        dst=((py+y*scale+dy)*width+px+x*scale+dx)*4
                        pixels[dst:dst+4]=frame[j:j+4]
    return png_rgba(width,height,(pixels[y*width*4:(y+1)*width*4] for y in range(height)))

def build(output,fronts=None,report=None,export_layers=None):
    # Explicitly do not mutate production approved registry.
    main=[draw(style,view) for style in STYLES for view in VIEWS]
    output=Path(output);output.parent.mkdir(parents=True,exist_ok=True)
    output.write_bytes(sheet(main,4))
    if fronts:
        thumbs=[draw(style,'front',tone,('neutral','soft_smile','delighted')[tone])
                for style in STYLES for tone in range(3)]
        fronts=Path(fronts);fronts.parent.mkdir(parents=True,exist_ok=True)
        fronts.write_bytes(sheet(thumbs,3))
    if export_layers:
        folder=Path(export_layers)
        for style in STYLES:
            for view in VIEWS:
                art=draw(style,view)
                for name in ORDER:
                    layer=folder/style/view/(name+'.png')
                    layer.parent.mkdir(parents=True,exist_ok=True)
                    layer.write_bytes(rgba_png(art.parts[name]))
    result={'status':'DRAFT_NOT_APPROVED','styles':STYLES,'views':VIEWS,
            'native_grid':[W,H],'rig_parts':ORDER,'turnaround_frames':12,
            'front_variants':9,'generated_png_source':'first_party_code_no_external_art'}
    if report:
        report=Path(report);report.parent.mkdir(parents=True,exist_ok=True)
        report.write_text(json.dumps(result,indent=2)+'\n',encoding='utf8')
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',required=True)
    parser.add_argument('--fronts')
    parser.add_argument('--report')
    parser.add_argument('--export-layers')
    args=parser.parse_args()
    build(args.output,args.fronts,args.report,args.export_layers)
    print('PASS first-party native C03 source DRAFT; art/IP/Unity approval NOT RUN')
