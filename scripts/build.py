from pathlib import Path
from subprocess import run

projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
for path in projects_directory.iterdir():
    if not path.is_dir():
        continue
    project_name = path.name
    csproj_file = Path(path, f"{project_name}.csproj")
    if not csproj_file.is_file():
        continue
    run([
        "dotnet", "clean",
        "--configuration", "Release"
    ], cwd=path)
    run([
        "dotnet", "build",
        "--configuration", "Release"
    ], cwd=path)
