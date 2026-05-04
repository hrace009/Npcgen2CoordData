# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-05-04

Inferred SemVer bump: **minor** — new user-facing features (dynamic object
handling and batch map import) were added in a backwards-compatible way
alongside a progress-bar fix and documentation updates.

### Added
- Dynamic object data handling in `npcgen` with extended progress tracking
  ([`89ca50a`](https://github.com/hrace009/Npcgen2CoordData/commit/89ca50a44f9b94c36ee740bc71b668d102aa981b)).
- Batch import that processes multiple maps from a server folder in one run
  ([`9b73e6a`](https://github.com/hrace009/Npcgen2CoordData/commit/9b73e6a732d616c580269e9ba9de3496cf5c7f1c)).
- Miscellaneous project assets
  ([`39e7c7c`](https://github.com/hrace009/Npcgen2CoordData/commit/39e7c7ca4e665f0a043af3c2f0787b8dd00e19ee)).

### Changed
- README rewritten to document the batch map import workflow
  ([`b6c7cef`](https://github.com/hrace009/Npcgen2CoordData/commit/b6c7cefd19aae89234e49026dec326b64021091d)).

### Fixed
- `ProgressMax` is now invoked in the correct order and `ProgressValue` is
  properly initialized, preventing progress-bar glitches during long imports
  ([`14d2698`](https://github.com/hrace009/Npcgen2CoordData/commit/14d26987c2959c40a6a09513a2222908ab2780e4)).

[1.1.0]: https://github.com/hrace009/Npcgen2CoordData/compare/14d2698...89ca50a
