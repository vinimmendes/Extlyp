"""Design drawings only: no Unity, gameplay, prefab generation or preference writes."""
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/UI/Previews/HudCompacta'
OUT.mkdir(parents=True, exist_ok=True)
PLACEMENTS = {e['Control']: e['Placement'] for e in json.loads((ROOT / 'Assets/Resources/UI/HudLayoutDefaults.json').read_text())['Controls']}
FONTS = ROOT / 'Assets/Resources/UI/Fonts'

def preview(width, height, theme, state='normal'):
    # Work at twice the virtual resolution, then downsample to the exact target size.
    W, H, K = width / height * 600, 600, 2
    im = Image.new('RGB', (round(W*K), H*K), '#30312c')
    d = ImageDraw.Draw(im)
    ink, rim, panel, accent = ('#e6d9ba', '#938665', '#20231c', '#ab784a') if theme == 'sombria' else ('#edf1e8', '#8b9c86', '#24312b', '#87b68c')
    def rect(box, fill=panel, outline=rim):
        x,y,w,h=box; d.rectangle((round(x*K),round(y*K),round((x+w)*K),round((y+h)*K)), fill=fill, outline=outline, width=K)
    def text(x,y,s,size=20,bold=False,anchor='lt',color=ink):
        font=ImageFont.truetype(str(FONTS / ('BarlowCondensed-SemiBold.ttf' if bold else 'BarlowCondensed-Regular.ttf')),round(size*K))
        d.text((round(x*K),round(y*K)),s,font=font,fill=color,anchor=anchor)
    def line(points,color=ink): d.line([(round(x*K),round(y*K)) for x,y in points], fill=color,width=2*K)
    def raw_bounds(c):
        p=PLACEMENTS[c]; w,h=p['Size']['x'],p['Size']['y']; x=W*p['Anchor']['x']+p['Offset']['x']; y=H*p['Anchor']['y']+p['Offset']['y']
        if c==17:
            x,y,w,h=bounds(0); return x,y+h+4,w,h
        if c in (0,17): w,h=max(200,w),max(42,h)
        if c in (4,5): w,h=max(96 if c==4 else 86,w),max(30,h)
        if c in (7,8,9,10):
            nw=max(76,w,h); x-=(nw-w)/2; y-=(nw-h)/2; w=h=nw
        if c in (3,6,14):
            nw,nh={3:(180,76),6:(144,68),14:(280,56)}[c]; nw,nh=max(w,nw),max(h,nh)
            x-=(nw-w)/2
            if c==6: y-=nh-h
            w,h=nw,nh
        return max(0,min(x,W-w)),max(0,min(y,H-h)),w,h
    def bounds(c):
        if c==5:
            mx,my,mw,mh=raw_bounds(15); side=max(76,44/(height/600))
            return mx-16-side,my,side,side
        if c not in (6,7,8,9,10,12): return raw_bounds(c)
        fire=raw_bounds(13); row=[(i,list(raw_bounds(i))) for i in (7,8,9,10)]
        total=sum(box[2] for _,box in row)+16*3; right=fire[0]-16; start=right-total
        for i,(_,box) in enumerate(row):
            box[0]=start; box[1]=fire[1]+fire[3]-box[3]; start+=box[2]+16
        if c in (7,8,9,10): return next(tuple(box) for i,box in row if i==c)
        x,y,w,h=raw_bounds(c)
        if c==6: return right-total/2-w/2,min(box[1] for _,box in row)-12-h,w,h
        return fire[0]+fire[2]/2-w/2,fire[1]-16-h,w,h
    # Neutral diagram background deliberately avoids pretending to be a gameplay capture.
    for x in range(0,round(W),64): line([(x,0),(x,H)],'#34362f')
    for y in range(0,H,64): line([(0,y),(W,y)],'#34362f')
    for c,name,v,ratio,col in [(0,'VIDA','100 / 100',1,'#9b5140'),(17,'STAMINA','70 / 100',.7,accent)]:
        x,y,w,h=bounds(c); rect((x,y,w,h)); text(x+10,y+2,name,22); text(x+w-10,y+2,v,22,True,'rt'); rect((x+12,y+32,w-24,7),'#4b4c40',None); rect((x+12,y+32,(w-24)*ratio,7),col,None)
    x,y,w,h=bounds(17)
    for dx,sw,label,col in [(0,130,'Alimentado','#a3bb8b'),(138,126,'Esporos 24%',ink)]:
        rect((x+dx,y+h+8,sw,28)); text(x+dx+25,y+h+10,label,20,color=col)
        line([(x+dx+7,y+h+27),(x+dx+14,y+h+15),(x+dx+21,y+h+27)],col)
    x,y,w,h=bounds(3); rect((x,y,w,h+(40 if state=='extracao' else 0))); text(x+w/2,y+3,'09:09',30,True,'mt'); text(x+w/2+10,y+40,'84 u',20,anchor='mt'); line([(x+45,y+59),(x+54,y+45),(x+63,y+59)])
    if state=='extracao':
        text(x+w/2,y+h+2,'Saindo: Escadaria',20,anchor='mt'); rect((x+10,y+h+34,w-20,5),'#526943',None)
    x,y,w,h=bounds(5)
    d.ellipse((round(x*K),round(y*K),round((x+w)*K),round((y+h)*K)),fill=panel,outline=rim,width=K)
    area=w*.64; ox=x+w*.18; oy=y+h*.18
    points=[]
    for i in range(33):
        radius=25 if i%4 in (1,2) else 19; angle=i*math.pi/16
        points.append((ox+(32+math.cos(angle)*radius)*area/64,oy+(32+math.sin(angle)*radius)*area/64))
    line(points)
    line([(ox+(32+math.cos(i*math.pi/12)*8)*area/64,oy+(32+math.sin(i*math.pi/12)*8)*area/64) for i in range(25)])
    x,y,w,h=bounds(15); rect((x,y,w,h)); text(x+w/2,y+2,'N',22,anchor='mt'); rect((x+4,y+32,w-8,h-66),'#363a30',None)
    for dx,dy in [(20,50),(50,68),(80,45),(104,90)]: rect((x+dx,y+dy,26,15),'#8c8669',None)
    text(x+w/2,y+h-29,'3 saídas · + Pronta',20,anchor='mt')
    x,y,w,h=bounds(6)
    if state!='desarmado':
        rect((x,y,w,h))
        main,secondary={'melee':('',''),'recarga':('0','999999')}.get(state,('12','24'))
        text(x+62+(w-70)/2,y+3,main,30,True,'mt'); text(x+62+(w-70)/2,y+36,secondary,20,anchor='mt')
        if state=='melee': line([(x+17,y+48),(x+43,y+22),(x+34,y+42)])
        else: line([(x+14,y+23),(x+46,y+23),(x+46,y+34),(x+30,y+34),(x+24,y+46)])
    for c in (7,8,9,10,11,12,13,16):
        x,y,w,h=bounds(c); cx,cy=x+w/2,y+h/2
        d.ellipse((round(x*K),round(y*K),round((x+w)*K),round((y+h)*K)),fill=panel,outline=accent if c==13 else rim,width=K)
        if c==11:
            d.ellipse((round((cx-22)*K),round((cy-22)*K),round((cx+22)*K),round((cy+22)*K)),fill=rim); continue
        def icon_path(points,tint=ink):
            s=w*(.44 if c==7 else .64); ox=x+(w-s)/2; oy=y+(w*.08 if c==7 else (h-s)/2)
            line([(ox+px*s/64,oy+py*s/64) for px,py in points],tint)
        if c==7:
            icon_path([(24,22),(24,14),(40,14),(40,22)])
            icon_path([(10,22),(54,22),(54,53),(10,53),(10,22)])
            icon_path([(25,37),(39,37)]); icon_path([(32,30),(32,44)])
            text(cx,y+h-3,'5 s' if state=='recarga' else '99+',20*w/50,True,'mb')
        elif c==8:
            icon_path([(10,21),(53,21),(43,11)]); icon_path([(53,21),(43,31)])
            icon_path([(54,43),(11,43),(21,33)]); icon_path([(11,43),(21,53)])
        elif c==9:
            for i in range(3):
                bx=12+i*16; tint='#8b897c' if state=='recarga' else ink
                icon_path([(bx,52),(bx,23),(bx+4,11),(bx+8,23),(bx+8,52),(bx,52)],tint)
                icon_path([(bx,42),(bx+8,42)],tint)
        elif c==10:
            icon_path([(24,16),(24,9),(40,9),(40,16)])
            icon_path([(17,17),(47,17),(52,25),(52,54),(12,54),(12,25),(17,17)])
            icon_path([(20,38),(44,38),(44,49),(20,49),(20,38)])
            icon_path([(20,29),(44,29)]); icon_path([(38,29),(38,33)])
        elif c==13:
            line([(cx-12,cy+1),(cx+16,cy-28),(cx+23,cy-21),(cx-5,cy+8)]);text(cx,y+h*.68,'ATACAR' if state=='melee' else 'ATIRAR',20,anchor='mt')
        elif c==12:
            line([(cx-13,cy),(cx+13,cy)]);line([(cx,cy-13),(cx,cy+13)])
        else:
            line([(cx+3,cy-15),(cx-6,cy-4),(cx+6,cy+3),(cx+12,cy+16)]);line([(cx-6,cy-4),(cx-13,cy+15)])
    suffix='' if state=='normal' else f'-{state}'
    im.resize((width,height),Image.Resampling.LANCZOS).save(OUT / f'{theme}-{width}x{height}{suffix}.png')

for size in [(640,360),(1280,720),(800,360)]:
    for theme in ['sombria','simples']: preview(*size,theme)
for state in ['melee','desarmado','recarga','extracao']: preview(640,360,'sombria',state)
html='''<!doctype html><meta charset="utf-8"><title>Composição da HUD compacta</title><style>body{background:#222;color:#eee;font:16px system-ui;margin:32px}img{display:block;max-width:none;margin:16px 0 32px}a{color:#c9c3ad}</style><h1>Prévia estática da composição</h1><p>Desenho em escala real, com Barlow e posições padrão do projeto. Ícones simplificados; não é captura nem validação da Unity. Preferências pessoais não são lidas ou alteradas.</p>'''
for size in [(640,360),(1280,720),(800,360)]:
    for theme in ['sombria','simples']:
        name=f'{theme}-{size[0]}x{size[1]}'; html+=f'<h2>{name}</h2><img src="{name}.png" width="{size[0]}" height="{size[1]}">'
for state in ['melee','desarmado','recarga','extracao']:
    html+=f'<h2>Estudo de estado: {state}</h2><img src="sombria-640x360-{state}.png" width="640" height="360">'
(OUT / 'index.html').write_text(html,encoding='utf-8')
print(OUT)
