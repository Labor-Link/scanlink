(() => {
const { useState } = React;
const { SiteTile, SearchField, Icon, EmptyState } = window.ScanLinkDesignSystem_c824b0;

const SITES = [
  { name: "Rooidraai Packhouse", id: "SL-2201", role: "Owner" },
  { name: "Kleinbos Cold Store", id: "SL-2208", role: "Employee" },
  { name: "Vaalkop Line 3", id: "SL-2214", role: "Employee" },
  { name: "Môrester Citrus", id: "SL-2231", role: "Employee" },
  { name: "Bergsig Avocado", id: "SL-2244", role: "Employee" }
];

function SiteSelectionScreen({ onSelect }) {
  const [q, setQ] = useState("");
  const hits = SITES.filter((s) => (s.name + s.id).toLowerCase().includes(q.toLowerCase()));
  return (
    <div style={{ height: "100%", overflowY: "auto", background: "var(--surface-app)", display: "grid", placeItems: "start center", padding: "56px 24px" }}>
      <div style={{ width: "100%", maxWidth: "680px", display: "flex", flexDirection: "column", gap: "24px" }}>
        <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
          <h1 style={{ margin: 0, font: "600 var(--f-3xl)/1.25 var(--font-ui)", color: "var(--text-heading)" }}>Which site are you working on?</h1>
          <p style={{ margin: 0, font: "400 var(--f-md)/1.5 var(--font-ui)", color: "var(--text-muted)" }}>You can switch site at any time from the bottom of the menu.</p>
        </div>
        <SearchField width="100%" icon={<Icon name="search" size={16} />} value={q}
          onChange={(e) => setQ(e.target.value)} onClear={() => setQ("")} placeholder="Search by site name or number…" />
        {hits.length === 0 ? (
          <EmptyState icon={<Icon name="search-x" size={22} />} title="No sites match that"
            description="Check the spelling, or clear the search to see all five sites." />
        ) : (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "12px" }}>
            {hits.map((s) => <SiteTile key={s.id} {...s} onClick={() => onSelect(s)} />)}
          </div>
        )}
      </div>
    </div>
  );
}

Object.assign(window, { SiteSelectionScreen });
})();
