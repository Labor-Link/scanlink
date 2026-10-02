try { (() => {
function SidebarItem({
  icon,
  label,
  hint,
  active,
  badge,
  collapsed,
  onClick
}) {
  const [h, setH] = React.useState(false);
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onClick,
    title: collapsed ? label : undefined,
    onMouseEnter: () => setH(true),
    onMouseLeave: () => setH(false),
    style: {
      display: "flex",
      alignItems: "center",
      gap: "12px",
      width: "100%",
      padding: collapsed ? "10px 0" : "9px 12px",
      justifyContent: collapsed ? "center" : "flex-start",
      background: active ? "var(--surface-shell-active)" : h ? "var(--surface-shell-hover)" : "transparent",
      color: active ? "var(--text-on-shell-strong)" : h ? "var(--text-on-shell-strong)" : "var(--text-on-shell)",
      border: "none",
      borderRadius: "var(--r-sm)",
      cursor: "pointer",
      textAlign: "left",
      font: `${active ? 600 : 500} var(--f-md)/1.2 var(--font-ui)`,
      transition: "background var(--t-fast), color var(--t-fast)"
    }
  }, icon, collapsed ? null : /*#__PURE__*/React.createElement("span", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px",
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      overflow: "hidden",
      textOverflow: "ellipsis",
      whiteSpace: "nowrap"
    }
  }, label), hint ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.2 var(--font-ui)",
      color: "rgba(255,255,255,.45)"
    }
  }, hint) : null), !collapsed && badge != null ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "600 var(--f-xs)/1 var(--font-ui)",
      background: "rgba(255,255,255,.16)",
      color: "#fff",
      padding: "3px 7px",
      borderRadius: "var(--r-pill)"
    }
  }, badge) : null);
}
Object.assign(__ds_scope, { SidebarItem });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/SidebarItem.jsx", error: String((e && e.message) || e) }); }
