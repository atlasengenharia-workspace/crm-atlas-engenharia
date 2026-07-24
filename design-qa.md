# Design QA — Login Atlas

**Source visual truth**

- `C:\Users\Vinicius Moreira\Desktop\crm-atlas-v2\crm-atlas-v2\output\frontend-audit\01-login.png`
- Pixels: 1280 × 720

**Rendered implementation**

- URL: `http://127.0.0.1:5188/Login`
- Screenshot: `C:\Users\Vinicius Moreira\Desktop\crm-atlas-v2\crm-atlas-v2\output\frontend-auth\login-desktop-v2.jpg`
- Comparison: `C:\Users\Vinicius Moreira\Desktop\crm-atlas-v2\crm-atlas-v2\output\frontend-auth\login-comparison.jpg`
- Pixels/CSS viewport: 1280 × 720
- Device pixel ratio: 1
- Density normalization: none; both artifacts were compared at 1280 × 720
- State: anonymous, desktop, initial login screen

**Full-view comparison evidence**

- The comparison contains the reference and implementation in the same image.
- The two-column composition, blueprint background, Atlas logo, technical product image, headline hierarchy, copper CTA, benefit chips, radii, borders and dark-blue palette are preserved.
- The implementation intentionally adds a short Auth0 security note below the CTA.

**Focused region comparison evidence**

- No separate crop was needed because the logo, CTA, hero copy, chips and source imagery are clearly readable in the full-size 2560 × 760 comparison.

**Required fidelity surfaces**

- Fonts and typography: hierarchy, weights, line-height and wrapping remain consistent; browser-safe Inter/Segoe UI fallbacks are used.
- Spacing and layout rhythm: central 1100 px shell, equal columns, panel padding, button height and chip spacing remain balanced at the reference viewport.
- Colors and visual tokens: deep navy, muted steel text and copper CTA match the source direction with accessible focus treatment.
- Image quality and asset fidelity: the supplied Atlas SVG logo, blueprint background and product image are reused; no handcrafted replacement artwork is present.
- Copy and content: Atlas-specific authentication, product value statement and benefits are preserved; Auth0 is identified as the secure identity provider.

**Findings**

- No actionable P0, P1 or P2 visual differences remain.
- P3: the implementation logo is slightly larger and the Auth0 security note adds vertical content not present in the source. Both are acceptable identity/security refinements.

**Interaction and accessibility evidence**

- Primary CTA is the only page action and points to the Razor Page Auth0 challenge handler with a local return URL.
- CTA has a descriptive accessible name, visible keyboard focus, hover/active/loading states and reduced-motion support.
- Browser console errors checked: none on the rendered login page.
- Responsive CSS explicitly collapses to one panel below 900 px and removes outer framing below 520 px; a browser-rendered mobile capture remains a residual test gap.
- The Auth0 challenge reached the OpenID Connect handler. Completion was blocked in the sandbox because outbound access to the tenant metadata endpoint was denied; this is an environment limitation, not a visual implementation failure.

**Comparison history**

- Initial rendered capture was recaptured without an invalid screenshot option so the complete two-panel state matched the source viewport.
- Post-fix evidence: `login-desktop-v2.jpg` and `login-comparison.jpg`.
- Accessible CTA naming was added after DOM inspection.

**Implementation checklist**

- [x] Preserve supplied Atlas identity assets.
- [x] Keep credentials and password collection inside Auth0 Universal Login.
- [x] Validate local return URLs before authentication redirects.
- [x] Add responsive, focus and reduced-motion states.
- [x] Build the Web project in Release with zero warnings and zero errors.

final result: passed
