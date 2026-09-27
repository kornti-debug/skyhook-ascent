# Design and Git Workflow

This project moves forward in small, testable slices. Brainstorming is welcome,
but an idea is not an implementation commitment until we agree to it.

## From idea to accepted design

1. Discuss possibilities freely. Nothing from a brainstorming conversation is
   committed or pushed just because it was discussed.
2. When a direction is chosen, record the decision and its scope in
   `docs/game-design.md`; keep unchosen alternatives explicitly labelled as
   proposed or parked in `docs/post-submission-roadmap.md`.
3. Before implementation, name the player-facing goal and a short acceptance
   check so the slice has a clear finish line.

Unaccepted notes can remain local or be discarded. A proposal may be committed
only when it is intentionally being kept as a useful project decision record,
not as a transcript of every idea.

## Implement and verify a slice

1. Implement one coherent feature or fix. Keep its code, Unity assets, and
   relevant documentation together; avoid mixing unrelated cleanup or settings
   changes into the same change set.
2. Let Unity finish importing and compiling. Check the Console for project
   errors, run relevant EditMode tests, and use fixed seeds for procedural
   checks.
3. Play-test the real game loop for changes that affect feel, readability, or
   traversal. Record any known limitation or unfinished balance question.
4. Review the complete diff. Confirm the names, scene references, and docs agree;
   exclude generated builds, caches, temporary captures, secrets, and unrelated
   local edits.

## Commit and push checkpoints

- Commit after a coherent slice passes its relevant checks and its diff has
  been reviewed. Use a short message describing the completed outcome. A
  documentation change that captures an accepted design decision can be
  committed with the feature it describes.
- Do not commit or push tentative brainstorming, unreviewed implementation,
  or every small intermediate edit. Keep those local until they form a useful
  checkpoint.
- Push after a verified commit when the checkpoint is ready to share or back
  up. A push publishes all commits on the branch, so review the branch and
  working tree first. For this project, the normal rhythm is one commit and
  push per tested milestone, not per conversation or individual edit.
- If a test or play-test reveals a problem, fix and re-verify the same slice
  before calling it done. If the fix is substantial, make it a clearly named
  follow-up checkpoint.

## Current checkpoint

The safe-versus-fast route fork and its mirrored variant form one tested
content milestone. This checkpoint includes the prefabs, generator scene
references, current design notes, and this workflow. The straight-looking
alternative remains parked and is not part of the generated pool.
