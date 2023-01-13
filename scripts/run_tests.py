from argparse import ArgumentParser
import json
from pathlib import Path
import platform
from subprocess import run
import xml.etree.ElementTree as ET

argparser = ArgumentParser()
argparser.add_argument("--github", action="store_true")
args = argparser.parse_args()
tags_to_reset = ["test-results"]
projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
for path in projects_directory.iterdir():
    if not path.is_dir() or not path.name.endswith(".Tests"):
        continue
    project_name = path.name
    run([
        "dotnet", "test", project_name,
        "--no-build",
        "--logger", "trx",
        "--logger", "html"
    ], cwd=projects_directory)
    test_results_directory = Path(path, "TestResults")
    result_files = [
        path
        for path in test_results_directory.iterdir()
        if path.is_file()
    ]
    for result_file in result_files:
        test_result = result_file.replace(result_file.with_stem(project_name))
        if args.github:
            run(["gh", "release", "upload", project_name, test_result])
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
                current_system = platform.system()
                badges = {
                    "schemaVersion": 1,
                    "label": f"{project_name} - {current_system}",
                    "message": "passing" if passing else "failing",
                    "color": "success" if passing else "critical"
                }
                badges_file = Path(
                    projects_directory.parent,
                    f"{project_name}.Badges.{current_system}.json"
                )
                with open(badges_file, "w", encoding="utf-8") as f:
                    json.dump(badges, f, indent=4)
                if args.github:
                    run(["gh", "release", "upload", project_name, badges_file])
