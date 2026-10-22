"""Production export from aranha-palida-acoes.blend through Blender MCP.

Load the authored scene, then run this script once. Each timer callback exports one
frame; render-status.json is the completion source of truth. Never run twice while
an export is active. No Unity validation or builds are started by this script.
"""
import bpy, json
from pathlib import Path

BASE=Path(__file__).resolve().parent/'Actions'
scene=bpy.context.scene
if not scene.objects.get('CTRL-WeaverActions'):
    raise RuntimeError('Select the authored Weaver scene before exporting.')
previous=bpy.app.driver_namespace.get('weaver_export')
if previous and not previous['finished']:
    raise RuntimeError('An export is already active.')
frames=list(range(201,233))+list(range(301,333))+[401]
state={'done':0,'total':len(frames),'error':None,'finished':False}
bpy.app.driver_namespace['weaver_export']=state
for folder in ['Dash','Descend','Nest']: (BASE/folder).mkdir(parents=True,exist_ok=True)
def render_next():
    try:
        frame=frames[state['done']]
        folder='Nest' if frame==401 else 'Dash' if frame<300 else 'Descend'
        scene.frame_set(frame); scene.render.filepath=str(BASE/folder/f'{frame}.png')
        bpy.ops.render.render(write_still=True,scene=scene.name)
        state['done']+=1; state['finished']=state['done']==state['total']
    except Exception as error:
        state['error']=str(error); state['finished']=True
    (BASE/'render-status.json').write_text(json.dumps(state),encoding='utf-8')
    return None if state['finished'] else .1
bpy.app.timers.register(render_next,first_interval=1)
