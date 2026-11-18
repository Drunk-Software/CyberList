# Contributing to CyberList

Fork it, break it, make it yours, and send a pull request if you build something cool.

## How PRs work

- Every PR needs a review from the maintainer before it can be merged.
- CI must pass: the build and the tests (`dotnet test CyberList.slnx`).
- The PR title decides the next version number, so start it with one of these:

  | Title starts with | Use it for                          |
  |-------------------|-------------------------------------|
  | `feat:`           | a new feature                       |
  | `!feat:`          | a breaking change (old data/behaviour no longer works) |
  | `patch:`          | fixes, tweaks, refactors, docs      |

  Example: `feat(tree): keyboard shortcuts for the selected task`

## Licensing of contributions

CyberList is licensed under the [PolyForm Noncommercial License 1.0.0](LICENSE.txt):
free to use, fork, modify and share, but not for commercial purposes.

By opening a pull request you agree that:

1. your contribution is licensed under the same license as the project, and
2. the maintainer may also distribute your contribution under other license terms in the
   future (for example, to relicense the project).

You keep the copyright to your own work.
