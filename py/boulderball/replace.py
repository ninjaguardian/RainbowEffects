import re
import struct
import tempfile
import subprocess
from pathlib import Path
from common.common import DIR, DISASSEMBLE_EXE

INPUT = DIR / "decompressed.bin"
OUTPUT = DIR / "decompressed_mod.bin"

LOAD = "l(1.000000,0.830770075,0.212230787,1.000000)"
EXTRA_NOPS = 2
LOAD_BYTES = bytes.fromhex("024000000000803f59ad543f0753593e0000803f")
TARGET_BUFFER_IDX = 0


def get_buffer(shdr_code: str) -> tuple[str, str]:
    m = re.search(r"iadd\s+r\d+\.[xyzw],\s*r\d+\.[xyzw],\s*cb(\d+)\[0\]\.z", shdr_code)
    assert m is not None

    buffer_index = int(m.group(1))

    m = re.search(rf"\bdcl_constantbuffer\s+CB{buffer_index}\[(\d+)\],\s*immediateIndexed\b", shdr_code)
    assert m is not None

    current_size = int(m.group(1))
    required_size = TARGET_BUFFER_IDX + 1

    buffer = f"cb{buffer_index}[{TARGET_BUFFER_IDX}]"

    if current_size >= required_size:
        print(f"Using buffer cb{buffer_index}[{current_size}]")
        return shdr_code, buffer

    shdr_code = (
        shdr_code[:m.start(1)]
        + str(required_size)
        + shdr_code[m.end(1):]
    )
    print(f"Using buffer cb{buffer_index}[{current_size}] -> cb{buffer_index}[{required_size}]")

    return shdr_code, buffer


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

        code = asm.read_text()
        # print(code)
        code, buffer_code = get_buffer(code)
        code = code.replace(LOAD, buffer_code)
        code += "nop\n" * EXTRA_NOPS
        # print(code)
        # code = code.replace(LOAD, "l(0.000000,1.000000,1.000000,1.000000)")
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
