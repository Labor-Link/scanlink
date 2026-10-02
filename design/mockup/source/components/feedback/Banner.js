try { (() => {
const TONES = {
  info: {
    bg: "var(--info-50)",
    fg: "var(--info-500)",
    border: "#C9DDFB"
  },
  success: {
    bg: "var(--ok-50)",
    fg: "var(--ok-700)",
    border: "#BFE5D2"
  },
  warning: {
    bg: "var(--warn-50)",
    fg: "var(--warn-700)",
    border: "#F5DCB3"
  },
  error: {
    bg: "var(--err-50)",
    fg: "var(--err-700)",
    border: "#F5C6C6"
  }
};

/** Inline message attached to the thing it is about. Replaces the old bottom status strip for anything that needs reading. */
function Banner({
  tone = "info",
  icon,
  title,
  children,
  action,
  onDismiss,
  style
}) {
  const t = TONES[tone] || TONES.info;
  return /*#__PURE__*/React.createElement("div", {
    role: "status",
    style: {
      display: "flex",
      alignItems: "flex-start",
      gap: "12px",
      padding: "12px 14px",
      background: t.bg,
      border: `1px solid ${t.border}`,
      borderRadius: "var(--r-md)",
      ...style
    }
  }, icon ? /*#__PURE__*/React.createElement("span", {
    style: {
      color: t.fg,
      display: "flex",
      marginTop: "1px"
    }
  }, icon) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0,
      display: "flex",
      flexDirection: "column",
      gap: "2px"
    }
  }, title ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "600 var(--f-sm)/1.4 var(--font-ui)",
      color: t.fg
    }
  }, title) : null, children ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-sm)/1.5 var(--font-ui)",
      color: "var(--text-body)"
    }
  }, children) : null), action ? /*#__PURE__*/React.createElement("div", {
    style: {
      flex: "none"
    }
  }, action) : null, onDismiss ? /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onDismiss,
    "aria-label": "Dismiss",
    style: {
      border: "none",
      background: "transparent",
      cursor: "pointer",
      color: t.fg,
      font: "400 14px/1 var(--font-ui)"
    }
  }, "\u2715") : null);
}
Object.assign(__ds_scope, { Banner });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Banner.jsx", error: String((e && e.message) || e) }); }
