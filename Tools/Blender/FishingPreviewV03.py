"""Horizontal fish-in-zone revision of the existing visual storyboard."""
from pathlib import Path

# Reuse the authored composition without editing or overwriting the previous version.
base=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Blender/FishingPreviewV02.py')
source=base.read_text(encoding='utf-8').replace('v02','v03').replace('V02','V03')
source=source.replace("('RECOLHA','Aproveite a pausa do peixe.'),('CEDA LINHA!','Arrancada! Alivie a tensão.')", "('NA FAIXA','Continue acompanhando o peixe.'),('FORA DA FAIXA','Aproxime a faixa do peixe.')")
source=source.replace("'TENSÃO',X,.61", "'FAIXA DE CAPTURA',X,.82")
source=source.replace("'LIMITE',X+W,.65", "'',X+W,.65")
source=source.replace("'Segure para recolher · solte para aliviar'", "'Segure: direita · solte: esquerda'")
source=source.replace("['LANÇAR ANZOL','AGUARDE','FISGAR','RECOLHER','LANÇAR NOVAMENTE']", "['LANÇAR ANZOL','AGUARDE','FISGAR','SEGURAR','LANÇAR NOVAMENTE']")
source=source.replace("rect('TensionWell',X,.10,W,.28,.2,'Inset')", "rect('TensionWell',X,.01,W,.53,.2,'Inset')")
source=source.replace("outline('TensionBorder',X-.035,.065,W+.07,.35,.22,'Edge',.008)", "outline('TensionBorder',X-.035,-.025,W+.07,.60,.22,'Edge',.008)")
source=source.replace("rect('DangerArea',X+W*.8,.11,W*.2-.02,.26,.25,'Danger')", "")
source=source.replace("rect('Tension',0,0,1,.24,.28,'Green'); tension.location=(X+.02,.12,0)", "rect('Tension',0,0,1,.45,.28,'DarkGreen'); tension.location=(X+.02,.05,0)")

# Introduce independent movable window and fish icon before animating the scene.
source=source.replace("timeline=[]", """
zoneborder=outline('CaptureWindow',0,0,1,.45,.31,'EdgeLight',.016)
zoneborder.location=(X,.05,0)
fishmarker=mesh('HorizontalFish',[(-.13,0,.40),(-.23,.10,.40),(-.21,0,.40),(-.23,-.10,.40),(-.13,0,.40),(-.06,-.075,.40),(.095,-.065,.40),(.17,0,.40),(.095,.065,.40),(-.06,.075,.40)],[(0,1,2,3),(4,5,6,7,8,9)],'Cream')
fishmarkereye=path('MarkerEye',[(.10,.02,.43),(.108,.02,.43)],'Inset',.012)
fishmarkereye.parent=fishmarker
horizontal_progress=.20
timeline=[]
""")
source=source.replace("    tension.scale.x=max(.001,tensionvalue*(W-.04)); key(tension,'scale',f)", """
    # Scripted tracking example: fixed-size window follows a moving fish, with brief misses.
    playing=9.65<=t<15.8
    u=max(0,t-9.65)
    fishpos=.50+.28*math.sin(u*2.0)+.035*math.sin(u*5)
    barpos=max(.14,min(.86,fishpos-.10*math.sin(u*3.4)-.08))
    inside=abs(fishpos-barpos)<=.14
    if playing:
        horizontal_progress=min(1,max(0,horizontal_progress+(1/12)*(.19 if inside else -.12)))
        for i in [6,7]:
            visible(headings[i],i==(6 if inside else 7),f)
            visible(descriptions[i],i==(6 if inside else 7),f)
    width=.28*(W-.04)
    tension.scale.x=width; tension.location=(X+.02+(barpos-.14)*(W-.04),.05,0)
    key(tension,'scale',f); key(tension,'location',f)
    zoneborder.location=(tension.location.x,.05,0); zoneborder.scale=(width,1,1)
    key(zoneborder,'location',f); key(zoneborder,'scale',f)
    fishmarker.location=(X+.02+fishpos*(W-.04),.275,0); key(fishmarker,'location',f)
    for o in [tension,zoneborder,fishmarker,fishmarkereye]:
        o.hide_render=not playing; key(o,'hide_render',f)
    pointer.hide_render=True; key(pointer,'hide_render',f)
    if playing: prog=horizontal_progress
""")
exec(compile(source,str(base)+' [horizontal-v03]','exec'))
