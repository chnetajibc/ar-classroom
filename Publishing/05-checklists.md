# 6. Upload checklists and test log

## Step 0 - once, before any store

1. `cp Publishing/publisher.env.example Publishing/publisher.env`, fill in your publisher name, support e-mail, website and jurisdiction.
2. Run `Publishing/tools/build_release.sh`. It puts your details into the PDF and the EULA, validates the package, exports the `.unitypackage` and builds the direct-sales zip in `Publishing/dist/`. (It stops if a placeholder is still there or the Unity project is open.)
3. Open `dist/RealisticClassroom_v1.0.0.unitypackage` in a fresh URP project once and press Play (5-minute smoke test).
4. Decide the minimum Unity version. Only 6000.6.1f1 is tested, so declare 6000.6. To go lower, import the package into a fresh 6000.0 LTS URP project first.
5. Have these ready: the text in `04-listing-copy.md`, the images in `marketing/`, `Third-Party Notices.txt`, your tax details, your PayPal or bank details.

## Unity Asset Store (best channel; review takes weeks)

- [ ] Publisher account at <https://publisher.unity.com> with your Unity ID: publisher name, support e-mail, website
- [ ] Tax interview (W-8BEN if you are an individual outside the US, W-9 if you are a US person) and payout profile (PayPal monthly or bank)
- [ ] Create a package draft and paste the title, descriptions, keywords, **AI description**, category, price
- [ ] Upload icon, card, cover, social image and the 12 screenshots (`marketing/unity-asset-store/`); add a YouTube link if you record a video
- [ ] In the Editor: install the **Asset Store Publishing Tools**, run **Tools > Asset Store > Validator** on `Assets/RealisticClassroom` and fix every red item
- [ ] Uploader: choose the draft and the folder `Assets/RealisticClassroom` (never `Assets/_Publishing`), upload
- [ ] Set the minimum Unity version and "URP only"; submit for review
- [ ] If rejected: read the reason in the portal, fix, resubmit (a second AI-disclosure strike is costly, so keep the disclosure accurate)

## Fab (same files as the Asset Store)

- [ ] <https://www.fab.com/sell>: sign in with your Epic account, accept the Fab agreements, complete the publisher profile, tax and payout set-up
- [ ] Create a listing: format **Unity**, render pipeline **URP**, Unity version 6000.6
- [ ] Upload `dist/RealisticClassroom_v1.0.0.unitypackage` (the same content as the Asset Store upload)
- [ ] Tick the third-party declaration and **Created with AI** (texts in `04-listing-copy.md`), Standard License, set the Personal/Professional prices
- [ ] Upload the 12 JPGs from `marketing/fab/` (each under 3 MB) and submit

## itch.io

- [ ] Account, then Dashboard > Create new project: kind **Downloadable**, classification **Assets**
- [ ] Paid, price from `04-listing-copy.md`; in Account settings choose **Collected by itch.io** so it handles VAT
- [ ] Upload `dist/RealisticClassroom_v1.0.0_direct-sales.zip`
- [ ] Fill the generative-AI disclosure (code and documentation only), tags, description, cover 630x500, screenshots
- [ ] Payout details (PayPal), save as Restricted, check the page as a buyer, then set Public

## Gumroad

- [ ] Account, payout method (PayPal or bank), identity and tax details
- [ ] New product > Digital product: name, price, cover 1280x720, thumbnail 600x600, description (limitations first)
- [ ] Upload the direct-sales zip (if Gumroad rejects `.zip` for the account, ask its support; do not rename files to bypass a check)
- [ ] Mention in the description that the licence is `EULA.md` inside the zip; publish

## Payhip

- [ ] Account; connect your own Stripe and/or PayPal (buyers pay you directly)
- [ ] Set the EU/UK VAT option; you remain responsible for US sales tax and other countries
- [ ] Add product > Digital download: upload the direct-sales zip, price, cover, description; publish

## Before pressing Submit anywhere

- [ ] The PDF and EULA contain no `[PLACEHOLDER]` text (the build script checks this)
- [ ] `Third-Party Notices.txt` is inside the package and the zip
- [ ] The AI disclosure is ticked or filled wherever the form has it
- [ ] Only your own words in the description; no claim that is not in the "Good to know" list
- [ ] You can be reached at the support address

## After launch

- [ ] Watch the support inbox; answer reviews
- [ ] Keep all stores on the same version. Fab requires the same files as the Asset Store, so update both together. Bump `CHANGELOG.md`, rebuild with `build_release.sh`
- [ ] Track income and taxes yourself (each store pays you as a self-employed seller)

---

# Test log (2026-09-28, Unity 6000.6.1f1, macOS Apple silicon)

| # | Test | Result |
|---|---|---|
| 1 | Export `Assets/RealisticClassroom` as `RealisticClassroom_v1.0.0.unitypackage` (61 MB) | OK |
| 2 | Import into a clean project that has only URP | Imported and compiled: 0 errors, 0 warnings |
| 3 | Tools > Realistic Classroom > Apply Recommended URP Settings | OK (Linear colour space, URP asset assigned) |
| 4 | Open `Classroom_Demo`, Play twice in a row with **Domain Reload disabled** (Fast Enter Play Mode) | Lesson ran both times; 0 errors, 0 warnings (screenshot: `reports/clean_project_play_test.png`) |
| 5 | Switch Active Input Handling to **Input System Package (New)** only | The Input System provider registered, lesson played, 0 errors, 0 warnings |
| 6 | Local pre-flight validator (`reports/validation.md`) | 0 FAIL, 0 WARN |
| 7 | Official Asset Store Validator | **Not run** (needs your Unity ID) |
| 8 | Windows, Linux, older Unity 6 versions, HDRP, Built-in, mobile, WebGL, XR | **Not tested** |
