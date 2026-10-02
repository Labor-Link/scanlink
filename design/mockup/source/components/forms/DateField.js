try { (() => {
function DateField({
  label,
  hint,
  value,
  defaultValue,
  onChange,
  width = "170px",
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
    hint: hint
  }, label) : null, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: "date",
    value: value,
    defaultValue: defaultValue,
    onChange: onChange,
    disabled: disabled,
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: __ds_scope.fieldBox({
      focus: f,
      disabled
    })
  }));
}
Object.assign(__ds_scope, { DateField });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/DateField.jsx", error: String((e && e.message) || e) }); }
