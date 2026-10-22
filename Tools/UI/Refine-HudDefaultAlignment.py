"""Align the approved defaults without changing control sizes or rebuilding prefabs."""
from pathlib import Path
import json,re
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Resources/UI/HudLayoutDefaults.json'
data=json.loads(path.read_text(encoding='utf-8'))
items={entry['Control']:entry['Placement'] for entry in data['Controls']}
def bounds(c):
    p=items[c];return [1066*p['Anchor']['x']+p['Offset']['x'],600*p['Anchor']['y']+p['Offset']['y'],p['Size']['x'],p['Size']['y']]
def move(c,x,y):
    r=bounds(c);items[c]['Offset']['x']+=x-r[0];items[c]['Offset']['y']+=y-r[1]
actions=[7,8,9,10];rects=[bounds(c) for c in actions]
left,right=rects[0][0],rects[3][0]+rects[3][2]
total=sum(r[2] for r in rects);gap=max(12,(right-left-total)/3);total+=3*gap
start=max(0,min(1066-total,(left+right-total)/2))
cy=min(600-max(r[3] for r in rects)/2-46,sum(r[1]+r[3]/2 for r in rects)/4)
x=start
for c,r in zip(actions,rects):move(c,x,cy-r[3]/2);x+=r[2]+gap
weapon=bounds(6);move(6,start+total/2-weapon[2]/2,min(weapon[1],cy-max(r[3] for r in rects)/2-weapon[3]-18))
fire,aim=bounds(13),bounds(12);move(12,fire[0]+fire[2]/2-aim[2]/2,aim[1])
joystick=bounds(11);move(11,joystick[0],min(joystick[1],600-joystick[3]-46))
food=bounds(1);move(1,bounds(0)[0],food[1]);move(2,bounds(2)[0],food[1])
path.write_text(json.dumps(data,ensure_ascii=False,indent=4)+'\n',encoding='utf-8')
prefab=ROOT/'Assets/Resources/UI/Prefabs/Hud.prefab'
source=prefab.read_text(encoding='utf-8')
for match in list(re.finditer(r'^  - Control: (\d+)\n.*?(?=^  - Control:|^  Panel:)',source,re.M|re.S)):
    c=int(match.group(1));frame=int(re.search(r'    Background: \{fileID: (\d+)\}',match.group(0)).group(1))
    block=re.search(r'^--- !u!114 &'+str(frame)+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S).group(0)
    if '  HudStyle:' not in block:source=source.replace(block,block+'  HudStyle: 1\n  Joystick: '+str(int(c==11))+'\n')
prefab.write_text(source,encoding='utf-8')
print('Aligned approved defaults; sizes preserved. Added HUD-native style flags to existing frames.')
