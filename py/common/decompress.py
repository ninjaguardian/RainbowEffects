import lz4.block
from .common import DIR


def main(decompressed_size: int):
    with open(DIR / "compressed.bin", "rb") as f:
        data = f.read()

    print(f"Compressed len: {len(data)}")

    decompressed = lz4.block.decompress(
        data,
        uncompressed_size=decompressed_size
    )

    with open(DIR / "decompressed.bin", "wb") as f:
        f.write(decompressed)
