"""Patch only the four requested existing Enhanced Input key overrides.

The game stores these records in a custom serialized profile after its GVAS
properties. Replace FString pairs including their own lengths; preserve all
record metadata and all other bytes. Refuse unfamiliar or ambiguous saves.
"""
from pathlib import Path
import argparse
import datetime
import hashlib
import os
import struct


def fstring(text):
    data = text.encode('ascii') + b'\0'
    return struct.pack('<i', len(data)) + data


CHANGES = (
    ('InputGrenade', 'Gamepad_LeftTrigger', 'Gamepad_RightStick_Down'),
    ('InputMap', 'Gamepad_RightThumbstick', 'Gamepad_Special_Left'),
    ('InputPing', 'FFW_Gamepad_Unbound', 'Gamepad_RightThumbstick'),
    ('InputAim', 'Gamepad_RightStick_Down', 'Gamepad_LeftTrigger'),
)


def patch(data):
    if not data.startswith(b'GVAS') or b'/Script/FarFarWest.FFWEnhancedInputUserSettings\0' not in data:
        raise ValueError('Unrecognized Far Far West input save; nothing changed.')
    result = data
    for action, old, new in CHANGES:
        before, after = fstring(action) + fstring(old), fstring(action) + fstring(new)
        if result.count(after) == 1 and result.count(before) == 0:
            continue
        if result.count(before) != 1 or result.count(after) != 0:
            raise ValueError(f'Unexpected binding for {action}; nothing changed.')
        result = result.replace(before, after, 1)
    # Reverse replacements prove every unrelated byte was retained.
    reverse = result
    for action, old, new in reversed(CHANGES):
        before, after = fstring(action) + fstring(old), fstring(action) + fstring(new)
        if before in data:
            reverse = reverse.replace(after, before, 1)
    if reverse != data:
        raise ValueError('Unrelated data changed; nothing written.')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    path = Path(os.environ['LOCALAPPDATA']) / 'FarFarWest/Saved/SaveGames/EnhancedInputUserSettings.sav'
    original = path.read_bytes()
    updated = patch(original)
    for action, old, new in CHANGES:
        print(f'{action}: {new}')
    if args.apply and original != updated:
        # Guard against an open game overwriting the patched input save.
        import subprocess
        running = subprocess.check_output(['tasklist', '/FI', 'IMAGENAME eq FarFarWest-Win64-Shipping.exe', '/NH'])
        if b'FarFarWest-Win64-Shipping' in running:
            raise SystemExit('Close the game before applying bindings.')
        stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
        backup = path.with_name(path.name + '.before-vr-input-' + stamp)
        backup.write_bytes(original)
        # Do not overwrite a save changed since the read above.
        if path.read_bytes() != original:
            raise SystemExit('Input save changed during preparation; nothing written.')
        path.write_bytes(updated)
        assert path.read_bytes() == updated
        print(f'Backup: {backup}')
        print(f'SHA256: {hashlib.sha256(updated).hexdigest()}')
    elif original == updated:
        print('Bindings already applied.')
    else:
        print('Preview only. Use --apply with the game closed.')
