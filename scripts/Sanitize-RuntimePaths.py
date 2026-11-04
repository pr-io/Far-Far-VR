"""Remove local developer profile roots from the pinned native build's strings.

Only equal-length replacements in non-executable, read-only PE sections are
permitted. The original binary remains untouched; executable bytes, file size,
section offsets and pointers are preserved. Run before creating a release delta.
"""
import argparse
import hashlib
import re
import struct
from pathlib import Path

SOURCE_SHA256 = 'E784A9946B924FEDCB7A174473EA6DB784399AE40BCFBB6F2FE94E2DC10096C0'


def sanitize(data):
    if hashlib.sha256(data).hexdigest().upper() != SOURCE_SHA256:
        raise ValueError('Input is not the pinned pre-sanitization native build.')
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    if data[:2] != b'MZ' or data[pe:pe + 4] != b'PE\0\0':
        raise ValueError('Invalid PE image.')
    count = struct.unpack_from('<H', data, pe + 6)[0]
    optional = struct.unpack_from('<H', data, pe + 20)[0]
    sections = []
    for index in range(count):
        offset = pe + 24 + optional + index * 40
        size, start = struct.unpack_from('<II', data, offset + 16)
        flags = struct.unpack_from('<I', data, offset + 36)[0]
        sections.append((start, start + size, flags))
    result = bytearray(data)
    edits = 0
    for match in re.finditer(rb'[A-Za-z]:\\Users\\[^\\\x00\r\n]+(?=\\)', data, re.IGNORECASE):
        if not any(start <= match.start() and match.end() <= end and
                   not flags & 0xA0000000 and flags & 0x40000000
                   for start, end, flags in sections):
            raise ValueError('Profile path is outside read-only, non-executable data.')
        replacement = b'C:\\Build\\' + b'_' * (len(match.group()) - len(b'C:\\Build\\'))
        if len(replacement) != len(match.group()):
            raise ValueError('Replacement would resize the image.')
        result[match.start():match.end()] = replacement
        edits += 1
    if not edits or re.search(rb'[A-Za-z]:\\Users\\', result, re.IGNORECASE):
        raise ValueError('Sanitization did not remove every local profile root.')
    for start, end, flags in sections:
        if flags & 0x20000000 and data[start:end] != result[start:end]:
            raise ValueError('Executable section changed.')
    if len(result) != len(data):
        raise ValueError('Image size changed.')
    return bytes(result), edits


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.input.resolve() == args.output.resolve():
        raise SystemExit('Choose a separate output file; the source is preserved.')
    output, edits = sanitize(args.input.read_bytes())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(output)
    print('Sanitized diagnostic profile roots:', edits)
    print('SHA256:', hashlib.sha256(output).hexdigest().upper())
