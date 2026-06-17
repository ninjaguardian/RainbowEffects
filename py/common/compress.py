from pathlib import Path
import lz4.block


def main(path: Path):
    with open(path / "decompressed_mod.bin", "rb") as f:
        raw = f.read()

    print("Decompressed len:", len(raw))

    compressed: bytes = lz4.block.compress(
        raw,
        mode="high_compression"
    )[4:]

    print("Compressed len:", len(compressed))

    with open(path / "compressed_mod.bin", "wb") as f:
        f.write(compressed)
