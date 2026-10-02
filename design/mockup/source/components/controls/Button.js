try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const V = {
  primary: {
    bg: "var(--action-primary)",
    hover: "var(--action-primary-hover)",
    active: "var(--action-primary-active)",
    fg: "#fff",
    border: "1px solid transparent",
    shadow: "var(--e-1)"
  },
  secondary: {
    bg: "var(--n-0)",
    hover: "var(--n-50)",
    active: "var(--n-100)",
    fg: "var(--n-800)",
    border: "1px solid var(--border-strong)",
    shadow: "var(--e-1)"
  },
  ghost: {
    bg: "transparent",
    hover: "var(--n-100)",
    active: "var(--n-200)",
    fg: "var(--n-600)",
    border: "1px solid transparent",
    shadow: "none"
  },
  navy: {
    bg: "var(--navy-900)",
    hover: "var(--navy-950)",
    active: "var(--navy-950)",
    fg: "#fff",
    border: "1px solid transparent",
    shadow: "var(--e-1)"
  },
  success: {
    bg: "var(--ok-500)",
    hover: "var(--ok-700)",
    active: "var(--ok-700)",
    fg: "#fff",
    border: "1px solid transparent",
    shadow: "var(--e-1)"
  },
  danger: {
    bg: "var(--err-500)",
    hover: "var(--err-700)",
    active: "var(--err-700)",
    fg: "#fff",
    border: "1px solid transparent",
    shadow: "var(--e-1)"
  },
  dangerQuiet: {
    bg: "var(--n-0)",
    hover: "var(--err-50)",
    active: "var(--err-50)",
    fg: "var(--err-700)",
    border: "1px solid var(--n-300)",
    shadow: "var(--e-1)"
  }
};
const S = {
  sm: {
    h: "var(--h-sm)",
    px: "12px",
    f: "var(--f-sm)",
    gap: "6px"
  },
  md: {
    h: "var(--h-md)",
    px: "16px",
    f: "var(--f-md)",
    gap: "8px"
  },
  lg: {
    h: "var(--h-lg)",
    px: "20px",
    f: "var(--f-md)",
    gap: "8px"
  }
};
function Button({
  variant = "primary",
  size = "md",
  disabled,
  loading,
  block,
  icon,
  iconEnd,
  children,
  onClick,
  type = "button",
  style,
  ...rest
}) {
  const [h, setH] = React.useState(false);
  const [d, setD] = React.useState(false);
  const [f, setF] = React.useState(false);
  const v = V[variant] || V.primary,
    s = S[size] || S.md;
  const off = disabled || loading;
  return /*#__PURE__*/React.createElement("button", _extends({
    type: type,
    disabled: off,
    onClick: onClick,
    onMouseEnter: () => setH(true),
    onMouseLeave: () => {
      setH(false);
      setD(false);
    },
    onMouseDown: () => setD(true),
    onMouseUp: () => setD(false),
    onFocus: () => setF(true),
    onBlur: () => setF(false),
    style: {
      display: block ? "flex" : "inline-flex",
      width: block ? "100%" : "auto",
      alignItems: "center",
      justifyContent: "center",
      gap: s.gap,
      height: s.h,
      padding: `0 ${s.px}`,
      boxSizing: "border-box",
      background: off ? "var(--n-100)" : d ? v.active : h ? v.hover : v.bg,
      color: off ? "var(--n-400)" : v.fg,
      border: off ? "1px solid var(--border-default)" : v.border,
      borderRadius: "var(--r-sm)",
      boxShadow: f ? "var(--ring-focus)" : off ? "none" : v.shadow,
      font: `600 ${s.f}/1 var(--font-ui)`,
      letterSpacing: ".01em",
      cursor: off ? "not-allowed" : "pointer",
      transition: "background var(--t-fast), box-shadow var(--t-fast)",
      outline: "none",
      whiteSpace: "nowrap",
      ...style
    }
  }, rest), loading ? /*#__PURE__*/React.createElement(Spinner, null) : icon, children, iconEnd);
}
function Spinner() {
  return /*#__PURE__*/React.createElement("span", {
    style: {
      width: 14,
      height: 14,
      borderRadius: "50%",
      border: "2px solid currentColor",
      borderTopColor: "transparent",
      display: "inline-block",
      animation: "slSpin .7s linear infinite"
    }
  }, /*#__PURE__*/React.createElement("style", null, "@keyframes slSpin{to{transform:rotate(360deg)}}"));
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/controls/Button.jsx", error: String((e && e.message) || e) }); }
