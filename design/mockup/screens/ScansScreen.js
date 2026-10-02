(() => {
const { useState, useMemo } = React;
const { Card, DataTable, Pagination, Badge, Button, SearchField, Select, DateField, Icon, EmptyState, Dialog } = window.ScanLinkDesignSystem_c824b0;

const PICKERS = ["J. Mokoena", "P. Naidoo", "T. van Wyk", "A. Dlamini"];
const SUPPLIERS = ["Rooidraai", "Kleinbos", "Vaalkop", "Môrester"];
const SCANS = Array.from({ length: 46 }, (_, i) => {
  const citrus = i % 2 === 1;
  return {
    serial: `SC-${(93 + i).toString().padStart(4, "0")}-A${String.fromCharCode(65 + (i % 26))}`,
    time: `07:${String(14 + (i % 45)).padStart(2, "0")}`,
    block: String(7 + (i % 3) * 7).padStart(2, "0"),
    line: String(1 + (i % 3)),
    picker: PICKERS[i % 4],
    supplier: SUPPLIERS[i % 4],
    product: citrus ? "Valencia 88" : "Hass Loose",
    state: i % 7 === 3 ? "Waiting" : "Synced",
    tone: i % 7 === 3 ? "warning" : "success"
  };
});

const RANGES = ["Today", "Last 7 days", "This season", "Custom"];

function ScansScreen({ onStatus }) {
  const [q, setQ] = useState("");
  const [range, setRange] = useState("Today");
  const [crop, setCrop] = useState("All crops");
  const [page, setPage] = useState(1);
  const [row, setRow] = useState(null);
  const size = 12;

  const filtered = useMemo(() => SCANS.filter((s) => {
    const t = `${s.serial} ${s.block} ${s.supplier} ${s.picker} ${s.product}`.toLowerCase();
    const okQ = t.includes(q.toLowerCase());
    const okC = crop === "All crops" || (crop === "Avocado" ? s.product === "Hass Loose" : s.product === "Valencia 88");
    return okQ && okC;
  }), [q, crop]);

  const pageCount = Math.max(1, Math.ceil(filtered.length / size));
  const rows = filtered.slice((page - 1) * size, page * size);

  const cols = [
    { key: "serial", header: "Serial", mono: true },
    { key: "time", header: "Time", muted: true },
    { key: "block", header: "Block" },
    { key: "line", header: "Line" },
    { key: "picker", header: "Picked by" },
    { key: "supplier", header: "Supplier" },
    { key: "product", header: "Product" },
    { key: "state", header: "Status", render: (r) => <Badge tone={r.tone} dot>{r.state}</Badge> }
  ];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px", maxWidth: "1180px" }}>
      <Card padding="14px 16px">
        <div style={{ display: "flex", alignItems: "center", gap: "12px", flexWrap: "wrap" }}>
          <SearchField width="300px" icon={<Icon name="search" size={16} />} value={q}
            onChange={(e) => { setQ(e.target.value); setPage(1); }} onClear={() => setQ("")}
            placeholder="Search serial, block, supplier or picker…" />
          <div style={{ display: "flex", gap: "4px", padding: "3px", background: "var(--n-100)", borderRadius: "var(--r-sm)" }}>
            {RANGES.map((r) => (
              <button key={r} type="button" onClick={() => setRange(r)}
                style={{ padding: "6px 12px", border: "none", borderRadius: "var(--r-xs)", cursor: "pointer",
                  background: range === r ? "var(--surface-card)" : "transparent",
                  boxShadow: range === r ? "var(--e-1)" : "none",
                  color: range === r ? "var(--text-heading)" : "var(--text-muted)",
                  font: `${range === r ? 600 : 500} var(--f-sm)/1 var(--font-ui)` }}>{r}</button>
            ))}
          </div>
          {range === "Custom" ? (
            <div style={{ display: "flex", gap: "8px" }}>
              <DateField defaultValue="2026-08-01" width="150px" />
              <DateField defaultValue="2026-08-10" width="150px" />
            </div>
          ) : null}
          <Select width="160px" value={crop} onChange={(e) => { setCrop(e.target.value); setPage(1); }}
            options={["All crops", "Avocado", "Citrus"]} />
          <span style={{ marginLeft: "auto", font: "400 var(--f-sm)/1 var(--font-ui)", color: "var(--text-muted)" }}>
            {filtered.length} scan{filtered.length === 1 ? "" : "s"}
          </span>
        </div>
      </Card>

      <Card padding="0" footer={<Pagination page={page} pageCount={pageCount} total={filtered.length} pageSize={size}
        onPrevious={() => setPage((p) => Math.max(1, p - 1))} onNext={() => setPage((p) => Math.min(pageCount, p + 1))} />}>
        <DataTable rowKey="serial" columns={cols} rows={rows} selectedId={row && row.serial}
          onRowClick={(r) => { setRow(r); onStatus(`Looking at ${r.serial}`, "info"); }}
          empty={<EmptyState icon={<Icon name="search-x" size={22} />} title="No scans match that search"
            description="Try a shorter search, or widen the date range to Last 7 days."
            action={<Button variant="secondary" size="sm" onClick={() => { setQ(""); setCrop("All crops"); }}>Clear filters</Button>} />} />
      </Card>

      {row ? (
        <Dialog title={row.serial} description="Everything recorded when this carton was scanned."
          onClose={() => setRow(null)} width="460px"
          actions={<><Button variant="secondary" onClick={() => setRow(null)}>Close</Button>
            <Button variant="primary" icon={<Icon name="printer" size={16} />}>Reprint this label</Button></>}>
          <div style={{ display: "grid", gridTemplateColumns: "auto 1fr", gap: "10px 20px" }}>
            {[["Time", row.time], ["Block", row.block], ["Line", row.line], ["Picked by", row.picker], ["Supplier", row.supplier], ["Product", row.product]].map(([k, v]) => (
              <React.Fragment key={k}>
                <span style={{ font: "400 var(--f-sm)/1.4 var(--font-ui)", color: "var(--text-muted)" }}>{k}</span>
                <span style={{ font: "500 var(--f-sm)/1.4 var(--font-ui)", color: "var(--text-body)" }}>{v}</span>
              </React.Fragment>
            ))}
          </div>
          <Badge tone={row.tone} dot>{row.state === "Synced" ? "Sent to the cloud" : "Waiting to be sent"}</Badge>
        </Dialog>
      ) : null}
    </div>
  );
}

Object.assign(window, { ScansScreen });
})();
