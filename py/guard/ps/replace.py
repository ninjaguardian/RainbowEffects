import struct
import tempfile
import subprocess
from pathlib import Path
from common.common import DIR, COMMON

INPUT = DIR / "decompressed.bin"
OUTPUT = DIR / "decompressed_mod.bin"
DISASSEMBLE_EXE = COMMON / "HLSLDecompiler.exe"

LOAD = "l(1.000000,0.205078766,0,0)"
TARGET = "cb0[3]"
# TARGET = "l(0,1.000000,1.000000,0)"
EXTRA_NOPS = 2
LOAD_BYTES = bytes.fromhex("024000000000803f2b00523e0000000000000000")

data = INPUT.read_bytes()
start_len = len(data)
pos = 0
while True:
    pos = data.find(LOAD_BYTES, pos)
    if pos == -1:
        break

    start = data.rfind(b"DXBC", 0, pos)
    assert start != -1, f"Invalid match at 0x{pos:X}"

    total_size: int = struct.unpack_from("<I", data, start + 24)[0]

    dxbc = data[start:start + total_size]

    with tempfile.TemporaryDirectory() as tmp_str:
        tmp = Path(tmp_str)

        inp = tmp / "shader.dxbc"
        inp.write_bytes(dxbc)

        dxbc_path = str(inp)

        subprocess.run([DISASSEMBLE_EXE, "-V", "-S", "-d", dxbc_path], check=True)

        asm = inp.with_suffix(".asm")
        assert asm.exists(), "ASM not generated"

        patched = tmp / "shader_patched.asm"

        code = asm.read_text().replace(LOAD, TARGET)
        code += "nop\n" * EXTRA_NOPS
        # print(code)
        patched.write_text(code)

        subprocess.run([DISASSEMBLE_EXE, "--copy-reflection", dxbc_path, "-S", "-V", "-a", str(patched)], check=True)

        shdr = patched.with_suffix(".shdr")
        assert shdr.exists(), "SHDR not generated"

        dxbc_patched = shdr.read_bytes()

    assert len(dxbc_patched) == total_size, f"Patched shader size is invalid ({len(dxbc_patched)} != {total_size})"

    data = (
        data[:start]
        + dxbc_patched
        + data[start + total_size:]
    )

    print(f"DXBC @ 0x{start:X}, size=0x{total_size:X}")
    print("Hash:", dxbc[4:20].hex())

    pos = start + total_size

assert start_len == len(data), "Output data size is invalid"
assert data.count(LOAD_BYTES) == 0, "Not all matches replaced"

OUTPUT.write_bytes(data)
