try { (() => {
function NumberField({
  label,
  required,
  hint,
  value,
  defaultValue,
  onChange,
  min,
  max,
  step = 1,
  unit,
  width = "160px",
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
  }, label) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      position: "relative",
      display: "flex",
      alignItems: "center"
    }
  }, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: "number",
    value: value,
    defaultValue: defaultValue,
    onChange: onChange,
    min: min,
    max: max,
    step: step,
    disabled: disabled,
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: {
      ...__ds_scope.fieldBox({
        focus: f,
        disabled
      }),
      paddingRight: unit ? "44px" : "12px"
    }
  }), unit ? /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      right: "12px",
      font: "400 var(--f-sm)/1 var(--font-ui)",
      color: "var(--text-muted)",
      pointerEvents: "none"
    }
  }, unit) : null));
}
Object.assign(__ds_scope, { NumberField });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/NumberField.jsx", error: String((e && e.message) || e) }); }
