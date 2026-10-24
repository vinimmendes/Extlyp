"""Render the review storyboard and a same-layout Simple theme still."""
import bpy
from pathlib import Path

out=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/Fishing/Preview-v02')
s=bpy.data.scenes['Fishing-Preview-v02']
bpy.context.window.scene=s
frames=out/'frames'; frames.mkdir(exist_ok=True)
for f in range(1,217):
    dest=frames/f'frame_{f:04d}.png'
    if dest.exists() and f not in set(range(1,15)) | set(range(109,117)):
        continue
    s.frame_set(f); s.render.filepath=str(dest)
    bpy.ops.render.render(write_still=True)

# Theme adaptation belongs to this separate background process; source stays Somber.
colors={
    'Panel':(.025,.041,.030),'Inset':(.012,.025,.018),
    'Button':(.050,.092,.058),'Edge':(.12,.22,.145),
    'EdgeLight':(.25,.39,.26),'Text':(.82,.87,.80),'Muted':(.39,.50,.41),
}
for name,color in colors.items():
    m=bpy.data.materials['MAT-FV02-'+name]
    socket=m.node_tree.nodes['Color'].inputs[0]
    for link in list(socket.links): m.node_tree.links.remove(link)
    socket.default_value=(*color,1)
bpy.data.objects['TXT-FV02-BuildLabel'].data.body='CONCEITO V02  ·  SIMPLES'
s.frame_set(140); s.render.filepath=str(out/'concept-simple.png')
bpy.ops.render.render(write_still=True)
print('FISHING_PREVIEW_RENDER_COMPLETE',flush=True)
