try { (() => {
/** Shown instead of an empty table. Always says what happened and what to do next. */
function EmptyState({
  icon,
  title,
  description,
  action,
  compact,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      alignItems: "center",
      justifyContent: "center",
      textAlign: "center",
      gap: "8px",
      padding: compact ? "32px 20px" : "56px 24px",
      ...style
    }
  }, icon ? /*#__PURE__*/React.createElement("span", {
    style: {
      width: "44px",
      height: "44px",
      borderRadius: "var(--r-lg)",
      background: "var(--indigo-50)",
      color: "var(--indigo-500)",
      display: "grid",
      placeItems: "center",
      marginBottom: "4px"
    }
  }, icon) : null, /*#__PURE__*/React.createElement("h3", {
    style: {
      margin: 0,
      font: "600 var(--f-md)/1.3 var(--font-ui)",
      color: "var(--text-heading)"
    }
  }, title), description ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      maxWidth: "380px",
      font: "400 var(--f-sm)/1.6 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, description) : null, action ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: "8px"
    }
  }, action) : null);
}
Object.assign(__ds_scope, { EmptyState });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/layout/EmptyState.jsx", error: String((e && e.message) || e) }); }
