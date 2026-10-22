"""Render the approved shotgun grip on the original walking rig, plus a legs-only layer."""
import bpy, sys
from pathlib import Path
BASE=Path(__file__).resolve().parent
OUT=BASE/'ShotgunSwitch'/'RuntimeExport'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'VisualV1/personagem-rustico.blend'))
scene=bpy.context.scene
gun_names=('Ferrolho pistola','Empunhadura pistola','Mira traseira','Massa de mira','Janela superior','Ranhura ferrolho')
arm_names=('Braco fixo','Antebraco fixo','Mao fixa','Ombro tecido','Cotovelo tecido','Remendo cotovelo','Ponto remendo','Costura ombro','Dobra manga')
for obj in scene.objects:
    if obj.name.startswith(gun_names+arm_names): obj.hide_render=True
prefixes=('CTRL - sacar shotgun','Shotgun ','Manga troca','Antebraco troca','Cotovelo troca','Mao troca','Punho troca','Ombro troca')
with bpy.data.libraries.load(str(BASE/'ShotgunSwitch'/'troca-pistola-shotgun.blend'),link=False) as (source,destination):
    destination.objects=[name for name in source.objects if name.startswith(prefixes)]
for obj in destination.objects:
    if obj is not None: scene.collection.objects.link(obj)
scene.frame_set(32); bpy.context.view_layer.update()
for obj in destination.objects:
    if obj is not None and obj.animation_data: obj.animation_data_clear()
scene.camera=bpy.data.objects['Camera jogo - topo']
scene.render.resolution_x=256; scene.render.resolution_y=256; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.cycles.samples=16
legs=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco')
for variant in ([] if '--upper-only' in sys.argv else ['Shotgun','Legs']):
    folder=OUT/variant; folder.mkdir(parents=True,exist_ok=True)
    if variant=='Legs':
        for obj in scene.objects:
            if obj.type in {'MESH','CURVE'} and not obj.name.startswith(legs): obj.hide_render=True
    for frame in range(1,33):
        scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png')
        bpy.ops.render.render(write_still=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'ShotgunSwitch'/'troca-pistola-shotgun.blend'))
scene=bpy.context.scene
for obj in scene.objects:
    if obj.type in {'MESH','CURVE'} and obj.name.startswith(legs): obj.hide_render=True
scene.render.resolution_x=256; scene.render.resolution_y=256
folder=OUT/'SwitchShotgun'; folder.mkdir(parents=True,exist_ok=True)
for frame in range(1,33):
    scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png')
    bpy.ops.render.render(write_still=True)
print('SHOTGUN RUNTIME FRAMES EXPORTED',flush=True)
