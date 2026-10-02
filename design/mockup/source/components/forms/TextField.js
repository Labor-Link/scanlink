try { (() => {
function FieldLabel({
  children,
  required,
  hint,
  htmlFor
}) {
  return /*#__PURE__*/React.createElement("span", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px",
      marginBottom: "6px"
    }
  }, /*#__PURE__*/React.createElement("label", {
    htmlFor: htmlFor,
    style: {
      font: "500 var(--f-sm)/1.2 var(--font-ui)",
      color: "var(--text-label)"
    }
  }, children, required ? /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--err-500)",
      marginLeft: "2px"
    }
  }, "*") : null), hint ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, hint) : null);
}
function fieldBox({
  focus,
  invalid,
  disabled,
  size = "md"
}) {
  return {
    width: "100%",
    boxSizing: "border-box",
    height: size === "sm" ? "var(--h-sm)" : size === "lg" ? "var(--h-lg)" : "var(--h-md)",
    padding: "0 12px",
    background: disabled ? "var(--n-50)" : "var(--n-0)",
    color: disabled ? "var(--n-400)" : "var(--text-body)",
    border: `1px solid ${invalid ? "var(--err-500)" : focus ? "var(--action-primary)" : "var(--border-strong)"}`,
    borderRadius: "var(--r-sm)",
    outline: "none",
    boxShadow: focus ? "var(--ring-focus)" : "none",
    font: `400 var(--f-md)/1 var(--font-ui)`,
    transition: "border-color var(--t-fast), box-shadow var(--t-fast)"
  };
}
function TextField({
  label,
  required,
  hint,
  error,
  value,
  defaultValue,
  placeholder,
  onChange,
  onKeyDown,
  width = "100%",
  size = "md",
  type = "text",
  readOnly,
  disabled,
  mono,
  prefix,
  suffix,
  id,
  style
}) {
  const [f, setF] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width,
      ...style
    }
  }, label ? /*#__PURE__*/React.createElement(FieldLabel, {
    htmlFor: id,
    required: required,
    hint: hint
  }, label) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      position: "relative",
      display: "flex",
      alignItems: "center"
    }
  }, prefix ? /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      left: "12px",
      display: "flex",
      color: "var(--n-400)",
      pointerEvents: "none"
    }
  }, prefix) : null, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: type,
    value: value,
    defaultValue: defaultValue,
    placeholder: placeholder,
    onChange: onChange,
    onKeyDown: onKeyDown,
    readOnly: readOnly,
    disabled: disabled,
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: {
      ...fieldBox({
        focus: f,
        invalid: !!error,
        disabled: disabled || readOnly,
        size
      }),
      paddingLeft: prefix ? "38px" : "12px",
      paddingRight: suffix ? "40px" : "12px",
      fontFamily: mono ? "var(--font-mono)" : "var(--font-ui)"
    }
  }), suffix ? /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      right: "8px",
      display: "flex"
    }
  }, suffix) : null), error ? /*#__PURE__*/React.createElement("span", {
    style: {
      display: "block",
      marginTop: "6px",
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: "var(--err-700)"
    }
  }, error) : null);
}
Object.assign(__ds_scope, { FieldLabel, fieldBox, TextField });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/TextField.jsx", error: String((e && e.message) || e) }); }
