"""Render upper-body action layers and partial reload variants from saved Blender models."""
import bpy,sys
from pathlib import Path
BASE=Path(__file__).resolve().parent; OUT=BASE/'PendingRuntimeExport'; OUT.mkdir(exist_ok=True)
legs=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco')
# Construct the one-shell variant without changing the approved two-shell file.
if '--rifle-partials-only' not in sys.argv:
    source=BASE/'build_shotgun_reload.py'; previous_args=sys.argv[:]
    sys.argv=[str(source),'--single-shell','--no-render']
    exec(compile(source.read_text(encoding='utf-8'),str(source),'exec'),{'__file__':str(source),'__name__':'__main__'})
    sys.argv=previous_args
clips=[('SwitchRifle',BASE/'RifleSwitch/troca-pistola-rifle.blend',range(1,33),0),
       ('ReloadPistol',BASE/'PistolReload/protagonista-recarga-pistola.blend',range(1,38),0),
       ('ReloadShotgun',BASE/'ShotgunReload/protagonista-recarga-shotgun.blend',range(1,65),0),
       ('ReloadShotgunSingle',BASE/'ShotgunReloadSingle/protagonista-recarga-shotgun.blend',range(14,56),0),
       ('ReloadRifle',BASE/'RifleReload/protagonista-recarga-rifle.blend',range(1,79),0)]
for name in ['ShotgunToPistol','RifleToPistol','ShotgunToRifle','RifleToShotgun']:
    clips.append((name,BASE/'WeaponTransitions'/name/(name+'.blend'),range(1,37),0))
for quantity in range(1,5): clips.append(('ReloadRifleRounds'+str(quantity),BASE/'RifleReload/protagonista-recarga-rifle.blend',range(29,52),quantity))
if '--rifle-partials-only' in sys.argv: clips=[c for c in clips if c[3]>0]
for name,model,frames,quantity in clips:
    bpy.ops.wm.open_mainfile(filepath=str(model)); scene=bpy.context.scene
    for o in scene.objects:
        if o.type in {'MESH','CURVE'} and o.name.startswith(legs): o.hide_render=True
    scene.render.resolution_x=256; scene.render.resolution_y=256; scene.render.resolution_percentage=100; scene.cycles.samples=16
    folder=OUT/name; folder.mkdir(exist_ok=True)
    if quantity:
        for i in range(quantity+1,6):
            for prefix in ['Municao ','Ponta municao ']:
                o=bpy.data.objects[prefix+str(i)]
                # Blender re-evaluates animated visibility when starting a render.
                # Mute only absent rounds' visibility tracks so the override survives.
                for curve in o.animation_data.action.fcurves:
                    if curve.data_path=='hide_render': curve.mute=True
                o.hide_render=True
    for f in frames:
        scene.frame_set(f)
        scene.render.filepath=str(folder/f'{f:03}.png'); bpy.ops.render.render(write_still=True)
    print('RUNTIME ACTION READY '+name,flush=True)
if '--rifle-partials-only' in sys.argv:
    print('RIFLE PARTIAL LAYERS EXPORTED',flush=True)
    raise SystemExit(0)
# Align the rifle's walking/idle hands with the approved switch/reload endpoint.
bpy.ops.wm.open_mainfile(filepath=str(BASE/'VisualV1/personagem-rustico.blend')); scene=bpy.context.scene
gun_names=('Ferrolho pistola','Empunhadura pistola','Mira traseira','Massa de mira','Janela superior','Ranhura ferrolho')
arm_names=('Braco fixo','Antebraco fixo','Mao fixa','Ombro tecido','Cotovelo tecido','Remendo cotovelo','Ponto remendo','Costura ombro','Dobra manga')
for o in scene.objects:
    if o.name.startswith(gun_names+arm_names): o.hide_render=True
with bpy.data.libraries.load(str(BASE/'RifleSwitch/troca-pistola-rifle.blend'),link=False) as (src,dst):
    dst.objects=[n for n in src.objects if n.startswith(('CTRL - sacar rifle','Rifle ','Manga troca','Antebraco troca','Cotovelo troca','Mao troca','Punho troca','Ombro troca'))]
for o in dst.objects:
    if o: scene.collection.objects.link(o)
scene.frame_set(32); bpy.context.view_layer.update()
for o in dst.objects:
    if o and o.animation_data: o.animation_data_clear()
scene.camera=bpy.data.objects['Camera jogo - topo']; scene.render.resolution_x=256; scene.render.resolution_y=256; scene.render.resolution_percentage=100; scene.cycles.samples=16
folder=OUT/'Mosin'; folder.mkdir(exist_ok=True)
for f in range(1,33):
    scene.frame_set(f); scene.render.filepath=str(folder/f'{f:03}.png'); bpy.ops.render.render(write_still=True)
print('ALL PENDING PLAYER ACTIONS EXPORTED',flush=True)
