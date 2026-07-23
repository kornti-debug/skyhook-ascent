# MCP for Unity Setup

Status: Connected and Verified

## Completed

- `com.coplaydev.unity-mcp` is pinned to `v10.1.0` in `Packages/manifest.json`.
- Unity resolved it into `Packages/packages-lock.json`.
- Unity `6000.3.6f1` imported and compiled the package successfully in batch mode.
- The MCP package remains tooling; game code must not reference its APIs.
- Unity reports one connected `final_assignment` instance.
- Codex can read the live Editor state, project information, and Console.
- The verified Console check returned no errors or warnings.

## One-time interactive setup record

1. Open `final_assignment` in Unity.
2. Open **Window > MCP for Unity**.
3. Complete the dependency/setup checks shown by the package.
4. Select **Codex** in the detected client list and choose **Configure Selected** (or configure all detected clients).
5. Start the MCP server and confirm that the status reads **Connected**.
6. Restart Codex or open a fresh Codex task so the newly configured MCP server is discovered.
7. In the new task, ask Codex to inspect the Unity editor state before making changes.

Official setup reference: <https://coplaydev.github.io/unity-mcp/getting-started/install>

## Reconnection test

Use a harmless, reversible prompt:

> Read the current Unity editor state, list the open scene and root GameObjects, and report any Console errors. Do not modify anything.

If that succeeds, follow with:

> Create an empty GameObject named `MCP_Connection_Test`, verify it exists, then delete it and verify the Console is clean.

## Troubleshooting

- Keep Unity open while using Unity MCP.
- If no tools appear in Codex, restart Codex after configuring the client.
- If the package reports a missing Python/`uv` dependency, use its setup wizard rather than adding a second server manually.
- If connection fails, verify the server status in **Window > MCP for Unity** and restart Unity once.
- Do not commit user-level Codex configuration or local connection secrets into this repository.
