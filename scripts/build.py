from pathlib import Path
import platform
from subprocess import run

projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
projects = [
    path
    for path in projects_directory.iterdir()
    if path.is_dir() and Path(path, f"{path.name}.csproj").is_file()
]
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
for project in projects:
    run([
        "dotnet", "restore",
        "--runtime", rid
    ], cwd=project)
for project in projects:
    run([
        "dotnet", "clean",
        "--configuration", "Release"
    ], cwd=project)
for project in projects:
    run([
        "dotnet", "build",
        "--configuration", "Release",
        "--no-restore",
        "--runtime", rid,
        "--self-contained", "true"
    ], cwd=project)
