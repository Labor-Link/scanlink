(() => {
const { Sidebar, TopBar, StatusBar, SiteTile, Icon, IconButton } = window.ScanLinkDesignSystem_c824b0;

const NAV = [
  { items: [{ key: "overview", icon: "layout-dashboard", label: "Overview" }] },
  { title: "Daily work", items: [
    { key: "scans", icon: "scan-line", label: "Scans", badge: "live" },
    { key: "labels", icon: "printer", label: "Print labels" }
  ] },
  { title: "Setup", items: [
    { key: "devices", icon: "usb", label: "Scanners & printer" },
    { key: "products", icon: "database", label: "Crops & products" },
    { key: "people", icon: "users", label: "People" }
  ] }
];

function AppShell({ site, active, onNavigate, onSwitchSite, onSignOut, title, subtitle, actions, status, statusTone, statusRight, children }) {
  const groups = NAV.map((g) => ({
    ...g,
    items: g.items.map((it) => ({ ...it, icon: <Icon name={it.icon} size={20} /> }))
  }));
  return (
    <div style={{ height: "100%", display: "flex", background: "var(--surface-app)" }}>
      <Sidebar logoSrc="../../assets/scanlink-logo-white.png" groups={groups} activeKey={active} onSelect={onNavigate}
        footer={
          <div style={{ display: "flex", flexDirection: "column", gap: "4px" }}>
            <SiteTile compact name={site.name} id={site.id} role={site.role} onClick={onSwitchSite} />
            <button type="button" onClick={onSignOut}
              style={{ display: "flex", alignItems: "center", gap: "12px", padding: "9px 12px", background: "transparent", border: "none", borderRadius: "var(--r-sm)", color: "rgba(255,255,255,.55)", font: "500 var(--f-sm)/1 var(--font-ui)", cursor: "pointer" }}>
              <Icon name="log-out" size={18} />Sign out
            </button>
          </div>
        } />
      <div style={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
        <TopBar title={title} subtitle={subtitle} actions={actions} />
        <main style={{ flex: 1, minHeight: 0, overflowY: "auto", padding: "24px" }}>{children}</main>
        <StatusBar tone={statusTone} right={statusRight}>{status}</StatusBar>
      </div>
    </div>
  );
}

Object.assign(window, { AppShell });
})();
