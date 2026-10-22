"""Package the existing Blender frames, preserving a fixed canvas and pivot."""
from pathlib import Path
from PIL import Image
import uuid

root = Path(__file__).resolve().parents[1]
source = root / 'Prototypes/MonsterAnimationBatch/Chest'
target = root / 'Assets/Resources/Environment/Chest'
target.mkdir(parents=True, exist_ok=True)
template = (root / 'Assets/Resources/Monsters/Stalker/ClawAttack.png.meta').read_text()
for clip, first in [('Open', 101), ('Close', 201)]:
    atlas = Image.new('RGBA', (2048, 768))
    for index in range(20):
        with Image.open(source / clip / f'{first + index}.png') as frame:
            atlas.paste(frame.convert('RGBA').crop((64, 64, 320, 320)),
                        (index % 8 * 256, index // 8 * 256))
    output = target / f'{clip}.png'
    atlas.save(output)
    meta = output.with_suffix('.png.meta')
    if not meta.exists():
        lines = template.splitlines()
        lines[1] = 'guid: ' + uuid.uuid4().hex
        meta.write_text('\n'.join(lines) + '\n')
