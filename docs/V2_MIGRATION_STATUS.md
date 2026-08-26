# ScanLink v2 shell migration — status

Branch: `feature/v2-shell` · baseline tag: `pre-v2-shell-migration`
Scope: restyle the WinForms client onto the v2 design system. **No new functionality.**

---

## Done

**Token layer** — `Theme.cs` ports `tokens/product.css`; `Themed/ThemeStyles.cs` holds the
shared button / input / grid / dialog treatment. 49 status-colour call sites in `Form1.cs`
now use semantic tones.

**Shell (P2)** — navy `SidebarNav` + `TopBar` replace the seven-tile header. Destinations:
Scans · Print labels · Scanners · Printer · Crops & products · **Reports** · site tile +
Sign out in the footer. The seven original buttons are hidden, not deleted, and every
sidebar item calls `PerformClick()` on them so their guards still run.

**Scans page (P3)** — output console, filters and grid wrapped in `CardPanel`; pagination is
the grid card's footer; the three counts became `StatTile`s. `UpdateCountLabels` is
untouched — labels moved inside the tiles and their `TextChanged` splits caption from value.

**Login (P4)** — split screen with the navy brand panel. The ~40 lines of centring
arithmetic in `LayoutRootPanels` are gated behind `V2LoginActive`.

**Dialogs (P5)** — all six restyled. `MultiUserDashboard` on the v2 tile treatment.

**Print (P6)** — Bar codes + Box labels merged into one `PrintLabelsPopupForm` with a
segmented switcher. Advanced settings collapsed behind a toggle. Both original popup forms
are left intact, so reverting to two entries is a one-line change in `Sidebar_ItemSelected`.

**Icons** — 15 Lucide icons × 3 sizes × 3 tints = 135 PNGs embedded, plus a white knockout
logo for the navy shell. Every icon call is a no-op if the asset is missing, so the app
degrades to its original emoji rather than to blank buttons.

---

## Bugs found and fixed along the way

| | |
|---|---|
| **Pre-existing build breaker** | Three caption labels were instantiated in `Form1.Designer.cs` but never declared. Broke `dotnet build` with CS1061 on a clean checkout; Visual Studio masked it. Orphans removed. |
| **AutoSize + docked children** | A WinForms container with `AutoSize=true` whose children are docked collapses to zero. I hit this in 5 containers — it blanked the entire login form and rendered cards as empty white boxes. All now use explicit heights or `FlowLayoutPanel`. |
| **DPI manifest** | PerMonitorV2 made WinForms rescale several hundred hard-coded controls at startup — slow *and* lossy, clipping captions to "Basic Wag", "Ap", "Clea". Reverted to DPI-unaware. |
| **`&` mnemonic** | "Crops & products" rendered as "Crops  products". Escaped; `UseMnemonic=false` on shell labels. |
| **Dark logo on navy** | Sidebar used the dark mark on the navy rail. White knockout now derived and embedded. |
| **Password toggle** | Both branches set the *same* glyph, so it gave no feedback. Now `eye` / `eye-off`. |
| **Site tile dead zones** | Only the panel carried the click handler, so clicking the site name or ID did nothing. Labels now forward click and hover. |

---

## Not done

- **Runtime verification.** Compiles clean (0 errors, 0 warnings) but layout correctness is
  unproven — the last screenshots predate the AutoSize fixes.
- **Tier C, excluded by scope** (present in the prototype, absent from the app): Overview
  screen, People, search field, date presets, Status badge column, home-view toggle.
- `BadgeLabel` is built but unused — it has no home until a status column exists.
- The **Daily Stats Logger** panel is dense original layout, never redesigned.

---

## Building on macOS

.NET Framework 4.8 cannot be installed on macOS, but its *reference assemblies* work
cross-platform, so the project compiles here (the exe still only runs on Windows):

```
curl -sSL -o net48.nupkg \
  https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net48/1.0.3/microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg
unzip -q net48.nupkg -d refasm
dotnet build ScanLink/ScanLink.csproj -c Release \
  -p:FrameworkPathOverride=refasm/build/.NETFramework/v4.8
```

Icons are regenerated with `npm i sharp lucide-static && node tools/render-icons.js`.
ImageMagick is **not** usable — its internal SVG renderer silently produces blank PNGs.
