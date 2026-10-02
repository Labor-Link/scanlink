try { (() => {
/** Navy application rail: logo, grouped destinations, account block pinned to the bottom. */
function Sidebar({
  logoSrc,
  groups = [],
  activeKey,
  onSelect,
  collapsed = false,
  footer,
  style
}) {
  return /*#__PURE__*/React.createElement("nav", {
    style: {
      width: collapsed ? "68px" : "var(--sidebar-w)",
      flex: "none",
      height: "100%",
      boxSizing: "border-box",
      background: "var(--surface-shell)",
      display: "flex",
      flexDirection: "column",
      padding: "16px 12px",
      gap: "20px",
      transition: "width var(--t-med)",
      overflow: "hidden",
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      padding: collapsed ? 0 : "0 4px",
      display: "flex",
      justifyContent: collapsed ? "center" : "flex-start"
    }
  }, logoSrc ? /*#__PURE__*/React.createElement("img", {
    src: logoSrc,
    alt: "ScanLink",
    style: {
      height: collapsed ? "20px" : "24px",
      width: "auto"
    }
  }) : null), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "18px",
      flex: 1,
      minHeight: 0,
      overflowY: "auto"
    }
  }, groups.map((g, gi) => /*#__PURE__*/React.createElement("div", {
    key: g.title || gi,
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px"
    }
  }, g.title && !collapsed ? /*#__PURE__*/React.createElement("div", {
    style: {
      font: "600 var(--f-xs)/1 var(--font-ui)",
      letterSpacing: ".08em",
      textTransform: "uppercase",
      color: "rgba(255,255,255,.38)",
      padding: "6px 12px 8px"
    }
  }, g.title) : null, g.items.map(it => /*#__PURE__*/React.createElement(__ds_scope.SidebarItem, {
    key: it.key,
    icon: it.icon,
    label: it.label,
    hint: it.hint,
    badge: it.badge,
    active: activeKey === it.key,
    collapsed: collapsed,
    onClick: () => onSelect && onSelect(it.key)
  }))))), footer ? /*#__PURE__*/React.createElement("div", {
    style: {
      borderTop: "1px solid rgba(255,255,255,.10)",
      paddingTop: "12px"
    }
  }, footer) : null);
}
Object.assign(__ds_scope, { Sidebar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/Sidebar.jsx", error: String((e && e.message) || e) }); }
