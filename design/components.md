# Component catalog: mockup → C#

Every row maps a mockup component (`design/mockup/source/components/…`) to its SL control
(`ScanLink/DesignSystem/…`, namespace `ScanLink.DesignSystem`). Measurements are what the C#
control already does; you should not need to set sizes, fonts or colours yourself.

**Rule of thumb:** if you are typing a number, a `Color`, a `Font` or `Location =` in UI code,
there is a component or token for it — find it here first.

---

## Tokens (`ScanLink/Theme.cs`)

| CSS | C# | Notes |
|---|---|---|
| `--action-primary` `--indigo-500` | `Theme.ActionPrimary` / `Theme.Indigo500` | `#4D4AEA` |
| `--text-heading` / `-body` / `-muted` / `-label` | `Theme.TextHeading` / `TextBody` / `TextMuted` / `TextLabel` | |
| `--surface-app` / `-card` / `-sunken` | `Theme.SurfaceApp` / `SurfaceCard` / `SurfaceSunken` | page bg `#F7F8FA`, card white |
| `--border-default` / `-subtle` / `-strong` | `Theme.BorderDefault` / `BorderSubtle` / `BorderStrong` | |
| `--ok-*` `--warn-*` `--err-*` `--info-*` | `Theme.Ok500`, `Theme.Warn700`, … | |
| `--n-0 … --n-900`, `--navy-*` | `Theme.N0 … N900`, `Theme.Navy900` | |
| `--s-1 … --s-16` (4px scale) | `Theme.S1 … S16` | |
| `--r-sm/md/lg` | `Theme.RadiusSm` 6 / `RadiusMd` 8 / `RadiusLg` 12 | |
| `--h-sm/md/lg` | `Theme.HeightSm` 32 / `HeightMd` 38 / `HeightLg` 44 | |
| `font: 600 …` | `Theme.Font*Semibold` | **never** `FontStyle.Bold` for 600 |
| `font: 500 …` | `Theme.FontSmMedium`, `Theme.FontMdMedium` | Chrome on Windows renders 500 as Segoe UI Semibold (verified in CI) |
| `font: 400 …` | `Theme.FontSm`, `Theme.FontMd`, … | Regular |
| `--font-mono` | `Theme.FontMonoBody` (13px), `FontMonoMd` (14px), `FontMonoXs` (12px) | Consolas |

Text should normally go through `SLText` with a role instead of a font token (below).

---

## Controls

### Button → `SLButton` (`SLButton.cs`)

```jsx
<Button variant="primary" size="sm" icon={<Icon name="printer" size={16}/>}>Print labels</Button>
<Button variant="ghost" iconEnd={<Icon name="arrow-right" size={16}/>}>See all scans</Button>
<Button variant="primary" size="lg" block loading={busy}>Sign in</Button>
```
```csharp
new SLButton { Text = "Print labels", Variant = SLVariant.Primary, ButtonSize = SLSize.Sm, IconName = "printer" };
new SLButton { Text = "See all scans", Variant = SLVariant.Ghost, IconEndName = "arrow-right" };
var signIn = new SLButton { Text = "Sign in", ButtonSize = SLSize.Lg, AutoSize = false, Height = Theme.HeightLg }; // block: stretch it in an SLStack
signIn.Loading = true;
```
* Variants: `Primary` (indigo), `Secondary` (white + border), `Ghost`, `Navy`, `Success`, `Danger`, `DangerQuiet`.
* Sizes (height / side padding / text): `Sm` 32/12/13px, `Md` 38/16/14px, `Lg` 44/20/14px. Weight 600, radius 6.
* Hover, pressed, disabled (`Enabled = false` → grey) and loading (spinner) are built in.
* AutoSize is on: do not set `Width`. For a full-width ("block") button set `AutoSize = false` and let an `SLStack` stretch it.
* It is a real `Button`: `DialogResult`, `AcceptButton`, `PerformClick()` work.
* **One primary per view.** Secondary actions are `Secondary`; low-emphasis links in cards are `Ghost`.

### IconButton → `SLIconButton`

```jsx
<IconButton size="sm" variant="danger" icon={<Icon name="trash-2" size={16}/>} label="Remove scanner" />
```
```csharp
new SLIconButton { IconName = "trash-2", ButtonSize = SLSize.Sm, Variant = SLIconButtonVariant.Danger, Label = "Remove scanner" };
```
* `Sm` 30×30, `Md` 36×36. Variants `Ghost` (default), `Solid` (white + border), `Danger` (red icon, red hover).
* Always set `Label` — it is the tooltip and accessible name.

### Icon → `SLIcon`

```jsx
<Icon name="cloud-upload" size={16} />
```
* On SL controls use the `IconName` / `PrefixIcon` properties.
* Custom painting: `SLIcon.Draw(g, "cloud-upload", new Rectangle(x, y, 16, 16), Theme.TextMuted);`
* Stock controls that need an `Image`: `SLIcon.Bitmap("cloud-upload", 16, Theme.TextBody)` (cached).
* Any Lucide name works (1,737 icons, https://lucide.dev/icons). Stroke 1.75, round caps — as in the mockup.

---

## Text → `SLText` (`SLDisplay.cs`)

Replace `Label` with `SLText` and pick a role. Wraps to its width, keeps CSS line-height.

| Role (`SLTextStyle`) | Mockup style | Use for |
|---|---|---|
| `Display` | 30px/600 heading | login hero |
| `Heading` | 24px/600 heading | page titles ("Sign in", "Which site…") |
| `Title` | 16px/600 heading | card / dialog titles (usually set via `Title` props instead) |
| `TitleSm` | 14px/600 heading | section titles |
| `Body` | 14px/400 body | lead paragraphs |
| `BodySm` | 13px/400 body | default UI text |
| `BodySmStrong` | 13px/600 body | emphasis |
| `BodySmMedium` | 13px/500 body | names in lists, key/value values |
| `Muted` | 13px/400 muted | subtitles, descriptions |
| `Caption` | 12px/400 muted | hints, footnotes |
| `Label` | 13px label ink | form labels (usually via `SLField`) |
| `Mono` / `MonoCaption` | Consolas 13 / 12 | serials, IPs, ports |

```csharp
new SLText("Use the email and password your manager set up for you.", SLTextStyle.Body) { Color = Theme.TextMuted };
new SLText("SL-HH-02 · COM3", SLTextStyle.MonoCaption);
new SLText("Line 3 scanner", SLTextStyle.BodySm) { LineHeight = 1.3f }; // when the mockup says /1.3
```

---

## Feedback

### Badge → `SLBadge`

```jsx
<Badge tone="success" dot>Working</Badge>
```
```csharp
new SLBadge("Working", SLTone.Success, dot: true);
```
* Tones `Neutral`, `Brand`, `Success`, `Warning`, `Error`, `Info`. 12px/600, padding 3×10, pill, 6px dot.
* Words, not codes: "Working", "Offline", "Waiting".
* In tables use `SLTable.SetBadge(...)` instead of a control per cell.

### Banner → `SLBanner`

```jsx
<Banner tone="warning" icon={<Icon name="cloud-off"/>} title="42 scans haven't reached the cloud yet"
        action={<Button size="sm" variant="secondary">Sync now</Button>}>They're safely saved…</Banner>
```
```csharp
new SLBanner {
    Tone = SLTone.Warning, IconName = "cloud-off",
    Title = "42 scans haven't reached the cloud yet",
    Message = "They're safely saved on this computer. Sync them before you clean up local scans.",
    Action = new SLButton { Text = "Sync now", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
};
```
* Tones `Info`, `Success`, `Warning`, `Error`. Padding 12×14, radius 8, icon 18px, height fits the text.
* Use it for messages about the thing on screen (errors in a dialog, a disconnected scanner) —
  not `MessageBox`, not a red `Label`.
* Show/hide with `SLVisibility.Set(banner, bool)`.

### EmptyState → `SLEmptyState`

```csharp
new SLEmptyState {
    IconName = "search-x", Title = "No scans match that search",
    Description = "Try a shorter search, or widen the date range to Last 7 days.",
    Action = new SLButton { Text = "Clear filters", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
};
```
* Always say what happened **and** what to do next. On a table: `table.EmptyState = …`.

---

## Forms

### TextField → `SLField` + `SLTextBox`

```jsx
<TextField label="Printer address" mono hint="You'll find this printed on the label." />
<TextField label="Email address" required size="lg" placeholder="you@packhouse.co" error={err} />
<TextField label="Password" type="password" suffix={<button>…eye…</button>} />
```
```csharp
new SLField("Printer address", new SLTextBox { Mono = true }) { Hint = "You'll find this printed on the label." };
var email = new SLTextBox { FieldSize = SLSize.Lg, PlaceholderText = "you@packhouse.co" };
var emailField = new SLField("Email address", email) { Required = true };
emailField.Error = "Enter the email address you use for ScanLink."; // red border + message; null clears
new SLTextBox { UseSystemPasswordChar = true, Suffix = new SLIconButton { IconName = "eye", ButtonSize = SLSize.Sm, Label = "Show password" } };
```
* `SLField` = label (13px, red `*` if `Required`) + optional `Hint` (12px muted) + 6px gap + input + `Error` (12px red).
* `SLTextBox`: 32/38/44px (`FieldSize`), white, `#D0D5DD` border, radius 6, indigo border + ring on focus, red when invalid, grey when `ReadOnly`/disabled.
* **Existing designer TextBox?** `new SLTextBox(usernameTextBox)` wraps it in place: same object, same handlers,
  restyled. Use this when other code already reads the field.
  `PrefixIcon`, `Suffix` (any control), `Unit`, `Mono`, `Multiline`. The real `TextBox` is `.Inner`.

### Select → `SLField` + `SLComboBox`

```csharp
var connection = new SLComboBox { PlaceholderText = "Choose a connection" };
connection.Items.AddRange(new object[] { "Network (LAN)", "USB cable" });
new SLField("Connection", connection) { Hint = "Network is the usual choice in a packhouse." };
```
* A real `ComboBox` (always `DropDownList`): `DataSource`, `DisplayMember`, `ValueMember`,
  `SelectedValue`, `SelectedIndexChanged` behave exactly as before. Swap `new ComboBox` for `new SLComboBox`.
* Same box as the text field; chevron at the right; list rows highlight indigo-50.

### NumberField → `SLField` + `SLNumberBox`

```csharp
new SLField("Avg weight", new SLNumberBox { Unit = "kg", DecimalPlaces = 2, Maximum = 1000, Increment = 0.1M });
```
* Same members as `NumericUpDown` (`Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ValueChanged`). 160px wide; ↑/↓ and the wheel step.

### Checkbox → `SLCheckBox`, Toggle → `SLToggle`

```csharp
new SLCheckBox { Text = "Keep me signed in", Checked = true };
new SLToggle { Text = "Open ScanLink on this page", Description = "Otherwise ScanLink opens on Scans." };
```
* Real `CheckBox`es (`Checked`, `CheckedChanged`). Toggle = settings that apply immediately; checkbox = choices confirmed by a button.

### FieldSet → `SLFieldSet`

```jsx
<FieldSet columns={2}> <Select label="Crop"…/> <Select label="Product"…/> </FieldSet>
```
```csharp
var fields = new SLFieldSet { Columns = 2 };          // optional Title / Description
fields.Add(new SLField("Crop", cropCombo) { Required = true });
fields.Add(new SLField("Product", productCombo) { Required = true });
fields.Add(new SLField("Notes", notes), span: 2);     // full row
```
* 16px gaps both ways; each field keeps its own height (align-items: start).

---

## Layout

### Flex boxes → `SLStack` (`SLContainers.cs`)

| Mockup | C# |
|---|---|
| `display:flex; flex-direction:column; gap:16px` | `new SLStack(SLOrientation.Vertical, 16)` |
| `display:flex; gap:8px; align-items:center` | `new SLStack(SLOrientation.Horizontal, 8) { Align = SLAlign.Center }` |
| `justify-content:flex-end` / `space-between` | `Justify = SLJustify.End` / `SLJustify.SpaceBetween` |
| `flex: 1` on a child | `stack.SetGrow(child)` |
| `padding: 10px 12px` | `Padding = new Padding(12, 10, 12, 10)` (left, top, right, bottom) |

* Children keep **insertion order** (unlike `Dock`). Add with `Controls.Add` or `AddRange`.
* Vertical stacks stretch children to full width (`Align = Stretch`); height comes from the child.
* Hide/show children with `SLVisibility.Set(child, bool)` so the stack re-flows (plain
  `Visible = false` before the form is shown is not observable).
* Transparent: the parent's surface shows through.

### Card → `SLCard`

```jsx
<Card title="Equipment" subtitle="Green means it's sending data right now." padding="8px"
      actions={<Button size="sm" variant="ghost">Manage</Button>} footer={<Pagination…/>}>…</Card>
```
```csharp
var card = new SLCard { Title = "Equipment", Subtitle = "Green means it's sending data right now.", BodyPadding = new Padding(8) };
card.Actions.Controls.Add(new SLButton { Text = "Manage", Variant = SLVariant.Ghost, ButtonSize = SLSize.Sm });
card.Body.Controls.Add(content);       // Body is a vertical SLStack, gap 16, padding 20
card.Footer.Controls.Add(pagination);  // Footer is hidden while empty
```
* White, 1px `#E4E7EC`, radius 12. Header padding 16×20 with a divider; footer `#FCFCFD` band.
* `padding="0"` (edge-to-edge tables) → `BodyPadding = Padding.Empty`.
* Inside a card, buttons and inputs automatically get correct corner colours (`ISLSurface`).

### Dialog → `SLDialog` (`SLDialog.cs`)

```jsx
<Dialog title="Remove this scanner?" tone="danger" width="440px" description="…"
        actions={<><Button variant="secondary">Keep it</Button><Button variant="danger">Remove scanner</Button></>}>
  …body…
</Dialog>
```
```csharp
internal class RemoveScannerDialog : SLDialog
{
    public RemoveScannerDialog(string name)
    {
        Title = "Remove this scanner?";
        Tone = SLDialogTone.Danger;
        DialogWidth = 440;
        Description = name + " will stop sending scans to this site. You can add it again later.";
        // Body.Controls.Add(...) for content
        AddAction(new SLButton { Text = "Keep it", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
        AddAction(new SLButton { Text = "Remove scanner", Variant = SLVariant.Danger, DialogResult = DialogResult.OK });
    }
}
```
* Header: title 16px/600 (red for `Danger`, amber for `Warning`), description 13px muted, ✕ close.
* `Body`: vertical `SLStack`, padding 16×20, gap 16. `Footer`: `#FCFCFD` band, buttons right-aligned, gap 8 —
  **secondary first, primary last**.
* Width `DialogWidth` (default 520); height fits the content. `BodyHeight` fixes it (for logs and grids that
  scroll — then `Body.SetGrow(theScrollingControl)`).
* Modal: rounded corners + shadow (Windows 11), dimmed backdrop over the owner, drag by the header, Esc closes.
* Embedded as a page (`EmbeddedFormHost`): drawn as a card on the page, no ✕, no backdrop.
* Inherit from it for **every** dialog. Never `FormBorderStyle.FixedDialog` + hand-placed controls.

### Segmented switch → `SLSegmentedControl`

The range picker on the mockup's Scans screen (Today / Last 7 days / This season / Custom). Use it
for a few mutually exclusive views or filters, e.g. tabs over a table.
```csharp
var tabs = new SLSegmentedControl("Today", "Last 7 days", "This season", "Custom");
tabs.SelectedIndexChanged += (s, e) => Reload(tabs.SelectedItem);
```
* `#F1F3F7` track, 3px padding, 4px gaps; selected item white with a soft shadow, heading ink, 600.
* ←/→ keys move the selection. AutoSize: do not set Width.

### Anything else in a field box → `SLFrame`

`new SLFrame(productComboBox)` / `new SLFrame(numericUpDown)` / `new SLFrame(checkedListBox) { Height = 96 }`:
a stock control the library has no version of (editable ComboBox, NumericUpDown, list boxes) sits in
the standard field box. Use it when the control must stay stock because code depends on its type.

### Replacing a designer Button

`button_x = ReplaceButton(button_x, button_x_Click, new SLButton { ... })` (Form1) moves the handler
across; the field then points at the SL button, so `Enabled`/`Text`/`PerformClick` keep working.

### Prompt → `SLPrompt`

Instead of `Microsoft.VisualBasic.Interaction.InputBox` or ad-hoc input forms:
```csharp
string name = SLPrompt.Ask(this, "Add a new grade", "Grade name", hint: "For example \"Class 3\".", confirmText: "Add grade");
if (name == null) return; // cancelled
```

### Key/value list → `SLKeyValueList`

```csharp
var kv = new SLKeyValueList(); kv.Add("Time", "07:14"); kv.Add("Picked by", "J. Mokoena");
```
Muted keys, 500-weight body values, 10px rows, 20px column gap (the scan-detail dialog).

---

## Data

### DataTable → `SLTable` / `SLTableStyle`

```jsx
<DataTable columns={[{key:"serial", header:"Serial", mono:true}, {key:"time", muted:true},
                     {key:"state", header:"Status", render: r => <Badge tone={r.tone} dot>{r.state}</Badge>}]} … />
```
```csharp
var table = new SLTable { DataSource = rows };              // read-only, full-row select, no row headers
table.SetMono("Serial");
table.SetMuted("Time");
table.SetBadge("Status", v => (string)v == "Synced" ? SLTone.Success : SLTone.Warning);
table.SetIconAction("Delete", "trash-2", danger: true);     // a DataGridViewButtonColumn, drawn as an IconButton
table.EmptyState = new SLEmptyState { IconName = "search-x", Title = "No scans match that search", Description = "…" };
```
* Header 37px `#F7F8FA`, 12px/600 UPPERCASE muted, 1px bottom border. Rows 43px (49px once a badge column is set), 16px padding,
  hover `#F7F8FA`, selected indigo-50, `#F1F3F7` dividers, no zebra stripes.
* Existing grids: `ThemeStyles.Grid(grid)` (or `SLTableStyle.Apply(grid)`) gives the same look without changing type.
* Put tables in an `SLCard` with `BodyPadding = Padding.Empty`.

---

## Not yet in the library

From the mockup but not ported yet: `Pagination`, `StatTile` (v1 exists as `Themed/StatTile.cs`),
`SiteTile` (`Themed/SiteTileButton.cs`), `SearchField` (`Themed/SearchField.cs`; or `SLTextBox` with
`PrefixIcon = "search"`), `DateField`,
`Slider`, `ProgressBar`, `StatusBar`, `Sidebar` / `TopBar` (`Themed/SidebarNav.cs`, `Themed/TopBar.cs`).
`Themed/SegmentedControl.cs` is superseded by `SLSegmentedControl`.
The `Themed/*` versions predate this library and restyle stock controls; when you touch them, port
them to SL controls (read the mockup source file, then follow the patterns above) and add a gallery scene.
