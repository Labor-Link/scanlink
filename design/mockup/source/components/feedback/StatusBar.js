try { (() => {
const TONES = {
  ready: {
    fg: "var(--text-muted)",
    dot: "var(--n-400)"
  },
  info: {
    fg: "var(--info-500)",
    dot: "var(--info-500)"
  },
  success: {
    fg: "var(--ok-700)",
    dot: "var(--ok-500)"
  },
  warning: {
    fg: "var(--warn-700)",
    dot: "var(--warn-500)"
  },
  error: {
    fg: "var(--err-700)",
    dot: "var(--err-500)"
  }
};

/** Quiet footer strip for ambient state (connection, last sync). Anything the user must act on goes in a Banner. */
function StatusBar({
  tone = "ready",
  children,
  right,
  style
}) {
  const t = TONES[tone] || TONES.ready;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: "10px",
      height: "36px",
      flex: "none",
      padding: "0 24px",
      background: "var(--surface-card)",
      borderTop: "1px solid var(--border-default)",
      font: "400 var(--f-xs)/1 var(--font-ui)",
      color: t.fg,
      ...style
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: "7px",
      height: "7px",
      borderRadius: "50%",
      background: t.dot,
      flex: "none"
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      flex: 1,
      minWidth: 0,
      overflow: "hidden",
      textOverflow: "ellipsis",
      whiteSpace: "nowrap"
    }
  }, children), right ? /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--text-muted)",
      flex: "none"
    }
  }, right) : null);
}
Object.assign(__ds_scope, { StatusBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/StatusBar.jsx", error: String((e && e.message) || e) }); }
