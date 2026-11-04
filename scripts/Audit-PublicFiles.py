"""Inspect tracked publication inputs without printing potential secret values."""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PATTERNS = {
    'local Windows profile path': re.compile(rb'[A-Za-z]:[\\/]Users[\\/][A-Za-z0-9_. -]+[\\/]', re.I),
    'GitHub credential': re.compile(rb'(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,})'),
    'private key': re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----'),
    'AWS access key': re.compile(rb'AKIA[0-9A-Z]{16}'),
    'Steam account identifier': re.compile(rb'7656119[0-9]{10}'),
}
PRIVATE_NAMES = {'github_token.txt', 'id_rsa', 'id_ed25519', '.env'}


def findings(data):
    # Also inspect embedded UTF-16 strings in Windows settings and media files.
    variants = [data, data.decode('utf-16le', errors='ignore').encode('utf-8')]
    return [name for name, pattern in PATTERNS.items() if any(pattern.search(value) for value in variants)]


if __name__ == '__main__':
    names = subprocess.check_output(['git', 'ls-files', '-z'], cwd=ROOT).decode('utf-8').split('\0')
    issues = []
    for name in filter(None, names):
        path = ROOT / name
        if path.name.lower() in PRIVATE_NAMES or path.suffix.lower() in {'.pem', '.pfx', '.p12', '.key'}:
            issues.append((name, 'private credential file'))
        if path.is_file():
            issues.extend((name, kind) for kind in findings(path.read_bytes()))
    for name, kind in issues:
        print(f'{name}: review required ({kind}); value withheld')
    if issues:
        raise SystemExit('Publication audit found potentially private content.')
    print('Tracked-file publication audit completed without matching private-data patterns.')
