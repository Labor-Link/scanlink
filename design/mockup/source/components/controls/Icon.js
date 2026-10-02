try { (() => {
/**
 * Renders a Lucide icon from the UMD build on window.lucide.
 * ScanLink's source has no icon set (the Windows client used emoji), so Lucide is a
 * documented substitution — see readme.md → ICONOGRAPHY.
 */
function Icon({
  name,
  size = 18,
  strokeWidth = 1.75,
  color = "currentColor",
  style
}) {
  const lib = typeof window !== "undefined" ? window.lucide : null;
  const pascal = String(name).split(/[-_ ]/).map(p => p.charAt(0).toUpperCase() + p.slice(1)).join("");
  const node = lib && lib.icons ? lib.icons[pascal] : null;
  const kids = [];
  if (Array.isArray(node)) {
    const list = Array.isArray(node[2]) ? node[2] : node;
    list.forEach((child, i) => {
      if (!Array.isArray(child)) return;
      const [tag, attrs] = child;
      if (typeof tag === "string") kids.push(React.createElement(tag, {
        key: i,
        ...attrs
      }));
    });
  }
  return /*#__PURE__*/React.createElement("svg", {
    width: size,
    height: size,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: color,
    strokeWidth: strokeWidth,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": "true",
    style: {
      display: "block",
      flex: "none",
      ...style
    }
  }, kids);
}
Object.assign(__ds_scope, { Icon });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/controls/Icon.jsx", error: String((e && e.message) || e) }); }
