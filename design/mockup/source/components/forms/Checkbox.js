try { (() => {
function Checkbox({
  label,
  description,
  checked,
  defaultChecked,
  onChange,
  disabled,
  id,
  style
}) {
  return /*#__PURE__*/React.createElement("label", {
    htmlFor: id,
    style: {
      display: "flex",
      alignItems: "flex-start",
      gap: "10px",
      cursor: disabled ? "not-allowed" : "pointer",
      ...style
    }
  }, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: "checkbox",
    checked: checked,
    defaultChecked: defaultChecked,
    onChange: onChange,
    disabled: disabled,
    style: {
      width: "16px",
      height: "16px",
      marginTop: "1px",
      accentColor: "var(--action-primary)",
      flex: "none",
      cursor: "inherit"
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px"
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: "500 var(--f-md)/1.3 var(--font-ui)",
      color: disabled ? "var(--n-400)" : "var(--text-body)"
    }
  }, label), description ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, description) : null));
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }
