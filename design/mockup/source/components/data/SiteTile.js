try { (() => {
/** Selectable site card — used in the site picker and in the sidebar footer. */
function SiteTile({
  name,
  id,
  role = "Employee",
  active,
  compact,
  onClick,
  style
}) {
  const [h, setH] = React.useState(false);
  const dark = compact;
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onClick,
    onMouseEnter: () => setH(true),
    onMouseLeave: () => setH(false),
    style: {
      display: "flex",
      alignItems: "center",
      gap: "12px",
      width: "100%",
      textAlign: "left",
      padding: compact ? "10px" : "16px",
      boxSizing: "border-box",
      background: dark ? h ? "rgba(255,255,255,.07)" : "transparent" : "var(--surface-card)",
      border: dark ? "1px solid transparent" : `1px solid ${active ? "var(--indigo-500)" : h ? "var(--border-strong)" : "var(--border-default)"}`,
      borderRadius: "var(--r-md)",
      boxShadow: dark ? "none" : h || active ? "var(--e-2)" : "var(--e-1)",
      cursor: "pointer",
      transition: "all var(--t-fast)",
      ...style
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: compact ? "32px" : "40px",
      height: compact ? "32px" : "40px",
      flex: "none",
      borderRadius: "var(--r-md)",
      display: "grid",
      placeItems: "center",
      background: dark ? "rgba(255,255,255,.10)" : "var(--indigo-50)",
      color: dark ? "#fff" : "var(--indigo-500)",
      font: `600 ${compact ? "var(--f-sm)" : "var(--f-md)"}/1 var(--font-ui)`
    }
  }, String(name || "?").slice(0, 2).toUpperCase()), /*#__PURE__*/React.createElement("span", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px",
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: "600 var(--f-md)/1.3 var(--font-ui)",
      color: dark ? "#fff" : "var(--text-heading)",
      overflow: "hidden",
      textOverflow: "ellipsis",
      whiteSpace: "nowrap"
    }
  }, name), /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.3 var(--font-ui)",
      color: dark ? "rgba(255,255,255,.55)" : "var(--text-muted)"
    }
  }, role, " \xB7 ", id)), !compact && active ? /*#__PURE__*/React.createElement(__ds_scope.Badge, {
    tone: "brand"
  }, "Current") : null);
}
Object.assign(__ds_scope, { SiteTile });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/SiteTile.jsx", error: String((e && e.message) || e) }); }
