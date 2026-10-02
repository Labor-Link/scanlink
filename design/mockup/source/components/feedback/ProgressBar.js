try { (() => {
/** Thin progress track. `marquee` for work with no known duration. */
function ProgressBar({
  value = 0,
  marquee = false,
  height = "6px",
  tone = "brand",
  label,
  style
}) {
  const fill = {
    brand: "var(--action-primary)",
    success: "var(--ok-500)"
  }[tone] || "var(--action-primary)";
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "6px",
      ...style
    }
  }, label ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-xs)/1.4 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, label) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      width: "100%",
      height,
      background: "var(--n-200)",
      borderRadius: "var(--r-pill)",
      overflow: "hidden",
      position: "relative"
    }
  }, /*#__PURE__*/React.createElement("style", null, "@keyframes slMarquee{0%{left:-40%}100%{left:100%}}"), marquee ? /*#__PURE__*/React.createElement("span", {
    style: {
      position: "absolute",
      top: 0,
      bottom: 0,
      width: "40%",
      background: fill,
      borderRadius: "var(--r-pill)",
      animation: "slMarquee 1.4s cubic-bezier(.4,0,.2,1) infinite"
    }
  }) : /*#__PURE__*/React.createElement("span", {
    style: {
      display: "block",
      height: "100%",
      width: `${Math.max(0, Math.min(100, value))}%`,
      background: fill,
      borderRadius: "var(--r-pill)",
      transition: "width var(--t-med)"
    }
  })));
}
Object.assign(__ds_scope, { ProgressBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/ProgressBar.jsx", error: String((e && e.message) || e) }); }
