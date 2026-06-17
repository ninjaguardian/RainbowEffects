from pathlib import Path


def main(path: Path):
    with open(path / "compressed_mod.bin", "rb") as f:
        data = f.read()

    print("Compressed len:", len(data))

    with open(path / "mod.txt", "w", encoding="utf-8") as f:
        for i, b in enumerate(data):
            f.write(f"   [{i}]\n    0 UInt8 data = {b}\n")
