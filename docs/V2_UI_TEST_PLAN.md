# ScanLink v2 UI: manual test plan

Branch `feature/v2-shell`. This release restyles the WinForms client onto the UI team's mockup
(design system in `ScanLink/DesignSystem`). **Business logic is meant to be unchanged.** The
main risk is that a control was moved or replaced and one of its behaviours stopped working,
so most checks below are "does it still work", not "does it look right".

## Getting the build

1. Open the latest **Design fidelity** run on `feature/v2-shell` in GitHub Actions.
2. Download the **scanlink-test-build** artifact and unzip it to a folder on a Windows PC.
3. Run `ScanLink.exe`. Keep the `.ps1` scripts and `.dll` next to the exe.

The test build contains the application only. Scanners in COM mode need the **Datalogic USB-COM
driver**, which the production installer installs. On a PC that has never had ScanLink, run the
current production installer once first (or install `DATALOGIC_USBCOM_v7.1.1` by hand), then use
the test build. The Argox printer driver is installed separately, as for production.

Test on at least one **small screen (1366×768 or 1280×720)** and one normal screen (1920×1080).
Report bugs with a screenshot, the screen name below and the steps.

## What to look for everywhere

- Nothing cut off, overlapping or clipped (buttons, text, table columns, dialog edges).
- Every dialog opens centred over the app with the rest of the window dimmed, closes with
  **Esc**, the **✕** or its Cancel/Close button, and can be dragged by its title area.
- Buttons show hover and pressed states; disabled buttons look grey and do nothing.
- Text fields show an indigo border when focused; **Tab** moves through fields in order.
- No Windows pop-up message boxes where a screen now shows an in-page banner (see below).
- **Status bar** along the bottom of every page: a coloured dot and one line of text — grey "Ready"
  at start, green for success (e.g. "Labels sent"), red for errors (e.g. a failed print), blue for
  progress. Long or multi-line messages stay on one line.

---

## 0. Shell

**Scrolling (everywhere):** with the mouse over any page, card, table or dialog, the wheel
scrolls it — no need to click into it first. Pages taller than the window show a scrollbar.

**Dialogs (everywhere):** Add combination, Find picker, Remove scanner, COM help, Add new grade /
count / carton type, error details — each opens over a dimmed app with **no error**, and clicking
the dimmed area does nothing.

| # | Check | Expected |
|---|---|---|
| 0.1 | Sidebar **Sign out** | Returns to the Sign in screen; signing in again works. |
| 0.2 | Reports → Open reports | Opens the web dashboard in the browser. |
| 0.3 | Status bar, right side | Shows "N Scanners Connected · 1 Printer Connected" and updates within ~2 s of plugging/unplugging. |
| 0.4 | Keyboard-mode (HID) scanner on the Scans page right after sign-in | Scans do not type into the search box or click header buttons. |

## 1. Login

| # | Check | Expected |
|---|---|---|
| 1.1 | Open the app | Navy panel left, "Sign in" form right. **No logo** (known, see Limitations). |
| 1.2 | Sign in with valid credentials | Button shows a spinner and "Signing in…", then the app loads. |
| 1.3 | Press **Enter** in the email or password field | Starts sign-in, same as the button. |
| 1.4 | Wrong password | Red banner "Invalid username or password" above the fields; button returns to "Sign in". |
| 1.5 | Leave a field empty and sign in | Red banner "Please fill in all required fields". |
| 1.6 | Eye icon in the password field | Toggles the password visible/hidden; icon changes eye ↔ eye-off. |
| 1.7 | Placeholders | Email shows grey "you@packhouse.co", password shows grey "Your password"; both clear when you click in. |
| 1.8 | Site selection after sign-in, then cancel it | Red banner explaining the cancel; can sign in again. |
| 1.9 | Restart the app while signed in | Info banner "Restoring session…" then the app opens without signing in. |
| 1.10 | Session expired / restore failed | The message shows as a banner on the Sign in screen (it used to be dropped). |
| 1.11 | Placeholders on a fresh start | "you@packhouse.co" and "Your password" are grey, not black like typed text. |

## 2. Scans page

| # | Check | Expected |
|---|---|---|
| 2.1 | Header | "Live" badge, **Sync now**, **Print labels**. Sync now = old "Sync logs to API"; Print labels opens the Print page. |
| 2.2 | Search box | Filters by serial, block, supplier or product ~0.3 s after you stop typing. |
| 2.3 | Today / Last 7 days / This season | Table and scan count update to that range. |
| 2.4 | Custom | Opens "More filters" with the date pickers; picking dates filters. |
| 2.5 | Crop select | Filters by crop; list fills after sign-in. |
| 2.6 | More filters / Fewer filters | Shows/hides a row of labelled From, To, Block, Line, Product fields with Apply and Clear; labels not broken across lines; Apply/Clear still filter. |
| 2.7 | Page opens | Filter card, then the **scans table** in a card with "Page x of y" and Previous / Next in its footer. The Daily Stats Logger and Connected Scanners panels are **hidden**. |
| 2.8a | Show details, then scroll down | The page scrolls to the Daily Stats and Connected Scanners cards and the table below them. |
| 2.9a | Daily Stats card | Fields in three columns with labels (Date, Hours, Basic wage … Driver name), a status badge (Not set / Loading / Data loaded / No site selected), **Save stats** (spinner while saving) and a quiet **Debug token**. Changing the date loads that day; Save stats saves. |
| 2.9b | Scanner output card | "Show the live scanner log" switch, **Sync logs to server** and **Clean up local scans** buttons work as before. |
| 2.8 | Show details / Hide details | Shows/hides the scanner console, the totals, and the Daily Stats Logger + Connected Scanners cards; the console keeps logging while hidden. |
| 2.9 | Daily Stats Logger (details shown) | "Date Selected" label not clipped; entering values and Save still work. |
| 2.10 | Connected Scanners (details shown) | Editing Line / Block / Supplier and the save icon on a row still update that scanner. |
| 2.11 | Previous / Next | Pages through scans; disabled on first/last page; "Page x of y" correct. |
| 2.12 | Table | Headers read DATE, TIME, SERIAL, BLOCK, LINE, SUPPLIER, CROP, PRODUCT, PICKER; serial in monospace, time in grey; hover highlights a row; selected row light indigo. |
| 2.13 | No scans for the filters | "No scans to show" message in the table instead of a blank box. |
| 2.14 | Clear filters after picking "Today" | The range switch returns to Custom; clicking Today again re-applies it. |
| 2.16 | Scan quickly with two or more scanners at once (< 1 s apart) for a minute | No "Error loading scans data" pop-ups; the table never empties; every scan appears. |
| 2.17 | Leave the app signed in past the token's expiry (or ask the backend team for a short-lived token) | Uploads keep working or ask you to sign in again; no silent 401 failures in the console. |
| 2.18 | Check the installed `scan_capture.ps1` | Same as `ScanLink/ScanLinkScanner/scan_capture.ps1`; scans are queued in `api_upload_logs.jsonl` and reach the server without restarting. |
| 2.15 | Scan count | "N scans" next to the filters is correct after paging, filtering and clearing. |

## 3. Print labels

| # | Check | Expected |
|---|---|---|
| 3.1 | Step buttons at the top | Current step indigo; finished steps show a green tick; you cannot jump past step 1 until crop, product and picker are set (red banner explains). |
| 3.2 | Step 1: Crop | Choosing a crop narrows the Product list. |
| 3.3 | Step 1: Product | Typing to search and picking from the list both still work; "This combination prints as" fills in. |
| 3.4 | Step 1: Who is picking? / Find picker | Typing works; **Find picker** opens the employee picker (see 7). |
| 3.5 | Step 1: Add combination | Opens "Add product combination" (see 6). |
| 3.6 | Step 2: Number of labels | Single box (no double border); arrow keys / wheel change it; preview updates. Back returns to step 1. |
| 3.7 | Step 3: summary | Crop, Product, Picker, Labels and Barcode listed correctly. |
| 3.8 | Generate barcode | Barcode appears in the summary and preview; **Start printing** enables and turns **green**. |
| 3.9 | Open full preview | Opens the existing full preview. |
| 3.10 | Start printing | Prints the right number of labels; progress bar runs; button text follows the job. If the printer is not set up, configure it first under **Printer** ("No USB device selected" means no USB printer was picked there). |
| 3.11z | Printer settings → Show, then scroll | The page scrolls down through every setting and the sticker diagram. |
| 3.11 | Printer settings → Show / Hide | Expands/collapses the advanced printer settings and scrolls them into view. |
| 3.11a | Printer settings on a **fresh PC** (never printed before) | Test Mode and Barcode Type are filled (not empty); printing works without opening settings first. |
| 3.11c | Printer settings look | No "Advanced Print Settings" frame inside the card; labels in grey field-label style; drop-downs (Printer language, Test mode, Barcode type, Speed) open and save their choice; "Print two stickers horizontally" checkbox works. |
| 3.11b | Sticker layout diagram | Shown **below** the settings (not cut off on the right); changing width / height / X / gap / "two stickers" updates it; the next print uses the new values. |
| 3.10e | Darkness slider above 16, or Speed "9 - Fastest", then print | Prints (values are capped to the printer's 15 / 8); no "BarcodePrinterIllegalArgumentException". |
| 3.10f | Move the USB printer to another USB port, then print | A clear "The USB printer could not be opened" message telling you to re-select it under Printer — not a stack trace, never "completed successfully". |
| 3.10g | Leave the Print page after generating a barcode (e.g. to Printer) and come back | The barcode is kept; Start printing is still enabled. |
| 3.10a | Printer switched off / unplugged, then Start printing | Status bar shows red "Print failed: the label printer could not be reached…" — never "completed successfully"; Start printing stays enabled to retry. |
| 3.10b | Generate barcode, go Back and change product or picker | Start printing greys out; status asks to generate again; the old barcode can never print with the new product. Changing only the count keeps the barcode. |
| 3.10c | Half-type a product name, then Next | Not allowed until a product is picked from the list. |
| 3.10d | After a successful print | Summary and preview update; Start printing reads "Start printing" (greyed until the next Generate). |
| 3.5a | Add combination, then look at the Product list | The new combination is there without leaving the page. |
| 3.12 | Preview card | Grey tray with a white label showing product, picker, barcode and serial. |

## 4. Scanners

Open from the sidebar (Scanners).

| # | Check | Expected |
|---|---|---|
| 4.1 | Page | "Scanners on this site"; table of detected scanners; red banner if one is not connected, with **Look again**. |
| 4.2 | Hover the Serial cell | Tooltip shows the full Device ID (the Device ID column is now hidden). |
| 4.3 | Status column | Green "Connected" / red "Not connected" badges (rows are no longer tinted). |
| 4.4 | Edit Line, Block, Supplier, then **Save assignments** | Green banner "Scanner assignments saved"; scanners reconnect; values persist after reopening. |
| 4.5 | Baud / Parity / Data / Stop | Click opens a drop-down; greyed and read-only for keyboard (HID) scanners. Saved correctly. |
| 4.6 | Trash icon on a row | "Remove this scanner?" dialog with its details. **Keep it** does nothing; **Remove scanner** removes and saves (green banner). |
| 4.7 | **Look for scanners** / Look again | Re-detects scanners. |
| 4.8 | **COM mode help** | Help dialog with the setup steps; **Got it** closes it. |
| 4.9 | **Show detection log** toggle | Shows/hides the detection log. |
| 4.10 | Small screen | Whole table fits without a horizontal scrollbar; status and trash visible. |
| 4.11 | Remove a scanner, then **Look for scanners** (and restart the app) | It stays removed (it used to come back). |
| 4.12 | Plug / unplug a scanner, go to another page and back to Scanners | The table and banner reflect the change without restarting. |
| 4.13 | PC with no scanners | "No scanners found yet" (no "Test Scanner" row, no "1 scanner isn't answering"); Save writes no fake scanner. |
| 4.14 | Baud / Parity / Data / Stop cell | One click opens the drop-down list. |
| 4.16 | Save assignments with scanners connected | No "COM port … already in use" error; scanners reconnect. |
| 4.17 | Plug a scanner in while on the Scans page, and wait | The app never freezes ("Not Responding"). |
| 4.18 | Edit Line/Block, leave the Scanners page without saving, come back | Your unsaved edits are still there (the page doesn't re-detect over them). |
| 4.15 | Esc on the Scanners or Printer page | Nothing happens (does not jump to Scans). |

## 5. Printer (connection)

| # | Check | Expected |
|---|---|---|
| 5.1 | Page / dialog | "How is the printer connected?" with Connection, Printer address and a Status badge. |
| 5.2 | Connection list | Shows Network (LAN), USB cable, Serial cable (COM), Save to a file, Several network printers (Multi-LAN). Selecting one updates address and status like before. |
| 5.3 | **Configure connection** | Opens the same configuration as before for the chosen type (folder picker, COM settings, USB device, IP). |
| 5.4 | Status badge | Green when configured, amber when USB not configured, blue for serial/network. |
| 5.5 | Close | Hides the dialog (or returns to Scans when used as a page); reopening keeps the settings. |
| 5.6 | USB cable → Configure connection | The printer picker opens **in front of** ScanLink. |
| 5.7 | Configure USB (or LAN / File), then **restart the app** and print | The connection is remembered; no "No USB device selected" error. |

## 6. Add product combination

| # | Check | Expected |
|---|---|---|
| 6.0 | Add combination (Print step 1) | Opens with **no error**; after Create the dialog closes, the status bar says "Combination created", and the new combination is in the Product list. |
| 6.1 | Open from Print step 1 | Six fields in two columns; each unlocks after the previous one is chosen. |
| 6.2 | "+ Add new grade / count / carton type" | Opens a ScanLink-styled prompt (not a grey Windows box); Cancel leaves the list unchanged. |
| 6.3 | Carton type | Asks for name, then empty carton weight. |
| 6.4 | **Create combination** | Disabled until all six are set and weight > 0; shows progress; closes on success. |
| 6.5 | Server error | Red banner inside the dialog with the message. |

## 7. Choose an employee

| # | Check | Expected |
|---|---|---|
| 7.1 | Open via Find picker | Loads employees and departments. |
| 7.2 | Search by name / ID, press Enter or **Search** | Filters the list. |
| 7.3 | Departments | Ticking departments filters results. |
| 7.4 | Previous / Next | Paging works; "Page x of y" and both buttons fully visible; total updates. |
| 7.5 | Double-click a row or **Select employee** | Fills "Who is picking?" and closes. |

## 8. Crops & products

| # | Check | Expected |
|---|---|---|
| 8.1 | Tabs: Combinations, Crops, Products, Varieties, Grades, Counts | Each shows its list; row count at the right is correct. |
| 8.2 | Crop / Variety filters (Combinations tab) | Choosing a crop narrows the variety list and the table. |
| 8.3 | No data loaded | "Nothing here yet" message instead of an empty grid. |

## 9. Error details

| # | Check | Expected |
|---|---|---|
| 9.1 | Trigger an error (e.g. sign in with the network disconnected, if it shows details) | Dialog with red title, details in a scrollable box, **OK** focused (text not pre-selected). |
| 9.2 | **Copy** | Copies the details; button shows "Copied" with a tick for ~1.5 s. |

---

## Known limitations (not bugs)

- A round blue button floating over the Scans date-range switch was seen in one screenshot. It is
  not part of ScanLink (nothing in the app draws it); if you see it, note which other apps were
  running (touch keyboard, screen-capture or remote-desktop tools).

- **No logo** on the login panel or sidebar: the client's white logo asset renders as a solid
  white block, so it is disabled until the UI team supplies a proper transparent white logo.
- Login has no "Keep me signed in" / "Forgot password?" (features the client does not have;
  sessions are already restored automatically).
- The mockup's Overview and People screens are not part of this release.
