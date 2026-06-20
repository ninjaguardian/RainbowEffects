import re
import struct
import subprocess
import tempfile
from pathlib import Path
from common.common import DIR, COMMON

INPUT = DIR / "decompressed.bin"
OUTPUT = DIR / "decompressed_mod.bin"
DISASSEMBLE_EXE = COMMON / "HLSLDecompiler.exe"

EXTRA_NOPS = 13
EXTRA_NOPS2 = 18
NOPS2_TRIGGER = "SV_RenderTargetArrayIndex"
LOAD_BYTES = bytes.fromhex("".join([
    "36000008f22010000000000002400000ffffffff000000000000000000000000",  # mov o0.xyzw, l(-1,0,0,0)
    "36000008f2201000020000000240000000000000000000000000000000000000",  # mov o2.xyzw, l( 0,0,0,0)
    "3600000872201000030000000240000000000000000000000000000000000000",  # mov o3.xyz,  l( 0,0,0,0)
    "3600000832201000010000000240000000000000000000000000000000000000"   # mov o1.xy,   l( 0,0,0,0)
]))
TARGET_BUFFER_IDX = 1


def inc_dcl_temps(code: str) -> tuple[str, int]:
    new_r = None

    def repl(match: re.Match) -> str:
        nonlocal new_r
        new_r = int(match.group(1))
        return f"dcl_temps {new_r + 1}"

    new_code = re.sub(r"\bdcl_temps\s+(\d+)\b", repl, code)

    assert new_r is not None, "No dcl_temps found"
    return new_code, new_r


def replace_if_block(code: str) -> str:
    lines = code.splitlines(keepends=True)

    start_i = None
    end_i = None
    for i, line in enumerate(lines):
        if start_i is None and line.startswith("if_nz"):
            start_i = i + 1
            continue

        if start_i is not None and "endif" in line:
            end_i = i + 1
            break

    assert start_i is not None and end_i is not None, "Match failed"

    insert_rel = None
    mov_line = None
    for j, line in enumerate(lines[end_i:], start=0):
        if line.startswith("if_nz"):
            insert_rel = j
            m = re.match(r"if_nz\s+([^\s,]+)", line)
            assert m is not None, "Could not parse destination from if line"
            dst = m.group(1)
            mov_line = f"  mov {dst}, l(1)\n"
            break

    assert insert_rel is not None and mov_line is not None, "No if_nz found"

    lines[start_i:end_i] = [mov_line, "else\n"]

    insert_i = start_i + 2 + insert_rel
    lines.insert(insert_i, "endif\n\n")

    return "".join(lines)


def inject_color(code: str, temp_var: int) -> str:
    lines = code.splitlines(keepends=True)

    target_i = None
    color_var = None
    for i, line in enumerate(lines):
        m = re.search(r"mul\s+o2\.xyz,\s*r(\d+)\.[xyzw]{4},\s*r(\d+)\.[xyzw]{4}", line)
        if m:
            target_i = i
            color_var = int(m.group(2))
            break

    assert target_i is not None and color_var is not None

    m = re.search(r"imad\s+r\d+\.[xyzw],\s*l\(18\),\s*cb\d+\[[^\]]+\]\.[xyzw],\s*cb(\d+)\[0\]\.[xyzw]", code)
    assert m is not None

    buffer_index = int(m.group(1))

    buffer = f"cb{buffer_index}[{TARGET_BUFFER_IDX}]"

    lines[target_i:target_i] = [
        f"add r{temp_var}.xyz, l(1.0,1.0,1.0), -{buffer}.xyz\n",
        f"mad r{color_var}.xyz, r{color_var}.zzzz, r{temp_var}.xyzx, {buffer}.xyzx\n",
    ]

    code = "".join(lines)

    m = re.search(rf"\bdcl_constantbuffer\s+CB{buffer_index}\[(\d+)\],\s*immediateIndexed\b", code)
    assert m is not None

    current_size = int(m.group(1))
    required_size = TARGET_BUFFER_IDX + 1

    if current_size < required_size:
        code = (
            code[:m.start(1)]
            + str(required_size)
            + code[m.end(1):]
        )
        print(f"Using buffer cb{buffer_index}[{current_size}] -> cb{buffer_index}[{required_size}]")
    else:
        print(f"Using buffer cb{buffer_index}[{current_size}]")

    return code


data = Path(INPUT).read_bytes()
start_len = len(data)
pos = 0
replaced = 0
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

        shdr_code, tmp_var = inc_dcl_temps(replace_if_block(asm.read_text()))
        shdr_code = inject_color(shdr_code, tmp_var)

        shdr_code += "nop\n" * (
            EXTRA_NOPS2
            if NOPS2_TRIGGER in shdr_code
            else EXTRA_NOPS
        )

        patched.write_text(shdr_code)

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
    replaced += 1

assert start_len == len(data), "Output data size is invalid"
assert data.count(LOAD_BYTES) == replaced, "Not all matches replaced"

Path(OUTPUT).write_bytes(data)
