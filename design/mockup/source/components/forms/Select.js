try { (() => {
function Select({
  label,
  required,
  hint,
  error,
  options = [],
  value,
  defaultValue,
  onChange,
  width = "100%",
  size = "md",
  disabled,
  id,
  style
}) {
  const [f, setF] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width,
      ...style
    }
  }, label ? /*#__PURE__*/React.createElement(__ds_scope.FieldLabel, {
    htmlFor: id,
    required: required,
    hint: hint
  }, label) : null, /*#__PURE__*/React.createElement("select", {
    id: id,
    value: value,
    defaultValue: defaultValue,
    onChange: onChange,
    disabled: disabled,
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: {
      ...__ds_scope.fieldBox({
        focus: f,
        invalid: !!error,
        disabled,
        size
      }),
      appearance: "auto",
      cursor: disabled ? "not-allowed" : "pointer"
    }
  }, options.map(o => {
    const v = typeof o === "string" ? o : o.value;
    const l = typeof o === "string" ? o : o.label;
    return /*#__PURE__*/React.createElement("option", {
      key: v,
      value: v
    }, l);
  })), error ? /*#__PURE__*/React.createElement("span", {
    style: {
      display: "block",
      marginTop: "6px",
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: "var(--err-700)"
    }
  }, error) : null);
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }
