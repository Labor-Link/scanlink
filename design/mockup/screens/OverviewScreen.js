(() => {
const { StatTile, Card, Badge, Banner, Button, Icon, DataTable, Toggle } = window.ScanLinkDesignSystem_c824b0;

const RECENT = [
  { serial: "SC-0093-AA", time: "07:14", block: "14", picker: "J. Mokoena", state: "Synced", tone: "success" },
  { serial: "SC-0093-AB", time: "07:15", block: "14", picker: "J. Mokoena", state: "Synced", tone: "success" },
  { serial: "SC-0094-AC", time: "07:16", block: "07", picker: "P. Naidoo", state: "Waiting", tone: "warning" },
  { serial: "SC-0094-AD", time: "07:17", block: "07", picker: "P. Naidoo", state: "Waiting", tone: "warning" }
];
const COLS = [
  { key: "serial", header: "Serial", mono: true },
  { key: "time", header: "Time", muted: true },
  { key: "block", header: "Block" },
  { key: "picker", header: "Picked by" },
  { key: "state", header: "Status", render: (r) => <Badge tone={r.tone} dot>{r.state}</Badge> }
];
const DEVICES = [
  { name: "Line 3 scanner", detail: "SL-HH-02 · COM3", state: "Working", tone: "success" },
  { name: "Line 1 scanner", detail: "SL-HH-05 · COM4", state: "Working", tone: "success" },
  { name: "Line 2 scanner", detail: "SL-HH-11 · COM7", state: "Offline", tone: "error" },
  { name: "Label printer", detail: "Argox · 192.168.1.44", state: "Working", tone: "success" }
];

function OverviewScreen({ onNavigate, onSync, isHome, onSetHome }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px", maxWidth: "1180px" }}>
      <Banner tone="warning" icon={<Icon name="cloud-off" />} title="42 scans haven't reached the cloud yet"
        action={<Button size="sm" variant="secondary" onClick={onSync}>Sync now</Button>}>
        They're safely saved on this computer. Sync them before you clean up local scans.
      </Banner>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,minmax(0,1fr))", gap: "12px" }}>
        <StatTile caption="Scanned today" value="1,284" delta="up 12% on yesterday" deltaTone="up" icon={<Icon name="scan-line" size={16} />} />
        <StatTile caption="Last hour" value="96" footnote="Steady with this morning." icon={<Icon name="clock" size={16} />} />
        <StatTile caption="Labels printed" value="310" footnote="Across 4 lines." icon={<Icon name="printer" size={16} />} />
        <StatTile caption="Waiting to sync" value="42" delta="sync when you're ready" deltaTone="neutral" icon={<Icon name="cloud-upload" size={16} />} />
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0,1.6fr) minmax(0,1fr)", gap: "16px", alignItems: "start" }}>
        <Card title="Latest scans" subtitle="The four most recent cartons scanned on this site." padding="0"
          actions={<Button size="sm" variant="ghost" iconEnd={<Icon name="arrow-right" size={16} />} onClick={() => onNavigate("scans")}>See all scans</Button>}>
          <DataTable rowKey="serial" columns={COLS} rows={RECENT} />
        </Card>

        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
          <Card title="Equipment" subtitle="Green means it's sending data right now."
            actions={<Button size="sm" variant="ghost" onClick={() => onNavigate("devices")}>Manage</Button>} padding="8px">
            <div style={{ display: "flex", flexDirection: "column" }}>
              {DEVICES.map((d) => (
                <div key={d.detail} style={{ display: "flex", alignItems: "center", gap: "12px", padding: "10px 12px" }}>
                  <span style={{ display: "flex", flexDirection: "column", gap: "1px", flex: 1, minWidth: 0 }}>
                    <span style={{ font: "500 var(--f-sm)/1.3 var(--font-ui)", color: "var(--text-body)" }}>{d.name}</span>
                    <span style={{ font: "400 var(--f-xs)/1.3 var(--font-mono)", color: "var(--text-muted)" }}>{d.detail}</span>
                  </span>
                  <Badge tone={d.tone} dot>{d.state}</Badge>
                </div>
              ))}
            </div>
          </Card>

          <Card title="Start here" subtitle="The two things most people do first.">
            <div style={{ display: "flex", flexDirection: "column", gap: "8px" }}>
              <Button variant="primary" block icon={<Icon name="printer" size={16} />} onClick={() => onNavigate("labels")}>Print labels</Button>
              <Button variant="secondary" block icon={<Icon name="scan-line" size={16} />} onClick={() => onNavigate("scans")}>Look up a scan</Button>
            </div>
            <div style={{ marginTop: "16px", paddingTop: "16px", borderTop: "1px solid var(--border-subtle)" }}>
              <Toggle label="Open ScanLink on this page" description="Otherwise ScanLink opens on Scans." checked={isHome} onChange={onSetHome} />
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { OverviewScreen });
})();
