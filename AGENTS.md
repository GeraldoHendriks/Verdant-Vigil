# Agent Instructions

Human contributors are first-class maintainers of this repository. Follow
[CONTRIBUTING.md](CONTRIBUTING.md) as the authoritative workflow; this file
only adds requirements specific to automated agents.

## Required Behavior

- Read `CONTRIBUTING.md`, the relevant source, and the current Git state before editing.
- Work only on the task requested by the human collaborator.
- Preserve all unrelated uncommitted changes, including changes made by people or other agents.
- Prefer the smallest correct implementation and keep changes reviewable.
- Explain material tradeoffs, assumptions, test gaps, and blockers clearly.
- Run the relevant build or validation command after making code or asset changes.
- Do not claim client-side behavior was tested unless it was actually tested in-game.

## Git Restrictions

- Do not commit, push, create pull requests, merge, rebase, amend, force-push, or alter Git configuration unless a human explicitly requests that action.
- Before a requested commit, inspect `git status`, `git diff`, and recent commit history.
- Stage only files required for the requested work.
- Never use destructive Git commands such as `git reset --hard` or `git checkout --` unless a human explicitly requests them.

## Boundaries

- Do not add dependencies, telemetry, external services, generated binaries, or release artifacts without explicit approval.
- Do not weaken multiplayer server authority for convenience.
- Do not add non-original third-party content or protected setting material.
