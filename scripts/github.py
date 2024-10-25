from argparse import ArgumentParser
import inspect
import math
from pathlib import Path
import shutil
from subprocess import run
from typing import cast
from urllib.request import Request, urlopen
from xml.dom.minidom import getDOMImplementation, Document, Element
import xml.etree.ElementTree as ET

from dotnet import Dotnet


class GitHub:
    def __init__(
        self,
        projects_directory: Path
    ) -> None:
        self.projects_directory = projects_directory
        self.repo_directory = self.projects_directory.parent
        self.dotnet = Dotnet(projects_directory)
        self.test_results_tag = "test-results"

    def get_tags(self) -> list[str]:
        tags = [self.test_results_tag, *self.dotnet.supported_systems]
        return tags

    def delete_tags(self) -> None:
        tags = self.get_tags()
        for tag in tags:
            run([
                "gh", "release", "delete", tag, "--yes", "--cleanup-tag",
            ], cwd=self.repo_directory)

    def create_tags(self) -> None:
        tags = self.get_tags()
        for tag in tags:
            run([
                "gh", "release", "create", tag, "--title", tag,
                "--notes", tag,
            ], cwd=self.repo_directory, check=True)

    def create_releases(self) -> None:
        current_system = self.dotnet.current_system
        if current_system == "linux":
            logo = "linux"
        elif current_system == "windows":
            logo = "microsoft"
        elif current_system == "macos":
            logo = "apple"
        else:
            raise RuntimeError("Invalid system")
        for executable_file in self.dotnet.bin_directory.iterdir():
            run([
                "gh", "release", "upload",
                current_system, executable_file.as_posix(),
            ], cwd=self.repo_directory, check=True)
        for project in self.dotnet.projects:
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
                ], cwd=self.repo_directory, check=True)
                if test_result.suffix != ".trx":
                    continue
                trx = ET.parse(test_result)
                counters = trx.find("./{*}ResultSummary/{*}Counters")
                if counters is None:
                    continue
                total = counters.attrib["total"]
                passed = counters.attrib["passed"]
                if total is None or passed is None:
                    continue
                total = int(total)
                passed = int(passed)
                passing = total == passed
                passing_percentage = (
                    100 if total == 0
                    else math.floor(100 * (passed / total))
                )
                badges_file = Path(
                    test_results_directory,
                    ".".join([
                        project.name,
                        self.dotnet.current_system,
                        "svg",
                    ])
                )
                status_color = "success" if passing else "critical"
                request = Request(
                    "/".join([
                        "https://img.shields.io",
                        "badge",
                        "-".join([
                            project.name,
                            f"{passing_percentage}%25",
                            f"{status_color}?logo={logo}",
                        ])
                    ]),
                    headers={
                        "User-Agent": "Mozilla/5.0"
                    }
                )
                with urlopen(request) as r:
                    with badges_file.open("wb") as f:
                        shutil.copyfileobj(r, f)
                run([
                    "gh", "release", "upload",
                    self.test_results_tag, badges_file.as_posix(),
                ], cwd=self.repo_directory, check=True)

    def write_index_html(self, path: Path) -> None:
        implementation = getDOMImplementation()
        if implementation is None:
            raise ValueError("No DOM Implementation!")
        document_type = implementation.createDocumentType("html", "", "")
        document = implementation.createDocument(None, "html", document_type)
        html = cast(Element, document.lastChild)
        html.setAttribute("lang", "en")
        head = html.appendChild(document.createElement("head"))
        head.appendChild(document.createElement("title")) \
            .appendChild(document.createTextNode("dotnet-monorepo"))
        meta = head.appendChild(document.createElement("meta"))
        meta.setAttribute("charset", "UTF-8")
        link = head.appendChild(document.createElement("link"))
        link.setAttribute("href", "index.css")
        link.setAttribute("rel", "stylesheet")
        body = html.appendChild(document.createElement("body"))
        body.appendChild(document.createElement("h1")) \
            .appendChild(document.createTextNode("dotnet-monorepo"))
        table = body.appendChild(document.createElement("table"))
        tr = table.appendChild(document.createElement("tr"))
        tr.appendChild(document.createElement("th")) \
            .appendChild(document.createTextNode("Project"))
        tr.appendChild(document.createElement("th")) \
            .appendChild(document.createTextNode("Tests"))
        for project in self.dotnet.projects:
            if not project.is_test:
                continue
            tr = table.appendChild(document.createElement("tr"))
            td = tr.appendChild(document.createElement("td"))
            td.setAttribute("rowspan", "3")
            td.appendChild(document.createTextNode(project.name))
            tr.appendChild(
                self.create_badge_cell(document, project.name, "linux")
            )
            table.appendChild(document.createElement("tr")) \
                .appendChild(
                    self.create_badge_cell(document, project.name, "windows")
                )
            table.appendChild(document.createElement("tr")) \
                .appendChild(
                    self.create_badge_cell(document, project.name, "macos")
                )
        path.write_bytes(document.toprettyxml(encoding="UTF-8"))

    def create_badge_cell(
        self,
        document: Document,
        project_name: str,
        system: str
    ) -> Element:
        td = document.createElement("td")
        a = td.appendChild(document.createElement("a"))
        a.setAttribute("href", f"{project_name}.{system}.html")
        img = a.appendChild(document.createElement("img"))
        img.setAttribute("src", f"{project_name}.{system}.svg")
        img.setAttribute("alt", f"{project_name} {system} badge")
        return td

    def write_index_css(self, path: Path) -> None:
        text = """
            table {
                margin: auto;
            }
            h1 {
                text-align: center;
            }
        """
        path.write_text(inspect.cleandoc(text), encoding="utf-8")

    def update_pages(self) -> None:
        index_html_file = Path(self.projects_directory, "index.html")
        index_css_file = Path(self.projects_directory, "index.css")
        self.write_index_html(index_html_file)
        self.write_index_css(index_css_file)
        for file in [index_html_file, index_css_file]:
            run([
                "gh", "release", "upload",
                self.test_results_tag, file.as_posix(),
                "--clobber",
            ], check=True)
            file.unlink()
        commands = [
            [
                "git", "config",
                "user.email", "github-actions[bot]@users.noreply.github.com",
            ],
            ["git", "config", "user.name", "github-actions[bot]",],
            ["git", "switch", "--orphan", "gh-pages",],
            ["gh", "release", "download", "test-results",],
            ["git", "add", "*.trx", "*.html", "*.svg", "*.css",],
            ["git", "commit", "-m", "Update Pages",],
            ["git", "push", "--set-upstream", "origin", "gh-pages", "-f",],
        ]
        for command in commands:
            run(command, cwd=self.repo_directory, check=True)


def main() -> None:
    argparser = ArgumentParser()
    subparsers = argparser.add_subparsers(
        title="subcommands",
        dest="subcommand"
    )
    subparsers.add_parser("delete-tags")
    subparsers.add_parser("create-tags")
    subparsers.add_parser("create-releases")
    subparsers.add_parser("update-pages")
    args = argparser.parse_args()
    projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
    gh = GitHub(projects_directory)
    if args.subcommand == "delete-tags":
        gh.delete_tags()
    if args.subcommand == "create-tags":
        gh.create_tags()
    if args.subcommand == "create-releases":
        gh.create_releases()
    if args.subcommand == "update-pages":
        gh.update_pages()


if __name__ == "__main__":
    main()
