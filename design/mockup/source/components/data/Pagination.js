try { (() => {
function Pagination({
  page = 1,
  pageCount = 1,
  total,
  pageSize,
  onPrevious,
  onNext,
  style
}) {
  const from = total != null && pageSize ? (page - 1) * pageSize + 1 : null;
  const to = total != null && pageSize ? Math.min(page * pageSize, total) : null;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      justifyContent: "space-between",
      gap: "16px",
      ...style
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-sm)/1.4 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, from != null ? `Showing ${from}–${to} of ${total}` : `Page ${page} of ${pageCount}`), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: "8px"
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Button, {
    variant: "secondary",
    size: "sm",
    disabled: page <= 1,
    onClick: onPrevious
  }, "Previous"), /*#__PURE__*/React.createElement(__ds_scope.Button, {
    variant: "secondary",
    size: "sm",
    disabled: page >= pageCount,
    onClick: onNext
  }, "Next")));
}
Object.assign(__ds_scope, { Pagination });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/Pagination.jsx", error: String((e && e.message) || e) }); }
