try { (() => {
/** On/off switch for settings that take effect immediately. */
function Toggle({
  label,
  description,
  checked,
  onChange,
  disabled,
  id,
  style
}) {
  return /*#__PURE__*/React.createElement("label", {
    htmlFor: id,
    style: {
      display: "flex",
      alignItems: "center",
      gap: "12px",
      cursor: disabled ? "not-allowed" : "pointer",
      ...style
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: "40px",
      height: "22px",
      flex: "none",
      borderRadius: "var(--r-pill)",
      position: "relative",
      background: disabled ? "var(--n-200)" : checked ? "var(--action-primary)" : "var(--n-300)",
      transition: "background var(--t-fast)"
    }
  }, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: "checkbox",
    checked: !!checked,
    onChange: onChange,
    disabled: disabled,
    style: {
      position: "absolute",
      opacity: 0,
      width: "100%",
      height: "100%",
      margin: 0,
      cursor: "inherit"
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      top: "3px",
      left: checked ? "21px" : "3px",
      width: "16px",
      height: "16px",
      borderRadius: "50%",
      background: "#fff",
      boxShadow: "var(--e-1)",
      transition: "left var(--t-fast)"
    }
  })), label ? /*#__PURE__*/React.createElement("span", {
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
  }, description) : null) : null);
}
Object.assign(__ds_scope, { Toggle });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Toggle.jsx", error: String((e && e.message) || e) }); }
