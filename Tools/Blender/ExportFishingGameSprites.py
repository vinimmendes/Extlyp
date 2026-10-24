"""Render separate reusable assets from the approved v03 scene; never save its changes."""
import bpy, math
from pathlib import Path
out=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/Fishing/GameSprites-v01')
out.mkdir(parents=True,exist_ok=True)
s=bpy.data.scenes['Fishing-Preview-v03']; bpy.context.window.scene=s
s.frame_set(117)
# Rendering evaluates animated visibility again. Detach storyboard actions in this
# disposable process so isolated passes cannot restore hidden UI or old transforms.
for o in s.objects:
    o.animation_data_clear()
    if o.data is not None and hasattr(o.data,'animation_data_clear'): o.data.animation_data_clear()
body=bpy.data.objects['GEO-FV03-FishBody']
body.data.shape_keys.animation_data_clear()
s.render.film_transparent=True; s.render.resolution_percentage=100; s.eevee.taa_render_samples=32
camera=s.camera; camera.location=(0,0,20); camera.rotation_euler=(0,0,0)
def show(objects):
    allowed=set(objects)
    for o in s.objects: o.hide_render=o not in allowed and o!=camera
def render(name,w,h,ortho):
    s.render.resolution_x=w; s.render.resolution_y=h; camera.data.ortho_scale=ortho
    s.render.filepath=str(out/name); bpy.ops.render.render(write_still=True)
fish=bpy.data.objects['ARM-FV03-Fish']; bobber=bpy.data.objects['ARM-FV03-Bobber']
for i in range(12):
    swim=math.sin(i*math.tau/12)
    body.data.shape_keys.key_blocks['TailLeft'].value=max(0,swim)
    body.data.shape_keys.key_blocks['TailRight'].value=max(0,-swim)
    bpy.data.objects['ARM-FV03-Tail'].rotation_euler.z=.25*swim
    bpy.data.objects['ARM-FV03-Tail'].location.y=.09*swim
    show([fish,*fish.children_recursive]); fish.location=(0,0,0); fish.rotation_euler=(0,0,0); fish.scale=(1,1,1)
    render(f'fish_{i:02}.png',256,128,1.6)
for i in range(12):
    s.frame_set(117+i); show([bobber,*bobber.children_recursive])
    bobber.location=(0,0,0); bobber.rotation_euler=(0,0,math.sin(i*math.tau/12)*.12); bobber.scale=(1,1,1)
    render(f'bobber_{i:02}.png',96,128,.42)
s.frame_set(1)
worm=bpy.data.objects['GEO-FV03-WormIcon']; show([worm]); worm.location=(-2.907,-2.14,0)
render('worm.png',128,128,.65)
spool=[o for o in s.objects if o.name.startswith('GEO-FV03-Spool')]
show(spool)
for o in spool: o.location=(-3.02,1.60,0)
render('spool.png',128,128,.85)
s.frame_set(1)
water=[o for o in s.objects if any(o.name.startswith('GEO-FV03-'+p) for p in ['WaterDash','Plank','Grain','Nail']) or o.name=='GEO-FV03-Water']
show(water); camera.location=(-2.675,-.07,20)
render('water-pier.png',768,462,9.15)
ring=bpy.data.objects['GEO-FV03-Ring0']; show([ring]); ring.location=(0,0,0); ring.scale=(1,1,1)
camera.location=(0,0,20)
render('ripple.png',128,128,2.15)
print('FISHING_SPRITES_RENDERED',flush=True)
