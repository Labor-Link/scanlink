try { (() => {
const TONES = {
  neutral: {
    bg: "var(--n-100)",
    fg: "var(--n-600)",
    dot: "var(--n-400)"
  },
  success: {
    bg: "var(--ok-50)",
    fg: "var(--ok-700)",
    dot: "var(--ok-500)"
  },
  warning: {
    bg: "var(--warn-50)",
    fg: "var(--warn-700)",
    dot: "var(--warn-500)"
  },
  error: {
    bg: "var(--err-50)",
    fg: "var(--err-700)",
    dot: "var(--err-500)"
  },
  info: {
    bg: "var(--info-50)",
    fg: "var(--info-500)",
    dot: "var(--info-500)"
  },
  brand: {
    bg: "var(--indigo-50)",
    fg: "var(--indigo-700)",
    dot: "var(--indigo-500)"
  }
};

/** Status pill. The label is a plain word — "Working", "Offline" — never a code. */
function Badge({
  tone = "neutral",
  dot = false,
  pulse = false,
  children,
  style
}) {
  const t = TONES[tone] || TONES.neutral;
  return /*#__PURE__*/React.createElement("span", {
    style: {
      display: "inline-flex",
      alignItems: "center",
      gap: "6px",
      padding: "3px 10px",
      background: t.bg,
      color: t.fg,
      borderRadius: "var(--r-pill)",
      font: "600 var(--f-xs)/1.5 var(--font-ui)",
      whiteSpace: "nowrap",
      ...style
    }
  }, dot ? /*#__PURE__*/React.createElement("span", {
    style: {
      width: "6px",
      height: "6px",
      borderRadius: "50%",
      background: t.dot,
      flex: "none",
      animation: pulse ? "slPulse 1.6s ease-in-out infinite" : "none"
    }
  }, /*#__PURE__*/React.createElement("style", null, "@keyframes slPulse{0%,100%{opacity:1}50%{opacity:.35}}")) : null, children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/Badge.jsx", error: String((e && e.message) || e) }); }
