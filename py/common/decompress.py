from pathlib import Path
import lz4.block


def main(path: Path, decompressed_size: int):
    with open(path / "compressed.bin", "rb") as f:
        data = f.read()

    print(f"Compressed len: {len(data)}")

    decompressed = lz4.block.decompress(
        data,
        uncompressed_size=decompressed_size
    )

    with open(path / "decompressed.bin", "wb") as f:
        f.write(decompressed)
