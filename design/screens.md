# Screens: mockup → app

How each mockup screen (`design/mockup/screens/*.js`) is composed from SL controls, and where it
lives in the app. Read the mockup file next to this one — every `<Card>`, `<Banner>`, `<Dialog>` in
it has a direct SL equivalent (`components.md`).

**Migration order that pays off most:** dialogs first (each is self-contained), then card
contents, then page layout. The shell (sidebar, top bar) already exists in `V2Shell.cs`.

## Status

| Mockup | App | State |
|---|---|---|
| Dialog (pattern) | `ErrorDialog.cs` | ✅ on `SLDialog` |
| Devices → "How is the printer connected?" | `PrinterConnectionDialog.cs` (also the Printer page via `EmbeddedFormHost`) | ✅ on `SLDialog` |
| Labels → "What are you labelling?" pattern | `AddCombinationDialog.cs` | ✅ on `SLDialog` + `SLFieldSet` |
| Input prompts | `SLPrompt` (replaces VB `InputBox` in AddCombinationDialog) | ✅ — Form1's LAN prompt still uses `InputBox` |
| All tables | `ThemeStyles.Grid` → `SLTableStyle` | ✅ painted to spec |
| Scans | `V2ScansPage.cs` (rehosts Form1 controls) | ⏳ filters/cards use `Themed/*`; move to `SLCard` + `SLTextBox`/`SLComboBox` |
| Labels | `V2PrintPage.cs`, `V2PrintSurfaces.cs` | ⏳ |
| Devices → scanners | `ScannerManagementForm.cs` (embedded page) | ⏳ |
| Crops & products | `SetupDialog.cs` (embedded page) | ✅ on `SLDialog`: `SLSegmentedControl` tabs, filter fields, `SLTable` in an `SLCard` |
| Employee picker | `EmployeeSelectionDialog.cs` | ✅ on `SLDialog` + `SLTable` in an `SLCard` with pager footer |
| Login | `V2LoginScreen.cs` | ⏳ inputs/buttons → `SLField`/`SLTextBox`/`SLButton` |
| Site selection | `Themed/SiteTileButton.cs` | ⏳ |
| Overview, People | — | not in the app (no functionality to attach) |

## Recipes

### Any dialog

```
SLDialog  (Title, Description, Tone, DialogWidth)
├─ Body: SLStack vertical gap 16
│   ├─ SLBanner (errors / status about this dialog; hidden until needed)
│   ├─ SLFieldSet columns=1|2
│   │   └─ SLField(label, SLTextBox | SLComboBox | SLNumberBox) { Required, Hint, Error }
│   └─ SLKeyValueList / SLTable / SLText as needed
└─ Footer: AddAction(Secondary "Cancel"), AddAction(Primary "<verb> <thing>")
```
Button labels say what happens ("Save connection", "Remove scanner"), not "OK".

### Devices (`DevicesScreen.js`)

```
SLStack vertical gap 16 (max width 1080)
├─ SLBanner Error "Line 2 scanner isn't answering"  [Action: Secondary Sm "Look again"]
├─ SLCard "Scanners on this site"  BodyPadding=0
│   Actions: Secondary Sm "Look for scanners" (refresh-cw, Loading while scanning), Primary Sm "Add scanner" (plus)
│   Body: SLTable  — Scanner (name + mono serial · port), Line, Block, Supplier, Status (badge), row actions (SLIconButton pencil / trash-2 danger)
│         EmptyState: usb "No scanners found yet"
├─ SLCard "Label printer"  Actions: Secondary Sm "Change connection" → PrinterConnectionDialog
│   Body: row — 42px indigo-50 tile with printer icon, name (14/600) + mono address, SLBadge status
└─ SLDialog Danger "Remove this scanner?" (Keep it / Remove scanner)
```

### Scans (`ScansScreen.js`)

```
SLStack vertical gap 16
├─ SLCard padding 14×16: horizontal SLStack gap 12
│   SearchField 300px (search icon) · segmented range (Today / Last 7 days / This season / Custom) ·
│   SLComboBox 160px crops · count text at the right (Muted)
└─ SLCard BodyPadding=0, Footer = Pagination
    SLTable: Serial (mono), Time (muted), Block, Line, Picked by, Supplier, Product, Status (badge)
    Row click → SLDialog scan detail: SLKeyValueList + SLBadge; actions Close / Reprint this label
```

### Labels (`LabelsScreen.js`)

```
Two columns (auto-fit, min 340)
├─ Step buttons ×3 (white cards, indigo ring on the current one, numbered circles)
├─ SLCard per step with SLFieldSet columns=2 and Footer "Next: …" (arrow-right)
└─ Preview SLCard: sticker preview on Theme.N100, Print button (Primary Lg), ProgressBar
```

### Login (`LoginScreen.js`)

```
Two columns: navy panel (Theme.Navy900, white logo, 30px/600 headline, 3 icon rows) |
centred form 380px: SLText Heading "Sign in", SLText Body muted, SLBanner Error (hidden),
SLField Email (Lg, Required), SLField Password (Lg, Suffix eye SLIconButton),
row: SLCheckBox "Keep me signed in" + link, SLButton Primary Lg block (Loading), SLText Muted centred.
```

### Overview (`OverviewScreen.js`)

Not in the app. If added: warning SLBanner, 4 StatTiles, "Latest scans" SLCard with SLTable,
"Equipment" SLCard (rows: name + MonoCaption + SLBadge), "Start here" SLCard with block buttons and an SLToggle.
