import json
from pathlib import Path
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


projects_directory = Path(Path(__file__).resolve().parent.parent, "src")
props_path = Path(projects_directory, "Directory.Packages.props")
props = ET.parse(props_path)
nuget = Nuget()

packages = props.findall(".//PackageVersion")
for package in packages:
    package_name = package.attrib["Include"]
    current_version = package.attrib["Version"]
    latest_version = nuget.get_latest_version(package_name)
    if not latest_version:
        print(f"{package_name}: Could not find version!")
        continue
    if latest_version != current_version:
        print(f"{package_name}: {current_version} -> {latest_version}")
        package.attrib["Version"] = latest_version
props.write(props_path)
