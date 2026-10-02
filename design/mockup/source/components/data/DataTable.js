try { (() => {
function DataTable({
  columns = [],
  rows = [],
  selectedId,
  rowKey = "id",
  onRowClick,
  empty,
  maxHeight,
  style
}) {
  const [hover, setHover] = React.useState(null);
  if (rows.length === 0 && empty) return /*#__PURE__*/React.createElement("div", {
    style: style
  }, empty);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      overflow: "auto",
      maxHeight,
      ...style
    }
  }, /*#__PURE__*/React.createElement("table", {
    style: {
      width: "100%",
      borderCollapse: "separate",
      borderSpacing: 0,
      font: "400 var(--f-sm)/1.4 var(--font-ui)",
      color: "var(--text-body)"
    }
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, columns.map(c => /*#__PURE__*/React.createElement("th", {
    key: c.key,
    style: {
      position: "sticky",
      top: 0,
      zIndex: 1,
      textAlign: c.align || "left",
      padding: "10px 16px",
      background: "var(--n-50)",
      color: "var(--text-muted)",
      font: "600 var(--f-xs)/1.4 var(--font-ui)",
      letterSpacing: ".03em",
      textTransform: "uppercase",
      borderBottom: "1px solid var(--border-default)",
      whiteSpace: "nowrap",
      width: c.width
    }
  }, c.header)))), /*#__PURE__*/React.createElement("tbody", null, rows.length === 0 ? /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("td", {
    colSpan: columns.length,
    style: {
      padding: 0
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.EmptyState, {
    compact: true,
    title: "Nothing here yet",
    description: "Scans will appear as soon as they come in."
  }))) : rows.map((r, i) => {
    const id = r[rowKey] ?? i;
    const sel = selectedId != null && selectedId === id;
    return /*#__PURE__*/React.createElement("tr", {
      key: id,
      onClick: onRowClick ? () => onRowClick(r, i) : undefined,
      onMouseEnter: () => setHover(id),
      onMouseLeave: () => setHover(null),
      style: {
        cursor: onRowClick ? "pointer" : "default",
        background: sel ? "var(--indigo-50)" : hover === id ? "var(--n-50)" : "var(--surface-card)",
        transition: "background var(--t-fast)"
      }
    }, columns.map(c => /*#__PURE__*/React.createElement("td", {
      key: c.key,
      style: {
        padding: "12px 16px",
        textAlign: c.align || "left",
        borderBottom: "1px solid var(--border-subtle)",
        fontFamily: c.mono ? "var(--font-mono)" : "var(--font-ui)",
        fontVariantNumeric: c.mono || c.align === "right" ? "tabular-nums" : "normal",
        color: c.muted ? "var(--text-muted)" : "inherit",
        whiteSpace: c.wrap ? "normal" : "nowrap"
      }
    }, c.render ? c.render(r) : r[c.key])));
  }))));
}
Object.assign(__ds_scope, { DataTable });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/DataTable.jsx", error: String((e && e.message) || e) }); }
