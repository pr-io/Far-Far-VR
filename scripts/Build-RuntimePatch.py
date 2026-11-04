"""Publisher-only: generate a Windows delta; players need no Python/compiler.
This does not establish permission to distribute the generated patch.
"""
import argparse,ctypes as c,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--base',required=True);p.add_argument('--target',required=True);p.add_argument('--output',default='Runtime/backend.ffvrdelta');args=p.parse_args()
source=Path(args.base).read_bytes();target=Path(args.target).read_bytes()
assert hashlib.sha256(source).hexdigest().upper()=='FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB','Unexpected upstream source'
assert hashlib.sha256(target).hexdigest().upper()=='6B4081352D2E23061F87C3086FDEE84A857682E95EAEB8A016C4C9BD7357B341','Unexpected target; only the explicitly pinned candidate engine is accepted'
class Input(c.Structure):_fields_=[('ptr',c.c_void_p),('size',c.c_size_t),('editable',c.c_int)]
class Output(c.Structure):_fields_=[('ptr',c.c_void_p),('size',c.c_size_t)]
api=c.WinDLL('msdelta.dll',use_last_error=True)
api.CreateDeltaB.argtypes=[c.c_longlong,c.c_longlong,c.c_longlong,Input,Input,Input,Input,Input,c.c_void_p,c.c_uint,c.POINTER(Output)];api.CreateDeltaB.restype=c.c_int
api.DeltaFree.argtypes=[c.c_void_p]
a=c.create_string_buffer(source);b=c.create_string_buffer(target);out=Output();timestamp=c.c_uint64(0)
if not api.CreateDeltaB(1,0,0,Input(c.cast(a,c.c_void_p),len(source),0),Input(c.cast(b,c.c_void_p),len(target),0),Input(),Input(),Input(),c.byref(timestamp),0,c.byref(out)):raise c.WinError(c.get_last_error())
try:
 data=c.string_at(out.ptr,out.size);path=Path(args.output);path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
 print('Patch SHA256:',hashlib.sha256(data).hexdigest().upper());print('Patch bytes:',len(data))
finally:api.DeltaFree(out.ptr)
