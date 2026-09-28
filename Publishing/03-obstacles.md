# 4. Obstacles and complications

Ordered by how likely each is to stop or slow the launch.

## A. Could block a listing

1. **Reviewer may judge it "found content".** The Unity Asset Store Content Transparency policy prohibits "content ... lacking original value beyond found content products", and most 3D assets here are CC0 downloads. What is original: the C# lesson/IK/camera/UI systems, the FBX/URP conversion, scene design and seating, the documentation and editor tools. *Mitigation:* lead the listing with the interactive teaching demo and the tools, keep the description honest, price it as a template rather than an art pack. *Residual risk: real; a rejection is possible and cannot be ruled out here.*
2. **AI disclosure is mandatory and strictly enforced.** Unity requires an "AI description" for AI-assisted content (two-strike policy); Fab requires the "Created with AI" self-declaration; itch.io asset pages can be delisted for missing AI tags. The scripts and editor tools were written with an AI assistant (Claude). TurboSquid and CGTrader forbid AI content outright (already excluded). *Action:* paste the disclosure from `04-listing-copy.md`; do not write "hand-crafted/hand-made". Also note AI-written code may be only weakly protected by copyright, so exclusivity claims are weak.
3. **Official Asset Store validator not run.** Asset Store Publishing Tools needs your Unity ID and cannot run headless. The local `PackageValidator` reports 0 fail/0 warn, but the official tool may flag something else (e.g. prefab checks). *Action:* run Tools > Asset Store > Validator on `Assets/RealisticClassroom` and fix findings before upload.
4. **Publisher account, identity, tax and payouts are yours.** Unity: publisher account, tax interview (W-8BEN individual / W-8BEN-E entity / W-9), payout profile; non-US publishers may face 30% US withholding unless a treaty rate is claimed with a tax ID. Fab, itch.io, Gumroad have their own KYC/tax setup. Nothing here can be done for you.

## B. Compatibility and quality limits (must be disclosed)

5. **Tested on one Unity version only** (6000.6.1f1, URP 17.6) on macOS (Apple silicon). Not tested on Windows/Linux, older Unity 6 LTS versions, HDRP, Built-in, mobile, WebGL or XR. Declare the tested version as the minimum, or test 6000.0 LTS first. Asset Store lets buyers refund within two weeks if the package is not as advertised or incompatible.
6. **URP only.** Buyers on Built-in or HDRP get pink materials; the listing must say so prominently. (Unity 6.5+ submissions must support URP or HDRP, so this is allowed.)
7. **Characters are low-poly and flat-shaded** (about 1,800-6,600 triangles), not photorealistic, and there is no facial animation/lip-sync/audio. A title containing "Realistic" would over-promise; the listing uses "Classroom Interior Pack". Only 10 distinct characters.
8. **Heavy props for a desktop demo:** the potted plant is about 119k triangles, laptops 14k each (x10), the whole demo is about 629k triangles with 195 renderers. No LODs. Fine on desktop; not for mobile.
9. **Unity 6.6 rule: Fast Enter Play Mode** (Domain Reload disabled) must work; it was tested in a clean project (see test log in `05-checklists.md`), but re-test after any change.
10. **Input:** works with the legacy Input Manager and the Input System package (bridge assembly). Other input packages are not supported.
11. **Package dependencies:** only URP (and uGUI, which ships with Unity). Everything else is inside the package.

## C. Licensing and legal

12. **All shipped third-party content is CC0** (verified per asset; evidence in `Publishing/license-evidence/`). Two sets were removed because they were unsafe to sell: a CC-BY blackboard (attribution licences are rejected by the Asset Store) and the "Horror School Props" pack whose author asks people not to resell it and whose page/README show conflicting licences (CC-BY 4.0 vs CC0). *Residual risk:* CC0 dedications rely on the uploader having the right to dedicate; the sources used are established (Poly Haven, Kenney, Quaternius) but Poly Pizza, OpenGameArt items are user-uploaded.
13. **CC0 cannot be re-restricted.** A EULA can only cover the original parts (scripts, scene arrangement, docs, tools). Buyers may legally reuse the CC0 models on their own. The draft `legal/EULA.md` says this; have it reviewed before using it outside the Asset Store (which has its own EULA).
14. **Marketplace-specific exclusions:** TurboSquid, CGTrader (see 01). Do not list there.
15. **Credit is voluntary but recommended;** the notices file lists every author.
16. **Trademarks/people:** no brands or real persons; the demo names ("Prof. Ada", "Mia" ...) are fictional. The lessons are original text; the Big-O numbers were re-checked (the old prototype's "31 years" figure was wrong and is not used).

## D. Process and support

17. **Review time** on the Asset Store is typically 2-4 weeks and can involve rejection rounds; Fab reviews in parallel.
18. **Support obligation:** the Provider Agreement makes you solely responsible for support and maintenance; publish a support e-mail.
19. **Piracy:** `.unitypackage` files sold on itch.io/Gumroad can be shared; DRM is not allowed on the Asset Store.
20. **No video produced:** no video encoder is installed here. Asset Store accepts YouTube/Vimeo links; Fab accepts MP4/MOV/WEBM. Screenshots/renders are provided; record a 60-second Play-mode capture yourself.
21. **The old AR features were not carried over.** The AR Foundation packages remain in the project but the product does not use them.
22. **Sales expectations:** classroom packs compete with many free assets; there is no guarantee of sales. Typical guidance for complete environment packs is $50-$200 but that is for larger scope; see the suggested price in `04-listing-copy.md`.
