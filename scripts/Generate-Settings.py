"""Emit the Lua data module from the launcher's shared settings catalog."""
import json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
catalog = json.loads((root / 'Profile/VRSettings.json').read_text())
def lua(value):
    if isinstance(value, dict): return '{' + ','.join('['+json.dumps(k)+']='+lua(v) for k,v in value.items()) + '}'
    if isinstance(value, list): return '{' + ','.join(lua(v) for v in value) + '}'
    if isinstance(value, bool): return 'true' if value else 'false'
    return json.dumps(value)
(root / 'Profile/scripts/farfarvr_settings.lua').write_text('-- Generated from Profile/VRSettings.json.\nreturn ' + lua(catalog) + '\n')
