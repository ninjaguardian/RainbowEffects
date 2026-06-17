from pathlib import Path


def main(path: Path):
    with open(path / "raw.txt", "r", encoding="utf-8") as fin, open(path / "compressed.bin", "wb") as fout:
        for line in fin:
            fout.write(int(line).to_bytes(1, byteorder="little"))
