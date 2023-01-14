from pathlib import Path
from subprocess import run

projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
projects = [
    path
    for path in projects_directory.iterdir()
    if path.is_dir() and Path(path, f"{path.name}.csproj").is_file()
]
for project in projects:
    run([
        "dotnet", "clean",
        "--configuration", "Release"
    ], cwd=project)
for project in projects:
    run([
        "dotnet", "build",
        "--configuration", "Release"
    ], cwd=project)
