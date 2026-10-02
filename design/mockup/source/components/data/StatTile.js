try { (() => {
/** Headline metric. Caption is a plain question or noun; delta explains movement in words. */
function StatTile({
  caption,
  value,
  unit,
  delta,
  deltaTone = "neutral",
  icon,
  footnote,
  style
}) {
  const tone = {
    up: "var(--ok-700)",
    down: "var(--err-700)",
    neutral: "var(--text-muted)"
  }[deltaTone];
  return /*#__PURE__*/React.createElement("div", {
    style: {
      background: "var(--surface-card)",
      border: "1px solid var(--border-default)",
      borderRadius: "var(--r-lg)",
      boxShadow: "var(--e-1)",
      padding: "16px 18px",
      display: "flex",
      flexDirection: "column",
      gap: "8px",
      minWidth: 0,
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: "8px"
    }
  }, icon ? /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--indigo-500)",
      display: "flex"
    }
  }, icon) : null, /*#__PURE__*/React.createElement("span", {
    style: {
      font: "500 var(--f-sm)/1.3 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, caption)), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "baseline",
      gap: "6px"
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: "600 var(--f-4xl)/1 var(--font-ui)",
      color: "var(--text-heading)",
      fontVariantNumeric: "tabular-nums"
    }
  }, value), unit ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "500 var(--f-sm)/1 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, unit) : null), delta || footnote ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: delta ? tone : "var(--text-muted)"
    }
  }, delta || footnote) : null);
}
Object.assign(__ds_scope, { StatTile });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/StatTile.jsx", error: String((e && e.message) || e) }); }
