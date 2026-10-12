"""Inventario e calculos geometricos da auditoria; nao substitui Play Mode."""
from pathlib import Path
import hashlib
import json
import math
import re
import argparse

root = Path(__file__).resolve().parents[2]
scripts = sorted((root / "Assets/Scripts").rglob("*.cs"))
editors = sorted((root / "Assets/Editor").rglob("*.cs"))
map_text = (root / "Assets/Scripts/Match/MapBuilder.cs").read_text(encoding="utf-8-sig")
config = (root / "Assets/Scripts/Core/GameConfig.cs").read_text(encoding="utf-8-sig")
policy = (root / "Assets/Scripts/Bots/BotDifficulty.cs").read_text(encoding="utf-8-sig")

def points(name):
    declaration = re.search(r"\b" + name + r"\s*=\s*\{([^}]+)\}", map_text).group(1)
    values = []
    for match in re.finditer(r"new Vector2\(([-\d.]+),\s*([-\d.]+)\)|Vector2.zero", declaration):
        values.append((float(match.group(1)), float(match.group(2))) if match.group(1) else (0.0, 0.0))
    return values

def constant(text, name):
    return float(re.search(r"\b" + name + r"\s*=\s*([\d.]+)", text).group(1))

speed = constant(config, "PlayerSpeed")
hold = constant(config, "ExtractionHoldTime")
unlock = constant(policy, "ExtractionUnlockSeconds")
spawns, exits = points("largeSpawns"), points("largeExits")
travel = []
for slot, point in enumerate(spawns):
    distance = min(math.dist(point, exit_point) for exit_point in exits)
    # Chegar a borda da zona 5x3.5 e suficiente: sua meia diagonal e um
    # desconto conservador, somado ao raio .4 do participante.
    conservative_distance = max(0, distance - math.hypot(2.5, 1.75) - .4)
    minimum_seconds = conservative_distance / speed + hold
    travel.append({"slot": slot, "spawn": point, "nearest_exit_center_units": round(distance, 3),
                   "optimistic_minimum_seconds_to_extract": round(minimum_seconds, 3),
                   "fits_last_minute_from_spawn": minimum_seconds <= unlock})

source_files = scripts + editors + sorted((root / "ProjectSettings").glob("*.asset")) + [root / "Packages/manifest.json", root / "Packages/packages-lock.json"]
source_files += sorted(root.glob("*.ps1"))
source_files = [p for p in source_files if p.exists()]
skills = sorted((root / ".agents/skills").glob("*/SKILL.md"))
result = {
    "date_local": "2026-10-09", "timezone": "America/Sao_Paulo",
    "scope": "inventario estatico e limites geometricos; sem simulacao de fisica ou medicao de FPS",
    "runtime_scripts": len(scripts), "editor_scripts": len(editors), "installed_skills": len(skills),
    "on_gui_runtime_files": [str(p.relative_to(root)).replace("\\", "/") for p in scripts if "OnGUI(" in p.read_text(encoding="utf-8-sig")],
    "gameplay_action_consumers": [str(p.relative_to(root)).replace("\\", "/") for p in scripts if re.search(r"\b(InputActionReference|InputActionAsset|PlayerInput)\b", p.read_text(encoding="utf-8-sig"))],
    "safe_area_consumers": [str(p.relative_to(root)).replace("\\", "/") for p in scripts if "Screen.safeArea" in p.read_text(encoding="utf-8-sig")],
    "large_map_bot_travel_lower_bounds": travel,
    "source_sha256": {str(p.relative_to(root)).replace("\\", "/"): hashlib.sha256(p.read_bytes()).hexdigest() for p in source_files},
    "skill_sha256": {p.parent.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in skills},
}
args = argparse.ArgumentParser()
args.add_argument("--output", default="Docs/Auditoria/source-evidence.json")
options = args.parse_args()
output = root / options.output
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({key: result[key] for key in ("runtime_scripts", "editor_scripts", "installed_skills", "large_map_bot_travel_lower_bounds")}, ensure_ascii=False, indent=2))
