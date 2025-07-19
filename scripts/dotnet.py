from argparse import ArgumentParser
from collections import deque
from collections.abc import Iterable, Sequence, Set
import json
from pathlib import Path
import platform
import shutil
from subprocess import run
from urllib.parse import urlencode
from urllib.request import urlopen
import xml.etree.ElementTree as ET


class Nuget:
    def __init__(self) -> None:
        self.search_url = self.get_search_url("SearchQueryService/3.5.0")

    def get_search_url(self, resource_type: str) -> str:
        with urlopen("https://api.nuget.org/v3/index.json") as r:
            payload = json.load(r)
            for resource in payload["resources"]:
                if resource["@type"] == resource_type:
                    return resource["@id"]
            raise ValueError("Could not find nuget search url!")

    def get_latest_version(self, package_name: str) -> str:
        query = {"q": package_name}
        with urlopen(f"{self.search_url}?{urlencode(query)}") as r:
            payload = json.load(r)
            for package_data in payload["data"]:
                if package_data["id"] == package_name:
                    return package_data["version"]
            return ""


class Project:
    def __init__(self, csproj_file: Path, framework: str) -> None:
        self.csproj_file = csproj_file
        self.name = csproj_file.stem
        self.directory = csproj_file.parent

        csproj = ET.parse(csproj_file)
        output_type_element = csproj.find("./PropertyGroup/OutputType")
        imports_executables_props = False
        for import_element in csproj.findall("./Import"):
            if import_element.attrib["Project"] == "$(Props_Executable)":
                imports_executables_props = True
        exe_output_type = (output_type_element is not None and
                           output_type_element.text == "Exe")
        self.is_executable = imports_executables_props or exe_output_type

        self.is_test = self.name.endswith(".Tests")

        self.framework = framework
        target_framework_element = csproj.find(
            "./PropertyGroup/TargetFramework"
        )
        if target_framework_element is not None:
            if target_framework_element.text is not None:
                self.framework = target_framework_element.text

        self.referenced_project_names = [
            ".".join(element.attrib["Include"][2:-1].split("_")[1:])
            for element in csproj.findall(".//ProjectReference")
        ]

        self.dependencies: list[Project] = []


class Dotnet:
    def __init__(self, projects_directory: Path) -> None:
        self.projects_directory = projects_directory

        self.supported_systems = [
            "linux",
            "windows",
            "macos",
        ]

        current_system = platform.system().lower()
        if current_system == "darwin":
            current_system = "macos"
        if current_system == "linux":
            runtime = "linux-x64"
        elif current_system == "windows":
            runtime = "win-x64"
        elif current_system == "macos":
            runtime = "osx-x64"
        else:
            raise RuntimeError("Invalid system")

        self.current_system = current_system
        self.runtime = runtime
        self.framework = self.get_framework()
        self.configuration = "Release"

        self.bin_directory = Path(projects_directory.parent, "bin")
        self.test_results_directory_name = "TestResults"

        self.nuget = Nuget()

        self.projects_map = {
            project.name: project
            for project in self.get_projects_without_dependencies()
        }
        for project in self.projects_map.values():
            dependency_names: set[str] = set()
            search: deque[str] = deque()
            search.append(project.name)
            while search:
                current_project_name = search.popleft()
                current_project = self.projects_map[current_project_name]
                dependency_names.add(current_project.name)
                if not current_project.is_test:
                    test_project_name = f"{current_project_name}.Tests"
                    if test_project_name in self.projects_map:
                        dependency_names.add(test_project_name)
                        test_project = self.projects_map[test_project_name]
                        search.extend(
                            set(
                                test_project.referenced_project_names
                            ) - dependency_names
                        )
                search.extend(
                    set(
                        current_project.referenced_project_names
                    ) - dependency_names
                )
            project.dependencies = sorted(
                [self.projects_map[name] for name in dependency_names],
                key=lambda p: p.name
            )
        self.projects = sorted(
            self.projects_map.values(),
            key=lambda p: p.name
        )

    def get_framework(self) -> str:
        build_props_file = Path(
            self.projects_directory,
            "Directory.Build.props"
        )
        build_props = ET.parse(build_props_file)
        element = build_props.find("./PropertyGroup/TargetFramework")
        target_framework = None if element is None else element.text
        if element is None or target_framework is None:
            raise RuntimeError("Could not determine dotnet version!")
        return target_framework

    def get_projects_without_dependencies(
        self,
        directory: Path | None = None
    ) -> Iterable[Project]:
        if directory is None:
            directory = self.projects_directory
        for path in directory.iterdir():
            if path.is_file() and path.suffix == ".csproj" and path.stem:
                yield Project(path, self.framework)
            elif path.is_dir():
                yield from self.get_projects_without_dependencies(path)

    def get_all_dependencies(
        self,
        project_names: Set[str]
    ) -> Sequence[Project]:
        if not project_names:
            return self.projects
        project_names_lower = {
            project_name.lower(): project_name
            for project_name in self.projects_map.keys()
        }
        projects_set: set[Project] = set()
        for project_name in project_names:
            if not project_name.lower() in project_names_lower:
                continue
            for dependency in self.projects_map[
                project_names_lower[project_name.lower()]
            ].dependencies:
                projects_set.add(dependency)
        return sorted(projects_set, key=lambda p: p.name)

    def clear_sln(self, path: Path | None = None) -> None:
        if path is None:
            path = self.projects_directory
        if path.is_file():
            if path.suffix == ".sln":
                path.unlink()
        elif path.is_dir():
            for sub_path in path.iterdir():
                self.clear_sln(sub_path)

    def sln_mono(self) -> None:
        self.clear_sln(Path(self.projects_directory, "Monorepo.sln"))
        run([
            "dotnet", "new", "sln",
            "--name", "Monorepo",
        ], cwd=self.projects_directory)
        for project in self.projects:
            run([
                "dotnet", "sln", "Monorepo.sln",
                "add", project.csproj_file.as_posix(),
            ], cwd=self.projects_directory)

    def sln(self, projects: Sequence[Project]) -> None:
        self.clear_sln()
        for project in projects:
            if project.is_test:
                continue
            run([
                "dotnet", "new", "sln",
                "--name", project.name,
            ], cwd=project.directory)
            for dependency in project.dependencies:
                run([
                    "dotnet", "sln", f"{project.name}.sln",
                    "add", dependency.csproj_file.as_posix(),
                ], cwd=project.directory)

    def update(self) -> None:
        packages_props_file = Path(
            self.projects_directory,
            "Directory.Packages.props"
        )
        packages_props = ET.parse(packages_props_file)
        packages = packages_props.findall(".//PackageVersion")
        for package in packages:
            package_name = package.attrib["Include"]
            current_version = package.attrib["Version"]
            latest_version = self.nuget.get_latest_version(package_name)
            if not latest_version:
                print(f"{package_name}: Could not find version!")
                continue
            if latest_version != current_version:
                print(f"{package_name}: {current_version} -> {latest_version}")
                package.attrib["Version"] = latest_version
        packages_props.write(packages_props_file)

    def zero(self, projects: Sequence[Project]) -> None:
        for project in projects:
            directories_to_delete = [
                Path(self.projects_directory, "artifacts", "bin"),
                Path(self.projects_directory, "artifacts", "obj"),
                Path(self.projects_directory, "artifacts", "publish"),
                Path(project.directory, self.test_results_directory_name),
            ]
            for directory_to_delete in directories_to_delete:
                shutil.rmtree(directory_to_delete, ignore_errors=True)

    def restore(self, projects: Sequence[Project]) -> None:
        for project in projects:
            run([
                "dotnet", "restore",
                project.csproj_file.name,
            ], cwd=project.directory)

    def clean(self, projects: Sequence[Project]) -> None:
        for project in projects:
            run([
                "dotnet", "clean",
                project.csproj_file.name,
                "--configuration", self.configuration,
            ], cwd=project.directory)

    def build(self, projects: Sequence[Project]) -> None:
        for project in projects:
            run([
                "dotnet", "build",
                project.csproj_file.name,
                "--configuration", self.configuration,
                "--no-restore",
            ], cwd=project.directory)

    def test(self, projects: Sequence[Project]) -> None:
        for project in projects:
            if not project.is_test:
                continue
            run([
                "dotnet", "test",
                project.csproj_file.name,
                "--configuration", self.configuration,
                "--no-build",
                "--logger", "trx",
                "--logger", "html",
            ], cwd=project.directory)

            test_results_directory = Path(
                project.directory,
                self.test_results_directory_name
            )
            result_files = [
                path
                for path in test_results_directory.iterdir()
                if path.is_file()
            ]
            for result_file in result_files:
                result_file.replace(
                    result_file.with_stem(
                        f"{project.name}.{self.current_system}"
                    )
                )

    def publish(self, projects: Sequence[Project]) -> None:
        for project in projects:
            if not project.is_executable:
                continue
            run([
                "dotnet", "publish",
                project.csproj_file.name,
                "--configuration", self.configuration,
                "--no-build",
            ], cwd=project.directory)
            self.bin_directory.mkdir(exist_ok=True)
            executable_file = Path(self.bin_directory, project.name)
            if self.current_system == "windows":
                executable_file = executable_file.with_suffix(".exe")
            original_executable_file = Path(
                self.projects_directory,
                "artifacts",
                "publish",
                project.name,
                f"{self.configuration.lower()}_{self.runtime}",
                executable_file.name
            )
            shutil.copy(original_executable_file, executable_file)


def main() -> None:
    argparser = ArgumentParser()
    argparser.add_argument("--sln-mono", action="store_true")
    argparser.add_argument("-s", "--sln", action="store_true")
    argparser.add_argument("-u", "--update", action="store_true")
    argparser.add_argument("-z", "--zero", action="store_true")
    argparser.add_argument("-r", "--restore", action="store_true")
    argparser.add_argument("-c", "--clean", action="store_true")
    argparser.add_argument("-b", "--build", action="store_true")
    argparser.add_argument("-t", "--test", action="store_true")
    argparser.add_argument("-p", "--publish", action="store_true")
    argparser.add_argument("projects", nargs="*")
    args = argparser.parse_args()
    projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
    dotnet = Dotnet(projects_directory)
    projects = dotnet.get_all_dependencies(set(args.projects))
    if args.sln_mono:
        dotnet.sln_mono()
    if args.sln:
        dotnet.sln(projects)
    if args.update:
        dotnet.update()
    if args.zero:
        dotnet.zero(projects)
    if args.restore:
        dotnet.restore(projects)
    if args.clean:
        dotnet.clean(projects)
    if args.build:
        dotnet.build(projects)
    if args.test:
        dotnet.test(projects)
    if args.publish:
        dotnet.publish(projects)


if __name__ == "__main__":
    main()
