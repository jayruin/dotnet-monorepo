from argparse import ArgumentParser
from pathlib import Path
import platform
import shutil
from subprocess import run
import xml.etree.ElementTree as ET

argparser = ArgumentParser()
argparser.add_argument("--github", action="store_true")
args = argparser.parse_args()
projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
bin_directory = Path(projects_directory.parent, "bin")
bin_directory.mkdir(exist_ok=True)
current_system = platform.system().lower()
if current_system == "darwin":
    current_system = "macos"
if current_system == "linux":
    rid = "linux-x64"
elif current_system == "windows":
    rid = "win-x64"
elif current_system == "macos":
    rid = "osx-x64"
else:
    rid = ""
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
        "--no-build",
        "--runtime", rid
    ], cwd=projects_directory)
    executable_file = Path(bin_directory, project_name)
    if current_system == "windows":
        executable_file = executable_file.with_suffix(".exe")
    original_executable_file = Path(
        path,
        "bin",
        "Release"
    )
    original_executable_file = Path(
        original_executable_file,
        [
            version_path
            for version_path in original_executable_file.iterdir()
            if version_path.is_dir()
        ][0],
        rid,
        "publish",
        executable_file.name
    )
    shutil.copy(original_executable_file, executable_file)
    executable_file = executable_file.replace(
        executable_file.with_stem(f"{project_name}-{current_system}")
    )
    if args.github:
        run([
            "gh", "release", "upload",
            project_name, executable_file.as_posix()
        ])
