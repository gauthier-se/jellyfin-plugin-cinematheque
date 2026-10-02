# Security policy

## Supported versions

Only the latest release receives fixes.

## Reporting a vulnerability

Please do not open a public issue. Use GitHub's
[private vulnerability reporting](https://github.com/gauthier-se/jellyfin-plugin-cinematheque/security/advisories/new)
instead, with a description of the issue and the steps to reproduce it. You should get an answer
within a week.

## Scope

The plugin exposes read-only endpoints under `/Cinematheque`. Every data endpoint requires an
authenticated user and only returns films that user can access. The web assets under
`/Cinematheque/Web` are public and contain no data.
