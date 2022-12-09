# Img Project

## Purpose And Overview

This project type is designed around the key assumption that each page is a single image with no additional content. Example use cases include
- scanned documents
- comics
- textbooks
- magazines
- scanned books

Pages may have different versions, but there must be a main version. Versions are supposed to be flexible and example use cases include:
- Translations where main version is the original language
- Different editions of the same book where the main version is the newest edition
- Different sources (digital vs scanned physical)

Pages are grouped by the entry in which they belong. Entries can contain sub-entries and can correspond to concepts such as
- Different books in the same series
- Chapters

Supported image file extensions:
- .jpg
- .png

## Storage Structure

Recursive filesystem structure
- Entry directories are prefixed with underscore (_).
- Page directories are left 0 padded based on total number of pages (which is fixed after initial import).
- In each page directory, there must be image file for the main version. Additional version image files are optional. Each page file should be named {version}{extension} (e.g. main_version.jpg).
- The root directory may contain pages as well
- The root directory should contain additional support files

Given the difficulty of migrating between formats:
- Page storage structure should not change
- Structure of support files may change if absolutely necessary

## Support Files

Support files should be placed in the root directory, start with dot (.) and be in JSON format.

All JSON keys must be present unless otherwise noted.

Coordinates is an array of int which represents the 1-based coordinates to an entry.

PageCoordinates is similar to Coordinates except the last coordinate is used to locate the page.

### .metadata.json (required)

Contains a single Metadata object. Specifies the metadata.

Metadata object
- versions: array of string
    - must be non-empty where first element is the main version
- title: object
    - key is version and value is title
    - main version must be present
- creators: array of Creator objects
    - may be empty
- languages: object
    - key is version and value is array of string
    - each string in value array must be a BCP-47 language code
    - main version must be present and its value array must be non-empty
- timestamp (optional): object
    - key is version and value is string
    - value string must be in the ISO-8601 extended format
    - may be empty
- cover: array of PageCoordinates
    - each PageCoordinates corresponds to a subcover
    - the cover will be a grid composed of these subcovers
    - may be empty
- direction: string
    - must be either ltr or rtl
    - represents the reading direction
- misc (optional): object
    - any additional info in arbitrary format

Creator object
- name: object
    - key is version and value is name
    - main version must be present
- roles: array of string
    - each role string must be a MARC relator code
    - may be empty
- misc (optional): object
    - any additional info in arbitrary format

### .nav.json (required)

Contains an array of Entry objects. Specifies the navigation structure. Each Entry object in the array represents a sub-entry of the root entry. Thus, if the array is empty, the root entry contains no sub-entries and only contains pages.

Entry object
- title: object
    - key is version and value is title
    - main version must be present
- entries: array of Entry objects
    - list of sub-entries
- timestamp (optional): object
    - key is version and value is string
    - value string must be in the ISO-8601 extended format
    - may be empty
- misc (optional): object
    - any additional info in arbitrary format

### .spreads.json (optional)

Contains an array of Spread objects. Specifies the synthetic page spreads.

Spread object
- left: string
- right: string