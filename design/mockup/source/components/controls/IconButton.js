try { (() => {
/** Square icon-only button for toolbars and table rows. Always give it `label`. */
function IconButton({
  icon,
  label,
  size = "md",
  variant = "ghost",
  disabled,
  onClick,
  style
}) {
  const [h, setH] = React.useState(false);
  const dim = size === "sm" ? 30 : 36;
  const bg = {
    ghost: "transparent",
    solid: "var(--n-0)",
    danger: "transparent"
  }[variant];
  const hoverBg = {
    ghost: "var(--n-100)",
    solid: "var(--n-50)",
    danger: "var(--err-50)"
  }[variant];
  const fg = {
    ghost: "var(--n-500)",
    solid: "var(--n-600)",
    danger: "var(--err-500)"
  }[variant];
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    title: label,
    "aria-label": label,
    disabled: disabled,
    onClick: onClick,
    onMouseEnter: () => setH(true),
    onMouseLeave: () => setH(false),
    style: {
      width: dim,
      height: dim,
      display: "inline-flex",
      alignItems: "center",
      justifyContent: "center",
      background: disabled ? "transparent" : h ? hoverBg : bg,
      color: disabled ? "var(--n-300)" : fg,
      border: variant === "solid" ? "1px solid var(--border-strong)" : "1px solid transparent",
      borderRadius: "var(--r-sm)",
      cursor: disabled ? "not-allowed" : "pointer",
      transition: "background var(--t-fast)",
      padding: 0,
      ...style
    }
  }, icon);
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/controls/IconButton.jsx", error: String((e && e.message) || e) }); }
