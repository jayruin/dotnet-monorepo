from argparse import ArgumentParser
import codecs
from pathlib import Path


def remove_bom(path: Path, buffer_size: int) -> None:
    print(buffer_size)
    if path.is_file():
        with path.open("r+b") as f:
            data = f.read(len(codecs.BOM_UTF8))
            if data != codecs.BOM_UTF8:
                return
            size = 0
            while buffer := f.read(buffer_size):
                f.seek(size)
                f.write(buffer)
                size += len(buffer)
                f.seek(size + len(codecs.BOM_UTF8))
            f.seek(size)
            f.truncate()
    elif path.is_dir():
        for sub_path in path.iterdir():
            remove_bom(sub_path, buffer_size)


def main() -> None:
    argparser = ArgumentParser()
    argparser.add_argument("path", type=Path)
    argparser.add_argument("--buffer-size", type=int, default=4096)
    args = argparser.parse_args()
    remove_bom(args.path, args.buffer_size)


if __name__ == "__main__":
    main()
