from argparse import ArgumentParser
import json
from pathlib import Path
import platform
from subprocess import run
import xml.etree.ElementTree as ET

argparser = ArgumentParser()
argparser.add_argument("--github", action="store_true")
args = argparser.parse_args()
test_results_tag = "test-results"
tags_to_reset = [test_results_tag]
projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
badges_directory = Path(projects_directory.parent, "badges")
badges_directory.mkdir(exist_ok=True)
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
    if not path.is_dir() or not path.name.endswith(".Tests"):
        continue
    project_name = path.name
    run([
        "dotnet", "test", project_name,
        "--configuration", "Release",
        "--no-build",
        "--logger", "trx",
        "--logger", "html",
        "--runtime", rid
    ], cwd=projects_directory)
    test_results_directory = Path(path, "TestResults")
    result_files = [
        path
        for path in test_results_directory.iterdir()
        if path.is_file()
    ]
    for result_file in result_files:
        test_result = result_file.replace(
            result_file.with_stem(f"{project_name}.{current_system}")
        )
        if args.github:
            run([
                "gh", "release", "upload",
                test_results_tag, test_result.as_posix()
            ])
        if test_result.suffix == ".trx":
            trx = ET.parse(test_result)
            counters = trx.find("./{*}ResultSummary/{*}Counters")
            if counters is not None:
                keys_to_ignore = {"total", "executed", "passed"}
                fails = set(
                    value
                    for key, value in counters.attrib.items()
                    if key not in keys_to_ignore
                )
                passing = len(fails) == 1 and "0" in fails
                badges = {
                    "schemaVersion": 1,
                    "label": f"{project_name} - {current_system}",
                    "message": "passing" if passing else "failing",
                    "color": "success" if passing else "critical"
                }
                badges_file = Path(
                    badges_directory,
                    f"{project_name}.badges.{current_system}.json"
                )
                with open(badges_file, "w", encoding="utf-8") as f:
                    json.dump(badges, f, indent=4)
                if args.github:
                    run([
                        "gh", "release", "upload",
                        test_results_tag, badges_file
                    ])
