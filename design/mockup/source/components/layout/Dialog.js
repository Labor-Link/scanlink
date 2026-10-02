try { (() => {
/** Centred modal. Body scrolls; the action row stays put. */
function Dialog({
  title,
  description,
  width = "520px",
  tone = "default",
  children,
  actions,
  onClose,
  style
}) {
  const accent = {
    default: null,
    danger: "var(--err-500)",
    warning: "var(--warn-500)"
  }[tone];
  return /*#__PURE__*/React.createElement("div", {
    style: {
      position: "fixed",
      inset: 0,
      zIndex: 60,
      display: "grid",
      placeItems: "center",
      padding: "24px",
      background: "rgba(16,24,40,.45)"
    }
  }, /*#__PURE__*/React.createElement("div", {
    role: "dialog",
    "aria-modal": "true",
    style: {
      width,
      maxWidth: "100%",
      maxHeight: "100%",
      background: "var(--surface-card)",
      borderRadius: "var(--r-lg)",
      boxShadow: "var(--e-4)",
      display: "flex",
      flexDirection: "column",
      overflow: "hidden",
      ...style
    }
  }, /*#__PURE__*/React.createElement("header", {
    style: {
      display: "flex",
      alignItems: "flex-start",
      gap: "12px",
      padding: "20px 20px 0"
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0,
      display: "flex",
      flexDirection: "column",
      gap: "4px"
    }
  }, /*#__PURE__*/React.createElement("h2", {
    style: {
      margin: 0,
      font: "600 var(--f-lg)/1.3 var(--font-ui)",
      color: accent || "var(--text-heading)"
    }
  }, title), description ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      font: "400 var(--f-sm)/1.5 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, description) : null), onClose ? /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onClose,
    "aria-label": "Close",
    style: {
      border: "none",
      background: "transparent",
      cursor: "pointer",
      color: "var(--n-400)",
      font: "400 16px/1 var(--font-ui)",
      padding: "2px 4px"
    }
  }, "\u2715") : null), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: "16px 20px",
      overflowY: "auto",
      display: "flex",
      flexDirection: "column",
      gap: "16px"
    }
  }, children), actions ? /*#__PURE__*/React.createElement("footer", {
    style: {
      display: "flex",
      justifyContent: "flex-end",
      gap: "8px",
      padding: "16px 20px",
      borderTop: "1px solid var(--border-subtle)",
      background: "var(--n-25)"
    }
  }, actions) : null));
}
Object.assign(__ds_scope, { Dialog });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/layout/Dialog.jsx", error: String((e && e.message) || e) }); }
