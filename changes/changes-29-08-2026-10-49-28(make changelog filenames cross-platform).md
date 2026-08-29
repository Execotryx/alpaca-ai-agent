# Make changelog filenames cross-platform

**Date:** 29 August 2026

## Scope

Correct changelog paths that used colons in their timestamp component and could not be checked out on Windows.

## Changes

- Renamed both existing changelog files from `HH:mm:ss` to `HH-mm-ss` timestamps without changing their contents.
- Updated `AGENTS.md` to require the cross-platform pattern `changes-dd-MM-yyyy-HH-mm-ss(brief sentence about changes made).md`.
- Explicitly prohibited colons and other Windows-invalid characters in changelog filenames.

## Validation

- Confirmed every path under `changes/` is colon-free.
- Confirmed the renamed files retain their original blob contents.
- No application tests were run because this change only corrects documentation filenames and repository guidance.
