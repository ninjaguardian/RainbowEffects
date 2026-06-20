from .common import DIR


def main():
    with open(DIR / "compressed_mod.bin", "rb") as f:
        data = f.read()

    print("Compressed len:", len(data))

    with open(DIR / "mod.txt", "w", encoding="utf-8") as f:
        for i, b in enumerate(data):
            f.write(f"   [{i}]\n    0 UInt8 data = {b}\n")
