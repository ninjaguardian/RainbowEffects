def convert(s: str) -> str:
    out = []
    i = 0

    while i < len(s):
        if s[i:i + 4] == "<bh:":
            j = s.find(">", i)
            hex_val = s[i + 4:j]
            out.append(hex_val)
            i = j + 1
        else:
            out.append(f"{ord(s[i]):02x}")
            i += 1

    return "".join(out)


print(convert(input("Data in:\n>")))
