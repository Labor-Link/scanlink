# ScanLink design system (WinForms)

The UI team's mockup (`mockup/ScanLink-Client-Standalone.html`) is a React prototype. This
folder turns it into something a C# developer — or a coding agent — can match exactly.

| Path | What it is |
|---|---|
| `mockup/source/components/**` | The mockup's 26 components, one readable file each. **The spec.** |
| `mockup/screens/*.js` | The mockup's screens (Login, Sites, Overview, Scans, Labels, Devices). |
| `mockup/tokens.css` | All CSS tokens. Only the **v2 layer** (bottom half, after "ScanLink v2 — the redesign layer") applies; the top half is the legacy palette. |
| `components.md` | **Start here.** Every component: React usage → C# usage, measurements, rules. |
| `screens.md` | Each mockup screen as a composition of SL controls, and where it lives in the app. |
| `reference/gallery.html` | Every component state rendered with the mockup's own code. |
| `tools/` | Unpack the mockup, generate icons, capture references, score fidelity. |
| `../ScanLink/DesignSystem/` | The C# component library (`SL*` controls). |
| `../ScanLink/Theme.cs` | Tokens (colours, spacing, radii, fonts) — a port of the v2 CSS layer. |

## Why it is built this way

* **Controls paint themselves.** `SLButton`, `SLComboBox`, `SLDialog`… draw every pixel with
  anti-aliased GDI+, instead of restyling stock controls (whose `Region` clipping gives jagged
  corners and cannot do borders, rings or states properly).
* **Layout is measured, not positioned.** `SLStack` (flexbox), `SLFieldSet` (form grid),
  `SLCard` and `SLDialog` size children from what they report. There is no
  `Location = new Point(…)` in design-system code, so new fonts and longer text cannot clip.
* **Names match the mockup.** `<Button variant="secondary" size="sm">` is
  `new SLButton { Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }`.
* **Stock behaviour is kept.** `SLButton : Button`, `SLComboBox : ComboBox`,
  `SLCheckBox : CheckBox`, `SLTable : DataGridView` — `DialogResult`, `DataSource`,
  `SelectedValue`, `CheckedChanged` all still work, so migrating a form changes its look, not its logic.
* **Icons are vectors.** All 1,737 Lucide icons (the mockup's icon set), any size, any colour:
  `IconName = "cloud-upload"`.
* **Font weights match Chrome on Windows.** CSS 600 → `Segoe UI Semibold`; CSS 500 → Regular
  (Segoe UI has no 500, so Chrome falls back to 400 too).

## Checking fidelity

The loop that tells you whether a change matches the mockup:

1. `design/reference/gallery.html` renders each scene (button, dialog, table…) with the mockup's
   own components; `ScanLink.exe --gallery <dir>` renders the **same scene ids** with SL controls
   (`ScanLink/DesignSystem/SLGallery.cs`).
2. `tools/compare.js` diffs them and writes `report.html` (mockup | WinForms | diff) and a score table.
3. The **Design fidelity** GitHub Actions workflow (`.github/workflows/design-fidelity.yml`) does
   all of this on a Windows runner on every push touching `ScanLink/` or `design/`. Read the
   score table on the run's summary page; download the `design-fidelity-report` artifact for images.

On Windows, locally:

```powershell
cd design/tools; npm ci; cd ../..
node design/tools/unpack-mockup.js
node design/tools/capture-references.js --out out/reference
msbuild ScanLink/ScanLink.csproj /p:Configuration=Release
ScanLink/bin/Release/ScanLink.exe --gallery out/actual
node design/tools/compare.js --ref out/reference --actual out/actual --out out/report
start out/report/report.html
```

On macOS you can build (see `docs/V2_MIGRATION_STATUS.md`) and capture references
(`CHROME_PATH=… node design/tools/capture-references.js`), but WinForms only runs on Windows.

**Reading scores.** Chrome and GDI anti-alias text differently, so a faithful port scores
roughly 94–98%, not 100%. A size note ("size 240x86, expected 240x80") means spacing is off —
fix those first. Then look at the red areas in the diff images.

**When you add a component or migrate a screen**, add a scene with the same id to both
`reference/gallery.html` and `SLGallery.cs`.

## Regenerating assets

```bash
node design/tools/unpack-mockup.js   # mockup runtime -> design/mockup/vendor (gitignored)
node design/tools/build-icons.js     # Lucide -> ScanLink/Assets/lucide-icons.txt
```

If the UI team sends a new mockup, replace `mockup/ScanLink-Client-Standalone.html` and run
`unpack-mockup.js`: it rewrites `mockup/source/` and `mockup/screens/`. `git diff design/mockup`
then shows exactly what the UI team changed — port those changes to the SL controls.
