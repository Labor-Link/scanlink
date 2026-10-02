try { (() => {
/** A labelled block of related fields inside a Card or a settings page. */
function FieldSet({
  title,
  description,
  columns = 2,
  children,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "12px",
      ...style
    }
  }, title ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px"
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      margin: 0,
      font: "600 var(--f-md)/1.3 var(--font-ui)",
      color: "var(--text-heading)"
    }
  }, title), description ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      font: "400 var(--f-sm)/1.5 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, description) : null) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "grid",
      gridTemplateColumns: `repeat(${columns}, minmax(0,1fr))`,
      gap: "16px",
      alignItems: "start"
    }
  }, children));
}
Object.assign(__ds_scope, { FieldSet });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/layout/FieldSet.jsx", error: String((e && e.message) || e) }); }
