"""Emit the Lua data module from the launcher's shared settings catalog."""
import json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
version = (root / 'version.txt').read_text().strip()
(root / 'Profile/scripts/farfarvr_version.lua').write_text('return '+json.dumps('v'+version)+'\n')
(root / 'Launcher/ProductVersion.cs').write_text('namespace FarFarWestVR;\ninternal static class ProductVersion { internal const string Value = '+json.dumps(version)+'; }\n')
# Local build metadata must not rewrite public release download links.
for name in ([] if '+' in version else ['README.md','BUILD.md']):
    path=root/name
    import re
    path.write_text(re.sub(r'0\.4\.\d+',version,path.read_text(encoding='utf-8-sig')),encoding='utf-8')
catalog = json.loads((root / 'Profile/VRSettings.json').read_text())
def lua(value):
    if isinstance(value, dict): return '{' + ','.join('['+json.dumps(k)+']='+lua(v) for k,v in value.items()) + '}'
    if isinstance(value, list): return '{' + ','.join(lua(v) for v in value) + '}'
    if isinstance(value, bool): return 'true' if value else 'false'
    return json.dumps(value)
(root / 'Profile/scripts/farfarvr_settings.lua').write_text('-- Generated from Profile/VRSettings.json.\nreturn ' + lua(catalog) + '\n')
