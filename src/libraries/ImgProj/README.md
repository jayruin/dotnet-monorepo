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

## .metadata.json (required)

Coordinates is an array of int which represents the 1-based coordinates to an entry.

PageCoordinates is similar to Coordinates except the last coordinate is used to locate the page.

Contains a single Metadata object. Specifies the metadata.

Metadata object
- versions (required): array of string
    - must be non-empty where first element is the main version
- direction (required): string
    - must be either ltr or rtl
    - represents the reading direction
- spreads (optional): array of Spread objects
    - represents page spreads
- root (optional): Entry object

Spread object (all properties required)
- left: PageCoordinates
- right: PageCoordinates

Entry object (all properties optional)
- title: object
    - key is version and value is title
- creators: object
    - key is version and value is object
    - value object maps creator names (string) to roles (array of strings)
    - each role string must be a MARC relator code
- languages: object
    - key is version and value is array of string
    - each string in value array must be a BCP-47 language code
- timestamp: object
    - key is version and value is string
    - value string must be in the ISO-8601 extended format
- cover: array of PageCoordinates
    - each PageCoordinates corresponds to a subcover
    - the cover will be a grid composed of these subcovers
- entries: array of Entry objects
    - list of sub-entries
- misc: object
    - any additional info in arbitrary format

For Entry objects, some missing optional properties may be inferred or provided defaults.

- title will default to single coordinate for that entry
- creators, languages may be inferred as the union of parent Entry and all child Entries
- timestamp may be inferred as the max of parent Entry and all child Entries
- covers will inherit the relevant subcovers of the parent Entry
