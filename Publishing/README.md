# Publishing kit - Classroom Interior Pack

Everything needed to sell `ARClassroom/Assets/RealisticClassroom` on five stores. Read in this order:

| File | What it is |
|---|---|
| [01-marketplaces.md](01-marketplaces.md) | Top 5 stores (Unity Asset Store, Fab, itch.io, Gumroad, Payhip), stores ruled out and why, and the Terms & Conditions check (Met / You / Risk) |
| [02-formats.md](02-formats.md) | Best deliverable per store and how it is built |
| [03-obstacles.md](03-obstacles.md) | Every obstacle and complication, most serious first |
| [04-listing-copy.md](04-listing-copy.md) | Title, descriptions, AI disclosure, tags, price suggestion |
| [05-checklists.md](05-checklists.md) | Upload steps per store and the test log |
| [legal/EULA.md](legal/EULA.md) | Draft licence for itch.io, Gumroad and Payhip (needs legal review) |

## Folders

| Path | Content |
|---|---|
| `marketing/` | Store artwork, sized per store (`raw/` is git-ignored) |
| `reports/` | `validation.md` (local pre-flight), `clean_project_play_test.png` |
| `license-evidence/` | Proof of the CC0 licence of every third-party source |
| `doc-src/`, `tools/` | Documentation source and the build scripts |
| `dist/` | Built `.unitypackage` and zip (git-ignored, rebuilt by the script) |

## Build the release

```bash
cp publisher.env.example publisher.env   # fill in your details
tools/build_release.sh                   # docs -> validate -> export -> zip
```

Close the Unity project first; the script runs Unity in batch mode. Outputs in `dist/`:

- `RealisticClassroom_v1.0.0.unitypackage` - Fab upload
- `RealisticClassroom_v1.0.0_direct-sales.zip` - itch.io, Gumroad, Payhip (package, PDF, third-party notices, EULA, readme)
- the Unity Asset Store upload does not use these files: use the Asset Store Publishing Tools on the folder `Assets/RealisticClassroom`

## What only you can do

Create the store accounts, complete tax and payout details, fill in `publisher.env`, run the official Asset Store Validator, confirm the AI-disclosure text is true for you, record an optional video, and (recommended) have the EULA reviewed by a lawyer.
