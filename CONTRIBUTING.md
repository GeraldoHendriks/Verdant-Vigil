# Contributing

Contributions are welcome. By contributing, you agree that your work may be
distributed under the [MIT License](LICENSE).

This is a human-first project. The workflow below applies to every contributor
and is designed for developers working directly in the repository. Automation
and coding agents must follow it too; their additional restrictions are in
[AGENTS.md](AGENTS.md).

## Non-Negotiable Rules

- Do not push directly to `main`.
- Do not rewrite published history with force pushes, rebases, or amended commits.
- Do not modify or discard another contributor's work without their explicit approval.
- Keep each branch and pull request focused on one coherent change.
- Do not commit generated `bin/`, `obj/`, or `Releases/` output.
- Keep new content original and compatible with Vintage Story 1.22.3.
- Do not add third-party character names, symbols, artwork, dialogue, or lore.
- Keep multiplayer-affecting gameplay authoritative on the server.

## Branch Workflow

1. Start from an up-to-date local `main` branch.
2. Create a descriptive branch using one of these prefixes:
   - `feature/` for player-facing additions.
   - `fix/` for behavior corrections.
   - `docs/` for documentation-only changes.
   - `chore/` for tooling, maintenance, or repository changes.
3. Make small, reviewable commits with imperative messages, such as `Add vessel recall cooldown`.
4. Rebase or merge the current `main` into your branch before opening a pull request if it has moved.
5. Push the branch and open a pull request into `main`.
6. Do not merge your own pull request unless repository maintainers have explicitly allowed it.

## Pull Requests

Every pull request must include:

- A concise description of the behavior change.
- The player-facing impact and any balance changes.
- Files or systems intentionally left unchanged when that context is useful.
- Verification performed, including the exact build command.
- In-game test notes for gameplay, animation, rendering, networking, or asset changes.

Reviewers should prioritize regressions, multiplayer authority, asset compatibility,
and whether the implementation is the smallest correct change.

## Development And Verification

1. Set `VINTAGE_STORY` to the Vintage Story installation directory.
2. Make the smallest focused change that solves the issue.
3. Run `./build.sh` to validate JSON, build the mod, and package a release.
4. Test affected gameplay in-game. Server startup alone does not validate client rendering, hotkeys, or animation behavior.
5. Check `git diff` and `git status` before committing. Stage only intended files.

## Assets And Localization

- Add English strings to `verdantvigil/assets/verdantvigil/lang/en.json` for all player-facing text.
- Keep asset paths in the `verdantvigil` domain.
- Verify all JSON asset changes with the build task.

## Reporting Issues

Include the Vintage Story version, mod version, reproduction steps, expected
behavior, actual behavior, and relevant client or server log excerpts.
