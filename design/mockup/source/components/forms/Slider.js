try { (() => {
function Slider({
  label,
  hint,
  value,
  min = 0,
  max = 20,
  step = 1,
  onChange,
  minLabel,
  maxLabel,
  showValue = true,
  width = "100%",
  id,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width,
      ...style
    }
  }, label ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "baseline",
      justifyContent: "space-between"
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.FieldLabel, {
    htmlFor: id,
    hint: hint
  }, label), showValue ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "600 var(--f-md)/1 var(--font-ui)",
      color: "var(--text-heading)"
    }
  }, value) : null) : null, /*#__PURE__*/React.createElement("input", {
    id: id,
    type: "range",
    value: value,
    min: min,
    max: max,
    step: step,
    onChange: onChange,
    style: {
      width: "100%",
      accentColor: "var(--action-primary)",
      margin: 0
    }
  }), minLabel || maxLabel ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "space-between",
      marginTop: "4px",
      font: "400 var(--f-xs)/1 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, /*#__PURE__*/React.createElement("span", null, minLabel), /*#__PURE__*/React.createElement("span", null, maxLabel)) : null);
}
Object.assign(__ds_scope, { Slider });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Slider.jsx", error: String((e && e.message) || e) }); }
