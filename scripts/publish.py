from argparse import ArgumentParser
import json
from pathlib import Path
import platform
from subprocess import run
import xml.etree.ElementTree as ET

argparser = ArgumentParser()
argparser.add_argument("--github", action="store_true")
args = argparser.parse_args()
projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
bin_directory = Path(projects_directory.parent, "bin")
bin_directory.mkdir(exist_ok=True)
for path in projects_directory.iterdir():
    if not path.is_dir():
        continue
    project_name = path.name
    csproj_file = Path(path, f"{project_name}.csproj")
    if not csproj_file.is_file():
        continue
    csproj = ET.parse(csproj_file)
    output_type = csproj.find("./PropertyGroup/OutputType")
    if output_type is None or output_type.text != "Exe":
        continue
    run([
        "dotnet", "publish", project_name,
        "--output", bin_directory.as_posix()
    ], cwd=projects_directory)
    current_system = platform.system().lower()
    if current_system == "darwin":
        current_system = "macos"
    executable_file = Path(bin_directory, project_name)
    if current_system == "windows":
        executable_file = executable_file.with_suffix(".exe")
    executable_file = executable_file.replace(
        executable_file.with_stem(f"{project_name}-{current_system}")
    )
    if args.github:
        run([
            "gh", "release", "upload",
            project_name, executable_file.as_posix()
        ])
