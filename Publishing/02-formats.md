# 3. Best format per marketplace

All five channels sell the **same product** built from `ARClassroom/Assets/RealisticClassroom`. Only the wrapper differs.

| Marketplace | Deliverable | Why | How to build it | Artwork |
|---|---|---|---|---|
| **Unity Asset Store** | The folder `Assets/RealisticClassroom` uploaded with **Asset Store Publishing Tools** (Tools > Asset Store > Uploader), which exports the `.unitypackage` itself. Unity 6 (6000.x), URP. | The store's native format; its validator runs on the folder; mesh files must be FBX/OBJ/DAE/ABC (all shipped models are FBX); best discoverability for Unity buyers | Open `ARClassroom/` in Unity 6000.6, run the validator, then Uploader > Export and Upload | Cover 1950x1300 (or 1200x800), card 420x280, icon 160x160, social 1200x630, 12 screenshots 2400x1600 in `marketing/unity-asset-store/` (sizes vary between Unity docs: the current 1200x800 cover and older 1950x1300 cover are both provided) |
| **Fab** | The same Unity product: `dist/RealisticClassroom_v1.0.0.unitypackage` (zip it with the PDF if the form asks for `.zip`); choose the *Unity* format, render pipeline *URP* | Fab requires Unity products to follow the Asset Store guidelines and to be "approximately the same files" as on the Asset Store | `AssetDatabase.ExportPackage` (already built) | 12 gallery images 1920x1080 JPEG < 3 MB in `marketing/fab/`; optional 1920x1080 video |
| **itch.io** | One ZIP: `.unitypackage` + `Documentation.pdf` + `Third-Party Notices.txt` + `EULA.md` + `README` | itch.io sells files; buyers import the package themselves | `tools/build_release.sh` produces `dist/RealisticClassroom_v1.0.0_itch-gumroad.zip` | Cover 630x500, 6 screenshots 1920x1080 in `marketing/itch-io/` |
| **Gumroad** | The same ZIP | Same reason; Gumroad hosts and delivers the file | same | Cover 1280x720, thumbnail 600x600 in `marketing/gumroad/` |
| **Payhip** | The same ZIP | Payhip delivers any file; buyers import the package themselves | same | Cover 1280x720 and the 1920x1080 screenshots from `marketing/itch-io/` |

**Why not a static 3D scene (FBX/GLB) product?** TurboSquid/CGTrader-style model sales are excluded (see 01), and on Fab a second "static scene" listing would need its own third-party declaration and would add little over the Unity product. If you want one later, export the demo room from Unity with the FBX Exporter (already installed in the project) and list it on Fab only.

**Render pipeline coverage.** The package is URP-only. Asset Store rules (Unity 6.5+) require URP *or* HDRP; state "URP only" on every listing. The Built-in pipeline would need a separate `.unitypackage` of converted materials (not done).

**Contents of the Unity package** (`Assets/RealisticClassroom`): `Animations/` (Universal Animation Library FBX + 2 controllers), `Documentation/Documentation.pdf`, `Materials/` (126 URP/Lit), `Models/` (42 FBX), `Prefabs/` (43), `Scenes/Classroom_Demo.unity`, `Scripts/` (Runtime, InputSystemBridge, Editor - 3 asmdefs), `Settings/` (URP asset, renderer with SSAO, post-processing profile), `Textures/` (90 images), `README.md`, `CHANGELOG.md`, `Third-Party Notices.txt`. Package size: about 61 MB compressed, about 106 MB unpacked.
