"""Authoring preview of the HUD composition. Not a Unity capture or gameplay test.

The icon geometry is read from HudArt.cs; fonts are the bundled runtime fonts.
Example gameplay values and map marks are illustrative.
"""
from pathlib import Path
import math
import re
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).parent
S = 2
W, H = 1066, 600
TEXT = (230, 217, 186, 255)
MUTED = (176, 168, 138, 220)
COPPER = (183, 125, 78, 245)
GOOD = (122, 155, 92, 255)
source = (ROOT / "Assets/Scripts/UI/HudArt.cs").read_text(encoding="utf-8")
canvas = Image.new("RGBA", (W*S, (H+38)*S))
pixels = canvas.load()
for y in range(H*S):
    for x in range(W*S):
        r = math.hypot((x/S-W/2)/W, (y/S-H/2)/H)
        shade = int(18 + max(0, 1-r*2)*12)
        pixels[x, y] = (shade, shade+2, shade-1, 255)
draw = ImageDraw.Draw(canvas)

def font(size, bold=False):
    name = "BarlowCondensed-SemiBold.ttf" if bold else "BarlowCondensed-Regular.ttf"
    return ImageFont.truetype(str(ROOT/"Assets/Resources/UI/Fonts"/name), round(size*S))

def rect(r, fill, outline=None, width=1):
    x,y,w,h = r
    draw.rectangle((x*S,y*S,(x+w)*S,(y+h)*S),fill=fill,outline=outline,width=max(1,round(width*S)))

def panel(r):
    x,y,w,h=r
    width,height=round(w*S),round(h*S)
    layer=Image.new("RGBA",(width,height)); p=layer.load()
    for py in range(height):
        for px in range(width):
            qx=abs(px+.5-width/2)-(width/2-1.5-5)
            qy=abs(py+.5-height/2)-(height/2-1.5-5)
            outside=math.hypot(max(qx,0),max(qy,0))+min(max(qx,qy),0)-5
            inside=-outside; coverage=max(0,min(1,.5-outside))
            grain=math.sin(px*1.37+py*.79)*math.sin(px*.29-py*1.71)
            patina=(math.sin(px*.09+py*.14)+math.sin(px*.031-py*.21)+2)/4
            rim=max(0,min(1,1.25-abs(inside-1-grain*.11)))
            chip=max(0,(math.sin(px*.41+py*.63)-.75)*2)
            fill=(18+grain,22+grain,16+grain*.8,189)
            edge=(133*(.76+patina*.28),116*(.76+patina*.28),85*(.76+patina*.28),(.58+patina*.16)*(1-chip*.55)*255)
            c=[fill[i]*(1-rim)+edge[i]*rim for i in range(4)]
            inset=max(0,.68-abs(inside-3.6))*(.08+patina*.1)
            c[:3]=[c[i]*(1-inset)+MUTED[i]*inset for i in range(3)]
            c[3]*=coverage
            p[px,py]=tuple(round(max(0,min(255,v))) for v in c)
    canvas.alpha_composite(layer,(round(x*S),round(y*S)))

def text(x,y,value,size=13,color=TEXT,center=False,bold=False):
    for i,line in enumerate(value.split("\n")):
        draw.text((x*S,(y+i*(size+2))*S),line,font=font(size,bold),fill=color,anchor="mt" if center else "lt")

def circle(r, prominent=False):
    x,y,w,h=r
    layer=Image.new("RGBA",(512,512)); p=layer.load()
    for py in range(512):
        for px in range(512):
            dx,dy=px-255.5,py-255.5
            distance=math.hypot(dx,dy); angle=math.atan2(dy,dx)
            grain=math.sin(px*.33+py*.72)*math.sin(px*.83-py*.29)*.5
            patina=(math.sin(px*.08+py*.07)+math.sin(px*.03-py*.095)+2)/4
            outer=250+grain*.65; rim=6 if prominent else 5
            abrasion=max(0,min(1,(patina-.58)*2.3))
            edge=list(COPPER if prominent else MUTED); edge[3]=int((.95 if prominent else .72)*(1-abrasion*.48)*255)
            fill=(19+grain*2,23+grain*2,17+grain*1.5,161)
            bevel=max(0,min(1,rim*.45+.5-abs(distance-(outer-rim*.58))))
            c=[fill[i]*(1-bevel)+edge[i]*bevel for i in range(4)]
            if prominent and -.08<angle<2.88 and abs(angle-1.65)>.055:
                companion=max(0,min(1,1.7-abs(distance-(outer-11))))*(.54+patina*.22)
                c=[c[i]*(1-companion)+edge[i]*companion for i in range(4)]
            c[3]*=max(0,min(1,outer+.5-distance))
            p[px,py]=tuple(round(max(0,min(255,v))) for v in c)
    layer=layer.resize((round(w*S),round(h*S)),Image.Resampling.LANCZOS)
    canvas.alpha_composite(layer,(round(x*S),round(y*S)))

def icon(symbol, r, color=TEXT, angle=0):
    match = re.search(r"case HudSymbol\."+symbol+r":(.*?)(?:break;)", source, re.S)
    body = match.group(1)
    # Expand the three small authoring loops in the C# silhouette definitions.
    for pattern,values in [(r"for\(int x=30;x<=90;x\+=30\)\s*\{(.*?)\}",[30,60,90]),
                           (r"for\(int x=42;x<=86;x\+=22\)\s*(p\.Path\(.*?\);)",[42,64,86]),
                           (r"for\(int y=40;y<=88;y\+=24\)\s*(p\.Path\(.*?\);)",[40,64,88])]:
        m = re.search(pattern, body, re.S)
        if m:
            variable="y" if "int y=" in m.group(0) else "x"
            body=body[:m.start()]+" ".join(re.sub(r"\b"+variable+r"\b",str(v),m.group(1)) for v in values)+body[m.end():]
    mask=Image.new("L",(512,512)); d=ImageDraw.Draw(mask)
    for method,args in re.findall(r"p\.(Line|Etch|Path|Box|Ring|Solid|Cut)\(([^;]+?)\);",body):
        n=[float(eval(v,{"__builtins__":{}},{})) for v in args.split(",")]
        if method in ("Line","Etch"):
            x,y,u,v=n[:4]; width=n[4] if len(n)>4 else 3
            if method=="Line":
                d.line((x*4,y*4,u*4,v*4),fill=255,width=round(width*4))
            else:
                scratch=Image.new("L",mask.size); sd=ImageDraw.Draw(scratch)
                sd.line((x*4,y*4,u*4,v*4),fill=199,width=round(width*4))
                from PIL import ImageChops
                mask=ImageChops.multiply(mask,ImageChops.invert(scratch)); d=ImageDraw.Draw(mask)
        elif method=="Path":
            width=n[0]; points=[(n[i]*4,n[i+1]*4) for i in range(1,len(n),2)]
            d.line(points,fill=255,width=round(width*4),joint="curve")
        elif method in ("Box","Cut"):
            x,y,w,h=n[:4]; width=n[4] if len(n)>4 else 3
            d.rectangle((x*4,y*4,(x+w)*4,(y+h)*4),fill=0 if method=="Cut" else None,outline=None if method=="Cut" else 255,width=round(width*4))
        elif method=="Ring":
            x,y,radius=n[:3]; width=n[3] if len(n)>3 else 3
            d.ellipse(((x-radius)*4,(y-radius)*4,(x+radius)*4,(y+radius)*4),outline=255,width=round(width*4))
        elif method=="Solid":
            d.polygon([(n[i]*4,n[i+1]*4) for i in range(0,len(n),2)],fill=255)
    if angle: mask=mask.rotate(-angle,resample=Image.Resampling.BICUBIC)
    x,y,w,h=r
    mask=mask.resize((round(w*S),round(h*S)),Image.Resampling.LANCZOS)
    tint=Image.new("RGBA",mask.size,color)
    tint.putalpha(mask)
    canvas.alpha_composite(tint,(round(x*S),round(y*S)))

panel((25,35,230,42)); text(37,40,"VIDA   86 / 120",14)
rect((37,59,206,10),(92,83,66,255)); rect((38,60,204,8),(32,34,27,255)); rect((38,60,204*86/120,8),GOOD)
for symbol,x,w,label,color in [("Food",25,112,"Bem alimentado",GOOD),("Spores",146,109,"Esporos  24%",(184,161,196,255))]:
    panel((x,83,w,25)); icon(symbol,(x+7,86,18,18),color); text(x+31,87,label,12,color)
panel((467,35,132,58)); text(533,40,"03:42",26,center=True,bold=True)
text(476,70,"Extração",12); icon("Arrow",(531,70,15,15),angle=45); text(549,70,"280 u",12)
for x,w,symbol,label in [(847,96,"Map","Mapa"),(954,86,"Menu","Menu")]:
    panel((x,35,w,26)); icon(symbol,(x+9,37,22,22)); text(x+37,40,label,14)
panel((862,70,178,160))
for points in [[(891,193),(910,171),(933,159),(962,123),(992,92)],[(876,133),(902,142),(933,159),(981,186),(1024,209)]]:
    draw.line([(x*S,y*S) for x,y in points],fill=(64,62,53,255),width=8*S,joint="curve")
icon("Arrow",(947,144,14,18)); icon("Arrow",(947,75,8,8)); text(951,84,"N",12,center=True)
for x,y,color in [(987,95,GOOD),(1019,157,TEXT),(873,210,TEXT)]: icon("Exit",(x,y,16,20),color)
icon("Wind",(1000,186,28,20),MUTED); text(1014,204,"Vento\nL",10,MUTED,center=True)
circle((28,368,160,160)); circle((74.4,414.4,67.2,67.2))
draw.ellipse((85.6*S,425.6*S,130.4*S,470.4*S),fill=(99,96,80,255),outline=MUTED,width=2)
for i in range(4):
    a=i*math.pi/2; x=108+math.sin(a)*69.6; y=448-math.cos(a)*69.6
    icon("Arrow",(x-4,y-4,8,8),MUTED,i*90)
text(108,534,"Mover",14,center=True)
panel((799,352,144,65)); icon("Pistol",(806,369,34,34),angle=-20)
text(846,356,"PISTOLA",12); text(846,371,"12 / 12",22,bold=True); text(846,398,"Reserva 12",12)
circle((959,336,72,72)); icon("Aim",(973.4,350.4,43.2,43.2))
for x,symbol,label in [(670,"Heal","Curar\n×3"),(733,"Swap","Trocar"),(796,"Ammo","Recarregar"),(859,"Backpack","Mochila")]:
    circle((x,462,50,50)); icon(symbol,(x+11.25,473.25,27.5,27.5)); text(x+25,515,label,13,center=True)
circle((930,424,110,110),True); icon("Bullet",(960.8,446,48.4,48.4)); text(985,497.7,"ATIRAR",16,center=True)
panel((463,438,140,38)); icon("Chest",(473,447,21,21)); text(500,447,"Abrir baú · 2 s",13)
rect((471,469,124,3),(80,76,62,255))
rect((0,H,W,38),(10,12,10,255)); text(20,H+10,"PRÉVIA DE COMPOSIÇÃO · Valores ilustrativos · Não é captura da Unity",13,MUTED)
OUT.mkdir(parents=True,exist_ok=True)
canvas.convert("RGB").save(OUT/"hud-sombria-composicao-v2.png")
print(OUT/"hud-sombria-composicao-v2.png")

# Enlarged artwork review; geometry is still extracted from the runtime source.
canvas=Image.new("RGBA",(1066*S,290*S),(18,21,17,255)); draw=ImageDraw.Draw(canvas)
text(30,16,"SOMBRIA · DETALHES DE ARTE E MOLDURAS",21,bold=True)
labels=[("Heal","Cura"),("Backpack","Mochila"),("Ammo","Cartuchos"),("Chest","Baú"),("Pistol","Pistola"),("Bullet","Disparo")]
for i,(symbol,label) in enumerate(labels):
    x=32+i*170
    circle((x,61,116,116),symbol=="Bullet")
    icon(symbol,(x+22,83,72,72))
    text(x+58,188,label,16,center=True)
panel((30,226,1006,43)); text(47,236,"Desgaste localizado · Cobre fosco · Gravações discretas · Contornos suaves · Prévia de autoria, não captura Unity",14)
canvas.convert("RGB").save(OUT/"hud-sombria-detalhes-v2.png")
print(OUT/"hud-sombria-detalhes-v2.png")
