import lz4.block
from .common import DIR


def main():
    with open(DIR / "decompressed_mod.bin", "rb") as f:
        raw = f.read()

    print("Decompressed len:", len(raw))

    compressed: bytes = lz4.block.compress(
        raw,
        mode="high_compression"
    )[4:]

    print("Compressed len:", len(compressed))

    with open(DIR / "compressed_mod.bin", "wb") as f:
        f.write(compressed)
