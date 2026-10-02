(() => {
const { useState } = React;
const { Card, DataTable, Badge, Button, IconButton, Icon, Banner, Dialog, Select, TextField, FieldSet, EmptyState } = window.ScanLinkDesignSystem_c824b0;

const INITIAL = [
  { serial: "SL-HH-02", name: "Line 3 scanner", port: "COM3", line: "3", block: "14", supplier: "Rooidraai", state: "Working", tone: "success" },
  { serial: "SL-HH-05", name: "Line 1 scanner", port: "COM4", line: "1", block: "07", supplier: "Kleinbos", state: "Working", tone: "success" },
  { serial: "SL-HH-11", name: "Line 2 scanner", port: "COM7", line: "2", block: "21", supplier: "Vaalkop", state: "Offline", tone: "error" }
];

function DevicesScreen({ onStatus }) {
  const [rows, setRows] = useState(INITIAL);
  const [removing, setRemoving] = useState(null);
  const [printerOpen, setPrinterOpen] = useState(false);
  const [scanning, setScanning] = useState(false);

  const cols = [
    { key: "name", header: "Scanner", render: (r) => (
      <span style={{ display: "flex", flexDirection: "column", gap: "1px" }}>
        <span style={{ font: "500 var(--f-sm)/1.3 var(--font-ui)" }}>{r.name}</span>
        <span style={{ font: "400 var(--f-xs)/1.3 var(--font-mono)", color: "var(--text-muted)" }}>{r.serial} · {r.port}</span>
      </span>) },
    { key: "line", header: "Line" },
    { key: "block", header: "Block" },
    { key: "supplier", header: "Supplier" },
    { key: "state", header: "Status", render: (r) => <Badge tone={r.tone} dot>{r.state}</Badge> },
    { key: "act", header: "", align: "right", render: (r) => (
      <span style={{ display: "inline-flex", gap: "4px" }}>
        <IconButton size="sm" icon={<Icon name="pencil" size={16} />} label={`Edit ${r.name}`} />
        <IconButton size="sm" variant="danger" icon={<Icon name="trash-2" size={16} />} label={`Remove ${r.name}`} onClick={() => setRemoving(r)} />
      </span>) }
  ];

  const rescan = () => {
    setScanning(true);
    onStatus("Looking for scanners…", "info");
    setTimeout(() => { setScanning(false); onStatus("Found 3 scanners. One isn't answering.", "warning"); }, 900);
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px", maxWidth: "1080px" }}>
      <Banner tone="error" icon={<Icon name="unplug" />} title="Line 2 scanner isn't answering"
        action={<Button size="sm" variant="secondary" onClick={rescan}>Look again</Button>}>
        Check that it's plugged in and switched on, then look again. Scans already saved are not affected.
      </Banner>

      <Card title="Scanners on this site" subtitle="Each scanner is tied to a line and a block so scans land in the right place."
        padding="0"
        actions={<>
          <Button size="sm" variant="secondary" loading={scanning} icon={<Icon name="refresh-cw" size={16} />} onClick={rescan}>Look for scanners</Button>
          <Button size="sm" variant="primary" icon={<Icon name="plus" size={16} />}>Add scanner</Button>
        </>}>
        <DataTable rowKey="serial" columns={cols} rows={rows}
          empty={<EmptyState icon={<Icon name="usb" size={22} />} title="No scanners found yet"
            description="Plug a scanner into this computer, then choose Look for scanners."
            action={<Button variant="secondary" size="sm" onClick={rescan}>Look for scanners</Button>} />} />
      </Card>

      <Card title="Label printer" subtitle="ScanLink sends every label here."
        actions={<Button size="sm" variant="secondary" onClick={() => setPrinterOpen(true)}>Change connection</Button>}>
        <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
          <span style={{ width: "42px", height: "42px", borderRadius: "var(--r-md)", background: "var(--indigo-50)", color: "var(--indigo-500)", display: "grid", placeItems: "center" }}><Icon name="printer" size={20} /></span>
          <span style={{ display: "flex", flexDirection: "column", gap: "2px", flex: 1 }}>
            <span style={{ font: "600 var(--f-md)/1.3 var(--font-ui)", color: "var(--text-heading)" }}>Argox thermal printer</span>
            <span style={{ font: "400 var(--f-sm)/1.3 var(--font-mono)", color: "var(--text-muted)" }}>Network · 192.168.1.44:9100</span>
          </span>
          <Badge tone="success" dot>Working</Badge>
        </div>
      </Card>

      {removing ? (
        <Dialog title="Remove this scanner?" tone="danger" width="440px"
          description={`${removing.name} (${removing.serial}) will stop sending scans to this site. You can add it again later.`}
          onClose={() => setRemoving(null)}
          actions={<>
            <Button variant="secondary" onClick={() => setRemoving(null)}>Keep it</Button>
            <Button variant="danger" onClick={() => { setRows(rows.filter((r) => r.serial !== removing.serial)); onStatus(`${removing.name} removed`, "warning"); setRemoving(null); }}>Remove scanner</Button>
          </>} />
      ) : null}

      {printerOpen ? (
        <Dialog title="How is the printer connected?" width="480px"
          description="Choose how this computer reaches the label printer. If you're not sure, ask whoever set it up."
          onClose={() => setPrinterOpen(false)}
          actions={<>
            <Button variant="secondary" onClick={() => setPrinterOpen(false)}>Cancel</Button>
            <Button variant="primary" onClick={() => { setPrinterOpen(false); onStatus("Printer connection saved", "success"); }}>Save connection</Button>
          </>}>
          <FieldSet columns={1}>
            <Select label="Connection" defaultValue="Network (LAN)" hint="Network is the usual choice in a packhouse."
              options={["Network (LAN)", "USB cable", "Serial cable (COM)", "Save to a file"]} />
            <TextField label="Printer address" mono defaultValue="192.168.1.44:9100" hint="You'll find this printed on the label on the printer." />
          </FieldSet>
        </Dialog>
      ) : null}
    </div>
  );
}

Object.assign(window, { DevicesScreen });
})();
