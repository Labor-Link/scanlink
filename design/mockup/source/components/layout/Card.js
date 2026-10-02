try { (() => {
/** The redesign's primary container: white, 1px border, 12px radius, soft elevation. */
function Card({
  title,
  subtitle,
  actions,
  footer,
  padding = "20px",
  children,
  style,
  bodyStyle
}) {
  return /*#__PURE__*/React.createElement("section", {
    style: {
      background: "var(--surface-card)",
      border: "1px solid var(--border-default)",
      borderRadius: "var(--r-lg)",
      boxShadow: "var(--e-1)",
      overflow: "hidden",
      display: "flex",
      flexDirection: "column",
      minWidth: 0,
      ...style
    }
  }, title || actions ? /*#__PURE__*/React.createElement("header", {
    style: {
      display: "flex",
      alignItems: "flex-start",
      gap: "16px",
      padding: "16px 20px",
      borderBottom: "1px solid var(--border-subtle)"
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px",
      flex: 1,
      minWidth: 0
    }
  }, title ? /*#__PURE__*/React.createElement("h2", {
    style: {
      margin: 0,
      font: "600 var(--f-lg)/1.3 var(--font-ui)",
      color: "var(--text-heading)"
    }
  }, title) : null, subtitle ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      font: "400 var(--f-sm)/1.5 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, subtitle) : null), actions ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: "8px",
      flex: "none"
    }
  }, actions) : null) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      padding,
      flex: 1,
      minWidth: 0,
      ...bodyStyle
    }
  }, children), footer ? /*#__PURE__*/React.createElement("footer", {
    style: {
      padding: "12px 20px",
      borderTop: "1px solid var(--border-subtle)",
      background: "var(--n-25)"
    }
  }, footer) : null);
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/layout/Card.jsx", error: String((e && e.message) || e) }); }
