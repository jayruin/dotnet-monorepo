from pathlib import Path
from subprocess import run
import xml.etree.ElementTree as ET

tags_to_reset = ["test-results"]
projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
for path in projects_directory.iterdir():
    if not path.is_dir():
        continue
    project_name = path.name
    csproj_file = Path(path, f"{project_name}.csproj")
    if not csproj_file.is_file():
        continue
    csproj = ET.parse(csproj_file)
    output_type = csproj.find("./PropertyGroup/OutputType")
    if output_type is not None and output_type.text == "Exe":
        tags_to_reset.append(project_name)
props = ET.parse(Path(projects_directory, "Directory.Build.props"))
element = props.find("./PropertyGroup/DotnetVersion")
dotnet_version = element.text if element is not None else ""
for tag in tags_to_reset:
    run(["git", "push", "--delete", "origin", "tag", tag])
    run(["gh", "release", "delete", tag, "--yes"])
    run([
        "gh", "release", "create", tag, "--title", tag,
        "--notes", f".NET {dotnet_version}"
    ])
