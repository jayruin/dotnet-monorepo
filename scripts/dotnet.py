from argparse import ArgumentParser
from collections.abc import Iterable
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
        self.is_executable = output_type_element is not None and output_type_element.text == "Exe"

        self.is_test = self.name.endswith(".Tests")

        self.framework = framework
        target_framework_element = csproj.find("./PropertyGroup/TargetFramework")
        if target_framework_element is not None:
            if target_framework_element.text is not None:
                self.framework = target_framework_element.text


class Dotnet:
    def __init__(self, projects_directory: Path) -> None:
        self.projects_directory = projects_directory

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

        self.nuget = Nuget()

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

    def get_projects(self, directory: Path | None = None) -> Iterable[Project]:
        if directory is None:
            directory = self.projects_directory
        for path in directory.iterdir():
            if path.is_file() and path.suffix == ".csproj" and path.stem:
                yield Project(path, self.framework)
            elif path.is_dir():
                yield from self.get_projects(path)

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

    def zero(self) -> None:
        for project in self.get_projects():
            directories_to_delete = [
                Path(project.directory, "bin"),
                Path(project.directory, "obj"),
                Path(project.directory, "TestResults"),
            ]
            for directory_to_delete in directories_to_delete:
                shutil.rmtree(directory_to_delete, ignore_errors=True)

    def restore(self) -> None:
        for project in self.get_projects():
            run([
                "dotnet", "restore",
                project.csproj_file.name,
                "--runtime", self.runtime,
            ], cwd=project.directory)

    def clean(self) -> None:
        for project in self.get_projects():
            run([
                "dotnet", "clean",
                project.csproj_file.name,
                "--configuration", self.configuration,
            ], cwd=project.directory)

    def build(self) -> None:
        for project in self.get_projects():
            run([
                "dotnet", "build",
                project.csproj_file.name,
                "--configuration", self.configuration,
                "--no-restore",
                "--runtime", self.runtime,
                "--self-contained", "true",
            ], cwd=project.directory)

    def test(self) -> None:
        for project in self.get_projects():
            if not project.is_test:
                continue
            run([
                "dotnet", "test",
                project.csproj_file.name,
                "--configuration", self.configuration,
                "--no-build",
                "--logger", "trx",
                "--logger", "html",
                "--runtime", self.runtime,
            ], cwd=project.directory)

            test_results_directory = Path(project.directory, "TestResults")
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

    def publish(self, bin_directory: Path) -> None:
        for project in self.get_projects():
            if not project.is_executable:
                continue
            run([
                "dotnet", "publish",
                project.csproj_file.name,
                "--no-build",
                "--runtime", self.runtime,
            ], cwd=project.directory)
            bin_directory.mkdir(exist_ok=True)
            executable_file = Path(bin_directory, project.name)
            if self.current_system == "windows":
                executable_file = executable_file.with_suffix(".exe")
            original_executable_file = Path(
                project.directory,
                "bin",
                self.configuration
            )
            original_executable_file = Path(
                original_executable_file,
                project.framework,
                self.runtime,
                "publish",
                executable_file.name
            )
            shutil.copy(original_executable_file, executable_file)
            executable_file = executable_file.replace(
                executable_file.with_stem(
                    f"{project.directory.name}-{self.current_system}"
                )
            )


def main() -> None:
    argparser = ArgumentParser()
    argparser.add_argument("-u", "--update", action="store_true")
    argparser.add_argument("-z", "--zero", action="store_true")
    argparser.add_argument("-r", "--restore", action="store_true")
    argparser.add_argument("-c", "--clean", action="store_true")
    argparser.add_argument("-b", "--build", action="store_true")
    argparser.add_argument("-t", "--test", action="store_true")
    argparser.add_argument("-p", "--publish", action="store_true")
    args = argparser.parse_args()
    projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
    bin_directory = Path(projects_directory.parent, "bin")
    dotnet = Dotnet(projects_directory)
    if args.update:
        dotnet.update()
    if args.zero:
        dotnet.zero()
    if args.restore:
        dotnet.restore()
    if args.clean:
        dotnet.clean()
    if args.build:
        dotnet.build()
    if args.test:
        dotnet.test()
    if args.publish:
        dotnet.publish(bin_directory)


if __name__ == "__main__":
    main()
