# Security and privacy

PrintMitra is designed for offline processing of potentially sensitive documents.

## Data handling

- No telemetry, advertising, cloud API, account or remote logging is included.
- Selected originals remain in their original locations and are never overwritten.
- Corrected working images use a randomly named session folder under the Windows temporary directory.
- The session folder is removed when the application closes; stale folders older than 12 hours are retried at the next start.
- Calibration files contain only printer name, scale factors and update time.

## Reporting a vulnerability

Do not attach real Aadhaar, PAN or other identity documents to a public issue. Report the software steps using synthetic sample images only.

## Release requirements

A production release must be code-signed, built by the Windows CI workflow and tested with synthetic identity-card samples before distribution.
