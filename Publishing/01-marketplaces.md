# 1. Where to sell it (researched 2026-09-28)

The product is a **Unity URP package** (prefabs, FBX models, scripts, demo scene). Marketplaces that pay per purchase and accept it:

| # | Marketplace | Fit | Seller share | Payout | Notes |
|---|---|---|---|---|---|
| 1 | **Unity Asset Store** | Best fit - native `Assets/` folder upload | 70% | PayPal monthly (threshold can be $0) or bank quarterly ($250 min); USD; W-8BEN/W-9 tax interview | No listing fee, min price $4.99, review typically 2-4 weeks, Unity handles VAT/taxes on sales |
| 2 | **Fab** (Epic Games) | Very good - accepts Unity products; must be "approximately the same files" as the Asset Store | 88% | ~30 days after month end once earnings reach $100 (else rolls over) | Standard licence with Personal / Professional price tiers; prices chosen from preset steps ($x.99 up to $99.99, then $5/$10/$25/$100 steps) |
| 3 | **itch.io** | Good - open marketplace, "Game assets" category | 90% by default (you can set 0-100% to itch.io) minus payment-processor fees (~2.9% + $0.30) | Min balance $5, 7-day hold, PayPal/Payoneer | Choose "Collected by itch.io" so it acts as merchant of record and remits EU VAT; recommended minimum price $2 |
| 4 | **Gumroad** | Good - simple storefront, any zip | 90% minus $0.50/sale and card fees; Gumroad Discover sales cost 30% | Fridays; balance minimum $100 (verified sellers may get $10); PayPal payout fee 2% | Merchant of record since Jan 2025 (handles VAT/sales tax) |
| 5 | **Payhip** | Good - simple storefront, any zip, no marketplace review | 95% on the free plan (5% Payhip fee; 0% on the $99/month plan) minus Stripe/PayPal processing (~2.9% + $0.30) | Paid straight into your own Stripe/PayPal account by the buyer's payment (Payhip does not hold your money) | **Not** a merchant of record: Payhip collects and remits EU/UK VAT, but US sales tax and other countries' GST/VAT stay with you. Resale-rights bundles are banned. Its Terms of Use only require that the content does not infringe third-party IP; they do not ban AI-assisted or third-party (CC0) content |

**Deliberately excluded (they would reject or terminate the listing) - GameDev Market was on my first shortlist and was replaced by Payhip after this check:**

| Marketplace | Why not |
|---|---|
| **TurboSquid** (Shutterstock) | Publishing Agreement: you warrant the content "is your original work and contains no third-party intellectual property" and "is not the product of any AI generative content creation software". This package is assembled from third-party CC0 models and its scripts were written with AI assistance. |
| **CGTrader** | Help Center: "using free assets found online ... will result in copyright infringement and breach of our Terms" and AI-generated content is not accepted. Payout rates also fall to 55-80% on 15 Sep 2026. |
| **GameDev Market** | Announced in Aug 2025 that it is a fully generative-AI-free marketplace (sellers certify no AI use) and requires the uploader to be the full copyright holder of what they sell. Here the scripts/tools are AI-assisted and the 3D content is third-party CC0, so neither statement can be made. Its own T&C/news pages sit behind a Cloudflare bot check that I did not bypass, so this comes from the announcement as reported in search results; if you disagree, read the page yourself before applying. |
| **Sketchfab Store** | Closed on 22 Oct 2024; sales moved to Fab. |

Sources: [Unity Asset Store submission guidelines](https://assetstore.unity.com/publishing/submission-guidelines), [Asset Store Provider Agreement](https://unity.com/legal/provider), [Unity payouts](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetStorePayouts.html), [Fab publisher docs](https://dev.epicgames.com/documentation/en-us/fab/publisher-get-started-in-fab), [Fab licences and pricing](https://dev.epicgames.com/documentation/en-us/fab/licenses-and-pricing-in-fab), [Fab Unity/3D file requirements](https://dev.epicgames.com/documentation/en-us/fab/asset-file-format-and-structure-requirements-in-fab), [itch.io payments](https://itch.io/docs/creators/payments), [itch.io quality guidelines](https://itch.io/docs/creators/quality-guidelines), [itch.io terms](https://itch.io/docs/legal/terms), [Gumroad terms](https://gumroad.com/terms), [Gumroad fees 2026](https://checkoutpage.com/blog/gumroad-fees), [Payhip terms](https://payhip.com/terms), [Payhip fees and tax](https://dodopayments.com/blogs/payhip-review), [GameDev Market AI policy](https://www.gamedevmarket.net/news/an-important-update-on-generative-ai-assets-on-gdm), [GameDev Market bans AI](https://thedesignlab.blog/2025/08/29/gamedev-market-bans-ai-generated-assets/), [TurboSquid Publishing Agreement](https://resources.turbosquid.com/general-info/terms-agreements/publishing-agreement/), [CGTrader payout update](https://www.cgtrader.com/forum/general-discussions/payout-rate-update-effective-september-15-2026), [Sketchfab -> Fab](https://sketchfab.com/blogs/community/sketchfab-update-what-you-need-to-know-now-that-fabs-live/).

---

# 2. Terms & conditions check (do we meet them?)

Legend: **Met** = verified in this repo; **You** = needs an action or a statement only you can make; **Risk** = allowed, but may be challenged by a reviewer.

## Unity Asset Store (Submission Guidelines + Provider Agreement)

| Requirement | Status | Evidence / action |
|---|---|---|
| You own or may distribute every asset (Provider Agreement 6.8) | Met | All third-party content is CC0 (Poly Haven, Kenney, Quaternius, CreativeTrio, DREAM_SEARCH_REPEAT); `Publishing/license-evidence/` holds the proof. CC-BY and conflicting-licence props were removed. |
| No licences that require attribution / GPL / CC-BY | Met | Nothing shipped requires attribution. |
| Non-exclusive licence: you may sell the same content elsewhere | Met | The Provider Agreement licence is non-exclusive ([source](https://unity.com/legal/provider)); Fab even requires the same files. Unity only asks you not to undercut other publishers |
| Third-Party Notices.txt listing third-party components | Met | `Assets/RealisticClassroom/Third-Party Notices.txt` |
| Mesh files are FBX / DAE / ABC / OBJ | Met | 100% FBX (+ 0 OBJ); no glTF/GLB shipped |
| Prefabs at pos 0 / rot 0 / scale 1, 1 unit = 1 m, sensible pivots | Met | 43 prefabs checked by `Publishing/reports/validation.md` |
| Every mesh has materials; PBR maps set up; normal maps flagged; tileable textures | Met | 126 URP/Lit materials, all normal maps import as Normal Map |
| Code in user-declared namespaces | Met | `RealisticClassroom.*` |
| File paths under 150 characters | Met | longest is 100 |
| One root folder, no duplicate/redundant files | Met | `Assets/RealisticClassroom`; duplicate + unused-asset checks pass |
| Documentation, demo scene, CHANGELOG | Met | `Documentation/Documentation.pdf`, `Scenes/Classroom_Demo.unity` |
| URP or HDRP support (Unity 6.5+) | Met | URP (HDRP not supported - state it) |
| Fast Enter Play Mode (Domain Reload disabled) from Unity 6.6 | Met (see test log) | Statics are reset on `SubsystemRegistration`; tested with Domain Reload disabled in a clean project |
| No deprecated-API warnings | Met | compiled with 0 warnings in the clean test project |
| Works with the Input System package | Met | input facade + `RealisticClassroom.InputSystemBridge`; tested |
| AI disclosure ("AI description" field, no wording that implies hand-made work) | **You** | Paste the text in `04-listing-copy.md`. Code/tools were AI-assisted; no AI art. |
| Official validator (Asset Store Publishing Tools) passes | **You** | Cannot be run headless (needs your Unity ID). Tools > Asset Store > Validator on `Assets/RealisticClassroom`. `PackageValidator` is a local approximation (0 fail / 0 warn). |
| Accurate listing: polycounts, texture sizes, pipeline, dependencies, limitations | Met | Numbers in `04-listing-copy.md`; state low-poly characters and URP-only |
| Support contact (Provider Agreement 4.7) | **You** | Publisher name, support e-mail/site |
| Publisher account, tax interview, payout profile (Provider Agreement 4.3/4.4) | **You** | W-8BEN (individual) or W-9; you are responsible for your income tax |
| "Lacks original value beyond found content" (Content Transparency policy) | **Risk** | The value is the scripts, editor tools, scene assembly and conversion work, but most 3D content is CC0. A reviewer may still reject it. |

## Fab (Distribution Agreement / Technical Requirements)

| Requirement | Status | Evidence / action |
|---|---|---|
| IP warranty (no infringement) | Met | CC0 only |
| Declare third-party software/files | **You** | Tick the declaration and paste the third-party list from `Third-Party Notices.txt` |
| "Created with AI" self-declaration | **You** | Declare AI-assisted code; state that models/textures/animations are not AI-made |
| Unity product = Unity Asset Store guidelines and the same files as the Asset Store | Met | Same folder/package |
| Media: images >= 1920x1080, < 3 MB (JPEG/PNG); video 1920x1080 MP4/MOV/WEBM <= 300 MB | Met (images) | `Publishing/marketing/fab/*.jpg` (all < 350 KB). Video: optional, not produced (no encoder on this machine) |
| Licence: Standard (Personal / Professional) | **You** | Pick price from the preset list |
| Publisher identity, tax, payout (>= $100/month) | **You** | Fab publisher setup |

## itch.io (Terms + Quality guidelines)

| Requirement | Status | Evidence / action |
|---|---|---|
| Own or have the rights to publish (Terms) | Met | CC0 sources |
| Give credit when re-using others' work | Met | `Third-Party Notices.txt` inside the download + the store page text |
| Generative-AI tagging on asset pages (failure can lead to delisting) | **You** | Tag "AI used for code" (text in `04-listing-copy.md`) |
| Accurate description (refunds if it does not work as advertised) | Met | Honest limitations are in the copy |
| Payment model | **You** | Choose "Collected by itch.io" (merchant of record) |

## Gumroad (Terms of Service)

| Requirement | Status | Evidence / action |
|---|---|---|
| You own / have rights to the digital product | Met | CC0 sources |
| Product documentation and end-user licence terms | Met | `Documentation.pdf`, `legal/EULA.md` |
| Upload formats | **Risk** | The terms list approved formats such as pdf/png/jpeg; delivering a `.zip` is normal practice on Gumroad, but confirm at upload |
| Merchant of record / VAT | Met | Handled by Gumroad; you report your own income |

## Payhip (Terms of Use)

| Requirement | Status | Evidence / action |
|---|---|---|
| Seller warrants the content does not infringe third-party IP and that they may enter the agreement | Met | CC0 sources; licence evidence in `Publishing/license-evidence/` |
| Prohibited products (adult, hate, illegal, cyberlockers, gambling, resale-rights bundles ...) | Met | None applies. Do **not** sell it as a "resale rights" bundle |
| AI / third-party content rules | Met (none found) | The Terms of Use do not mention them; disclose the AI-assisted code anyway (text in `04-listing-copy.md`) |
| Tax | **You** | Payhip handles EU/UK VAT only; you owe US sales tax / other GST and your income tax. Payhip is not a merchant of record |
| Payment account | **You** | Connect your own Stripe and/or PayPal; payouts come straight from those |

## Not eligible

TurboSquid, CGTrader and GameDev Market: **not met** (see the exclusion table). Do not list there.
