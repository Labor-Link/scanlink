# ScanLink — notes for coding agents

WinForms client on **.NET Framework 4.8, C# 7.3** (no switch expressions, `??=`, `using var`,
records, or target-typed `new`). Main project: `ScanLink/ScanLink.csproj` (old-style csproj:
every new `.cs` file needs a `<Compile Include=…>` entry).

## UI work: use the design system

The UI is being moved onto the UI team's mockup. The mockup and its exact specs live in `design/`;
the C# implementation is `ScanLink/DesignSystem/` (`SL*` controls) plus tokens in `ScanLink/Theme.cs`.

**Before any UI change, read `design/components.md`** (React → C# mapping for every component)
and the relevant part of `design/screens.md`. When a measurement is unclear, the source of truth is
the mockup component in `design/mockup/source/components/**`, not your judgement.

Rules:

1. **Build UI from SL controls.** `SLButton`, `SLIconButton`, `SLTextBox`, `SLComboBox`, `SLNumberBox`,
   `SLCheckBox`, `SLToggle`, `SLField`, `SLFieldSet`, `SLCard`, `SLBanner`, `SLBadge`, `SLText`,
   `SLEmptyState`, `SLTable`, `SLKeyValueList`, `SLDialog`, `SLPrompt`.
   Do not create `new Button()`, `new Label()`, `new TextBox()`, `new ComboBox()`, `GroupBox`, or
   `MessageBox`/`InputBox` for in-app messages in new or migrated UI.
2. **Every dialog inherits `SLDialog`.** Content goes in `Body`, buttons via `AddAction`
   (secondary first, primary last). No `FormBorderStyle.FixedDialog`, no hand-placed controls.
3. **No absolute layout.** No `Location = new Point(…)`, no hard-coded `Size` for layout. Use
   `SLStack` (flexbox), `SLFieldSet` (form grid), `SLCard`, `SLDialog`. Hide/show children of these
   with `SLVisibility.Set(control, bool)`.
4. **No literals.** No `Color.FromArgb`, hex values, `new Font(…)` or `FontStyle.Bold` in UI code.
   Colours/spacing/radii come from `Theme`; text styles come from `SLTextStyle` roles. CSS weight 600
   is `Theme.*Semibold`, never Bold.
5. **Icons are Lucide names** (`IconName = "cloud-upload"`), the same names as the mockup's
   `<Icon name=…>`. No emoji in UI text.
6. **Keep behaviour.** SL controls subclass the stock ones (`SLComboBox : ComboBox`, `SLButton : Button`,
   `SLCheckBox : CheckBox`), so event handlers, `DataSource`/`SelectedValue`, `DialogResult` and
   field names should survive a migration unchanged. Restyle; do not rewrite logic.
7. **One primary button per view.** Button text names the action ("Save connection"), not "OK".
8. **Missing component?** Port it from `design/mockup/source/components/…` into
   `ScanLink/DesignSystem/` following the existing SL controls (paint in `OnPaint` with `SLPaint`,
   measure with `ISLMeasure`), document it in `design/components.md`, and add a gallery scene.

## Verifying UI changes

You cannot see WinForms output on macOS/Linux. After a UI change:

1. Build (macOS: see `docs/V2_MIGRATION_STATUS.md`, "Building on macOS").
2. If you changed or added a component or a dialog, add/update a scene with the **same id** in
   `design/reference/gallery.html` and `ScanLink/DesignSystem/SLGallery.cs`.
3. Push: the **Design fidelity** GitHub Actions workflow renders the gallery on Windows and scores it
   against the mockup. Read the score table on the run summary
   (`gh run view <id>`; `gh run download <id> -n design-fidelity-report` for the images) and fix
   size mismatches and low scores before calling the work done. See `design/README.md`.

## Repo notes

- `ScanLink/bin/**` build outputs are tracked in git; building rewrites them.
- `docs/V2_MIGRATION_STATUS.md` records the v2 shell migration and WinForms pitfalls found so far
  (AutoSize + Dock collapse, DPI manifest, `&` mnemonics). Read it before touching `Form1`.
