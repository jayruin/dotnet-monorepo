from argparse import ArgumentParser
import json
from pathlib import Path
from subprocess import run
import xml.etree.ElementTree as ET

from dotnet import Dotnet


class GitHub:
    def __init__(
        self,
        projects_directory: Path,
        badges_directory: Path
    ) -> None:
        self.projects_directory = projects_directory
        self.badges_directory = badges_directory
        self.dotnet = Dotnet(projects_directory)
        self.test_results_tag = "test-results"

    def reset_tags(self) -> None:
        tags_to_reset = [self.test_results_tag]
        for project in self.dotnet.get_projects():
            if project.is_executable:
                tags_to_reset.append(project.name)
        for tag in tags_to_reset:
            run(["git", "push", "--delete", "origin", "tag", tag,])
            run(["gh", "release", "delete", tag, "--yes",])
            run([
                "gh", "release", "create", tag, "--title", tag,
                "--notes", tag,
            ])

    def create_releases(self) -> None:
        self.badges_directory.mkdir(exist_ok=True)
        for executable_file in self.dotnet.bin_directory.iterdir():
            parts = executable_file.stem.split("-")
            tag_name = "-".join(parts[:-1])
            run([
                "gh", "release", "upload",
                tag_name, executable_file.as_posix(),
            ])
        for project in self.dotnet.get_projects():
            test_results_directory = Path(
                project.directory,
                self.dotnet.test_results_directory_name
            )
            if not test_results_directory.is_dir():
                continue
            for test_result in test_results_directory.iterdir():
                if not test_result.is_file():
                    continue
                run([
                    "gh", "release", "upload",
                    self.test_results_tag, test_result.as_posix(),
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
                            "label": " - ".join([
                                project.name,
                                self.dotnet.current_system,
                            ]),
                            "message": "passing" if passing else "failing",
                            "color": "success" if passing else "critical"
                        }
                        badges_file = Path(
                            self.badges_directory,
                            ".".join([
                                project.name,
                                "badges",
                                self.dotnet.current_system,
                                "json",
                            ])
                        )
                        with open(badges_file, "w", encoding="utf-8") as f:
                            json.dump(badges, f, indent=4)
                        run([
                            "gh", "release", "upload",
                            self.test_results_tag, badges_file,
                        ])


def main() -> None:
    argparser = ArgumentParser()
    subparsers = argparser.add_subparsers(
        title="subcommands",
        dest="subcommand"
    )
    subparsers.add_parser("reset-tags")
    subparsers.add_parser("create-releases")
    args = argparser.parse_args()
    projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
    badges_directory = Path(projects_directory.parent, "badges")
    gh = GitHub(projects_directory, badges_directory)
    if args.subcommand == "reset-tags":
        gh.reset_tags()
    if args.subcommand == "create-releases":
        gh.create_releases()


if __name__ == "__main__":
    main()
