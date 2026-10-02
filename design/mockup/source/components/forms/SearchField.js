try { (() => {
/** Search input with a leading glyph and a clear affordance. */
function SearchField({
  value,
  onChange,
  onClear,
  placeholder = "Search…",
  width = "280px",
  icon,
  disabled,
  style
}) {
  const [f, setF] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width,
      position: "relative",
      display: "flex",
      alignItems: "center",
      ...style
    }
  }, icon ? /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      left: "12px",
      display: "flex",
      color: "var(--n-400)",
      pointerEvents: "none"
    }
  }, icon) : null, /*#__PURE__*/React.createElement("input", {
    type: "search",
    value: value,
    onChange: onChange,
    placeholder: placeholder,
    disabled: disabled,
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: {
      ...__ds_scope.fieldBox({
        focus: f,
        disabled,
        size: "sm"
      }),
      paddingLeft: icon ? "36px" : "12px"
    }
  }), value && onClear ? /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onClear,
    "aria-label": "Clear search",
    style: {
      position: "absolute",
      right: "8px",
      border: "none",
      background: "transparent",
      color: "var(--n-400)",
      cursor: "pointer",
      font: "400 var(--f-md)/1 var(--font-ui)"
    }
  }, "\u2715") : null);
}
Object.assign(__ds_scope, { SearchField });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/SearchField.jsx", error: String((e && e.message) || e) }); }
