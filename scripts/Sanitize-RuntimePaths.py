"""Remove local developer profile roots from the pinned native build's strings.

Only equal-length replacements in non-executable, read-only PE sections are
permitted. The original binary remains untouched; executable bytes, file size,
section offsets and pointers are preserved. Run before creating a release delta.
"""
import argparse
import hashlib
import math
import re
import struct
from pathlib import Path

SOURCE_SHA256 = 'E784A9946B924FEDCB7A174473EA6DB784399AE40BCFBB6F2FE94E2DC10096C0'


def shader_digest(data):
    """DXBC's legacy container digest, not a security/integrity hash.

    Format reference: Microsoft's DirectXShaderCompiler DxilHash.cpp,
    ComputeHashRetail. Its final-block layout differs from ordinary MD5.
    https://github.com/microsoft/DirectXShaderCompiler/blob/main/lib/DxilHash/DxilHash.cpp
    """
    mask = 0xffffffff
    shifts = [7, 12, 17, 22] * 4 + [5, 9, 14, 20] * 4 + [4, 11, 16, 23] * 4 + [6, 10, 15, 21] * 4
    constants = [int(abs(math.sin(i + 1)) * 2 ** 32) & mask for i in range(64)]
    size = len(data)
    full = size // 64 * 64
    tail = data[full:]
    bits = struct.pack('<I', (size * 8) & mask)
    end = struct.pack('<I', (size * 2) | 1)
    if len(tail) < 56:
        padded = data[:full] + bits + tail + b'\x80' + b'\0' * (55 - len(tail)) + end
    else:
        padded = data[:full] + tail + b'\x80' + b'\0' * (63 - len(tail)) + bits + b'\0' * 56 + end
    state = [0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476]
    for offset in range(0, len(padded), 64):
        words = struct.unpack_from('<16I', padded, offset)
        a, b, c, d = state
        for i in range(64):
            if i < 16:
                f, index = (b & c) | (~b & d), i
            elif i < 32:
                f, index = (d & b) | (~d & c), (5 * i + 1) % 16
            elif i < 48:
                f, index = b ^ c ^ d, (3 * i + 5) % 16
            else:
                f, index = c ^ (b | ~d), 7 * i % 16
            value = (a + f + constants[i] + words[index]) & mask
            value = ((value << shifts[i]) | (value >> (32 - shifts[i]))) & mask
            a, d, c, b = d, c, b, (b + value) & mask
        state = [(x + y) & mask for x, y in zip(state, [a, b, c, d])]
    return struct.pack('<4I', *state)


def repair_shader_containers(original, result):
    """Preserve GPU program chunks and update checksums after metadata edits."""
    repaired = 0
    for match in re.finditer(b'DXBC', original):
        start = match.start()
        if start + 32 > len(original):
            continue
        version, size, count = struct.unpack_from('<III', original, start + 20)
        if version != 1 or not 1 <= count <= 100 or size < 32 + count * 4 or start + size > len(original):
            continue
        chunks = []
        for index in range(count):
            offset = struct.unpack_from('<I', original, start + 32 + index * 4)[0]
            if offset < 32 + count * 4 or offset + 8 > size:
                break
            length = struct.unpack_from('<I', original, start + offset + 4)[0]
            if offset + 8 + length > size:
                break
            chunks.append((start + offset, start + offset + 8 + length))
        if len(chunks) != count:
            continue
        for first, last in chunks:
            if original[first:first + 4] in (b'SHDR', b'SHEX', b'DXIL') and original[first:last] != result[first:last]:
                raise ValueError('GPU program chunk changed; refusing publication.')
        if original[start:start + size] == result[start:start + size]:
            continue
        if shader_digest(original[start + 20:start + size]) != original[start + 4:start + 20]:
            raise ValueError('Unrecognized original shader container digest.')
        result[start + 4:start + 20] = shader_digest(bytes(result[start + 20:start + size]))
        repaired += 1
    return repaired


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
    # Read-only data also contains compiled GPU programs. Metadata inside a
    # DXBC container is covered by its digest even though .text is unchanged.
    repaired = repair_shader_containers(data, result)
    if repaired != 2:
        raise ValueError('Unexpected number of shader metadata containers.')
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
