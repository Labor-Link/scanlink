try { (() => {
/** Page top bar: title + subtitle on the left, contextual actions on the right. */
function TopBar({
  title,
  subtitle,
  breadcrumb,
  actions,
  left,
  style
}) {
  return /*#__PURE__*/React.createElement("header", {
    style: {
      minHeight: "var(--topbar-h)",
      boxSizing: "border-box",
      display: "flex",
      alignItems: "center",
      gap: "16px",
      padding: "12px 24px",
      background: "var(--surface-card)",
      borderBottom: "1px solid var(--border-default)",
      ...style
    }
  }, left, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: "2px",
      flex: 1,
      minWidth: 0
    }
  }, breadcrumb ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "500 var(--f-xs)/1.2 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, breadcrumb) : null, /*#__PURE__*/React.createElement("h1", {
    style: {
      margin: 0,
      font: `600 var(--f-xl)/var(--lh-tight) var(--font-ui)`,
      color: "var(--text-heading)",
      overflow: "hidden",
      textOverflow: "ellipsis",
      whiteSpace: "nowrap"
    }
  }, title), subtitle ? /*#__PURE__*/React.createElement("span", {
    style: {
      font: "400 var(--f-sm)/1.4 var(--font-ui)",
      color: "var(--text-muted)"
    }
  }, subtitle) : null), actions ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: "8px",
      flex: "none"
    }
  }, actions) : null);
}
Object.assign(__ds_scope, { TopBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/TopBar.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/AppShell.jsx
try { (() => {
(() => {
  const {
    Sidebar,
    TopBar,
    StatusBar,
    SiteTile,
    Icon,
    IconButton
  } = window.ScanLinkDesignSystem_c824b0;
  const NAV = [{
    items: [{
      key: "overview",
      icon: "layout-dashboard",
      label: "Overview"
    }]
  }, {
    title: "Daily work",
    items: [{
      key: "scans",
      icon: "scan-line",
      label: "Scans",
      badge: "live"
    }, {
      key: "labels",
      icon: "printer",
      label: "Print labels"
    }]
  }, {
    title: "Setup",
    items: [{
      key: "devices",
      icon: "usb",
      label: "Scanners & printer"
    }, {
      key: "products",
      icon: "database",
      label: "Crops & products"
    }, {
      key: "people",
      icon: "users",
      label: "People"
    }]
  }];
  function AppShell({
    site,
    active,
    onNavigate,
    onSwitchSite,
    onSignOut,
    title,
    subtitle,
    actions,
    status,
    statusTone,
    statusRight,
    children
  }) {
    const groups = NAV.map(g => ({
      ...g,
      items: g.items.map(it => ({
        ...it,
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: it.icon,
          size: 20
        })
      }))
    }));
    return /*#__PURE__*/React.createElement("div", {
      style: {
        height: "100%",
        display: "flex",
        background: "var(--surface-app)"
      }
    }, /*#__PURE__*/React.createElement(Sidebar, {
      logoSrc: "../../assets/scanlink-logo-white.png",
      groups: groups,
      activeKey: active,
      onSelect: onNavigate,
      footer: /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          flexDirection: "column",
          gap: "4px"
        }
      }, /*#__PURE__*/React.createElement(SiteTile, {
        compact: true,
        name: site.name,
        id: site.id,
        role: site.role,
        onClick: onSwitchSite
      }), /*#__PURE__*/React.createElement("button", {
        type: "button",
        onClick: onSignOut,
        style: {
          display: "flex",
          alignItems: "center",
          gap: "12px",
          padding: "9px 12px",
          background: "transparent",
          border: "none",
          borderRadius: "var(--r-sm)",
          color: "rgba(255,255,255,.55)",
          font: "500 var(--f-sm)/1 var(--font-ui)",
          cursor: "pointer"
        }
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "log-out",
        size: 18
      }), "Sign out"))
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        minWidth: 0,
        display: "flex",
        flexDirection: "column"
      }
    }, /*#__PURE__*/React.createElement(TopBar, {
      title: title,
      subtitle: subtitle,
      actions: actions
    }), /*#__PURE__*/React.createElement("main", {
      style: {
        flex: 1,
        minHeight: 0,
        overflowY: "auto",
        padding: "24px"
      }
    }, children), /*#__PURE__*/React.createElement(StatusBar, {
      tone: statusTone,
      right: statusRight
    }, status)));
  }
  Object.assign(window, {
    AppShell
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/AppShell.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/DevicesScreen.jsx
try { (() => {
(() => {
  const {
    useState
  } = React;
  const {
    Card,
    DataTable,
    Badge,
    Button,
    IconButton,
    Icon,
    Banner,
    Dialog,
    Select,
    TextField,
    FieldSet,
    EmptyState
  } = window.ScanLinkDesignSystem_c824b0;
  const INITIAL = [{
    serial: "SL-HH-02",
    name: "Line 3 scanner",
    port: "COM3",
    line: "3",
    block: "14",
    supplier: "Rooidraai",
    state: "Working",
    tone: "success"
  }, {
    serial: "SL-HH-05",
    name: "Line 1 scanner",
    port: "COM4",
    line: "1",
    block: "07",
    supplier: "Kleinbos",
    state: "Working",
    tone: "success"
  }, {
    serial: "SL-HH-11",
    name: "Line 2 scanner",
    port: "COM7",
    line: "2",
    block: "21",
    supplier: "Vaalkop",
    state: "Offline",
    tone: "error"
  }];
  function DevicesScreen({
    onStatus
  }) {
    const [rows, setRows] = useState(INITIAL);
    const [removing, setRemoving] = useState(null);
    const [printerOpen, setPrinterOpen] = useState(false);
    const [scanning, setScanning] = useState(false);
    const cols = [{
      key: "name",
      header: "Scanner",
      render: r => /*#__PURE__*/React.createElement("span", {
        style: {
          display: "flex",
          flexDirection: "column",
          gap: "1px"
        }
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          font: "500 var(--f-sm)/1.3 var(--font-ui)"
        }
      }, r.name), /*#__PURE__*/React.createElement("span", {
        style: {
          font: "400 var(--f-xs)/1.3 var(--font-mono)",
          color: "var(--text-muted)"
        }
      }, r.serial, " \xB7 ", r.port))
    }, {
      key: "line",
      header: "Line"
    }, {
      key: "block",
      header: "Block"
    }, {
      key: "supplier",
      header: "Supplier"
    }, {
      key: "state",
      header: "Status",
      render: r => /*#__PURE__*/React.createElement(Badge, {
        tone: r.tone,
        dot: true
      }, r.state)
    }, {
      key: "act",
      header: "",
      align: "right",
      render: r => /*#__PURE__*/React.createElement("span", {
        style: {
          display: "inline-flex",
          gap: "4px"
        }
      }, /*#__PURE__*/React.createElement(IconButton, {
        size: "sm",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "pencil",
          size: 16
        }),
        label: `Edit ${r.name}`
      }), /*#__PURE__*/React.createElement(IconButton, {
        size: "sm",
        variant: "danger",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "trash-2",
          size: 16
        }),
        label: `Remove ${r.name}`,
        onClick: () => setRemoving(r)
      }))
    }];
    const rescan = () => {
      setScanning(true);
      onStatus("Looking for scanners…", "info");
      setTimeout(() => {
        setScanning(false);
        onStatus("Found 3 scanners. One isn't answering.", "warning");
      }, 900);
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "16px",
        maxWidth: "1080px"
      }
    }, /*#__PURE__*/React.createElement(Banner, {
      tone: "error",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "unplug"
      }),
      title: "Line 2 scanner isn't answering",
      action: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        onClick: rescan
      }, "Look again")
    }, "Check that it's plugged in and switched on, then look again. Scans already saved are not affected."), /*#__PURE__*/React.createElement(Card, {
      title: "Scanners on this site",
      subtitle: "Each scanner is tied to a line and a block so scans land in the right place.",
      padding: "0",
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        loading: scanning,
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "refresh-cw",
          size: 16
        }),
        onClick: rescan
      }, "Look for scanners"), /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "primary",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "plus",
          size: 16
        })
      }, "Add scanner"))
    }, /*#__PURE__*/React.createElement(DataTable, {
      rowKey: "serial",
      columns: cols,
      rows: rows,
      empty: /*#__PURE__*/React.createElement(EmptyState, {
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "usb",
          size: 22
        }),
        title: "No scanners found yet",
        description: "Plug a scanner into this computer, then choose Look for scanners.",
        action: /*#__PURE__*/React.createElement(Button, {
          variant: "secondary",
          size: "sm",
          onClick: rescan
        }, "Look for scanners")
      })
    })), /*#__PURE__*/React.createElement(Card, {
      title: "Label printer",
      subtitle: "ScanLink sends every label here.",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        onClick: () => setPrinterOpen(true)
      }, "Change connection")
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: "16px"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: "42px",
        height: "42px",
        borderRadius: "var(--r-md)",
        background: "var(--indigo-50)",
        color: "var(--indigo-500)",
        display: "grid",
        placeItems: "center"
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "printer",
      size: 20
    })), /*#__PURE__*/React.createElement("span", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "2px",
        flex: 1
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: "600 var(--f-md)/1.3 var(--font-ui)",
        color: "var(--text-heading)"
      }
    }, "Argox thermal printer"), /*#__PURE__*/React.createElement("span", {
      style: {
        font: "400 var(--f-sm)/1.3 var(--font-mono)",
        color: "var(--text-muted)"
      }
    }, "Network \xB7 192.168.1.44:9100")), /*#__PURE__*/React.createElement(Badge, {
      tone: "success",
      dot: true
    }, "Working"))), removing ? /*#__PURE__*/React.createElement(Dialog, {
      title: "Remove this scanner?",
      tone: "danger",
      width: "440px",
      description: `${removing.name} (${removing.serial}) will stop sending scans to this site. You can add it again later.`,
      onClose: () => setRemoving(null),
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        onClick: () => setRemoving(null)
      }, "Keep it"), /*#__PURE__*/React.createElement(Button, {
        variant: "danger",
        onClick: () => {
          setRows(rows.filter(r => r.serial !== removing.serial));
          onStatus(`${removing.name} removed`, "warning");
          setRemoving(null);
        }
      }, "Remove scanner"))
    }) : null, printerOpen ? /*#__PURE__*/React.createElement(Dialog, {
      title: "How is the printer connected?",
      width: "480px",
      description: "Choose how this computer reaches the label printer. If you're not sure, ask whoever set it up.",
      onClose: () => setPrinterOpen(false),
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        onClick: () => setPrinterOpen(false)
      }, "Cancel"), /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        onClick: () => {
          setPrinterOpen(false);
          onStatus("Printer connection saved", "success");
        }
      }, "Save connection"))
    }, /*#__PURE__*/React.createElement(FieldSet, {
      columns: 1
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Connection",
      defaultValue: "Network (LAN)",
      hint: "Network is the usual choice in a packhouse.",
      options: ["Network (LAN)", "USB cable", "Serial cable (COM)", "Save to a file"]
    }), /*#__PURE__*/React.createElement(TextField, {
      label: "Printer address",
      mono: true,
      defaultValue: "192.168.1.44:9100",
      hint: "You'll find this printed on the label on the printer."
    }))) : null);
  }
  Object.assign(window, {
    DevicesScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/DevicesScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/LabelsScreen.jsx
try { (() => {
(() => {
  const {
    useState
  } = React;
  const {
    Card,
    FieldSet,
    Select,
    NumberField,
    Slider,
    Checkbox,
    Button,
    Icon,
    Badge,
    ProgressBar,
    Banner
  } = window.ScanLinkDesignSystem_c824b0;
  const STEPS = ["What are you labelling?", "How many?", "Check and print"];
  function Sticker({
    picker,
    product,
    count,
    twoUp
  }) {
    const one = /*#__PURE__*/React.createElement("div", {
      style: {
        width: "196px",
        background: "#fff",
        border: "1px solid var(--border-strong)",
        borderRadius: "var(--r-xs)",
        padding: "12px",
        display: "flex",
        flexDirection: "column",
        gap: "8px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        font: "700 11px/1.4 var(--font-mono)",
        color: "var(--n-900)"
      }
    }, product, /*#__PURE__*/React.createElement("br", null), "PICKER ", picker), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: "1px",
        alignItems: "flex-end",
        height: "38px"
      }
    }, Array.from({
      length: 40
    }, (_, i) => /*#__PURE__*/React.createElement("span", {
      key: i,
      style: {
        width: i % 3 ? "2px" : "3px",
        height: "100%",
        background: i % 4 === 0 ? "transparent" : "#111"
      }
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        font: "700 10px/1 var(--font-mono)",
        letterSpacing: ".1em",
        color: "var(--n-900)"
      }
    }, "SC-0093-A", count % 10));
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: "12px",
        justifyContent: "center",
        padding: "20px",
        background: "var(--n-100)",
        borderRadius: "var(--r-md)"
      }
    }, one, twoUp ? one : null);
  }
  function LabelsScreen({
    onStatus
  }) {
    const [step, setStep] = useState(0);
    const [crop, setCrop] = useState("Avocado (01)");
    const [product, setProduct] = useState("Hass Loose (118)");
    const [picker, setPicker] = useState("4471 — J. Mokoena");
    const [count, setCount] = useState(12);
    const [twoUp, setTwoUp] = useState(false);
    const [darkness, setDarkness] = useState(8);
    const [progress, setProgress] = useState(0);
    const [done, setDone] = useState(false);
    const [advanced, setAdvanced] = useState(false);
    const print = () => {
      setProgress(20);
      setDone(false);
      setTimeout(() => setProgress(70), 250);
      setTimeout(() => {
        setProgress(100);
        setDone(true);
        onStatus(`${count} labels sent to the printer`, "success");
      }, 700);
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(auto-fit,minmax(340px,1fr))",
        gap: "16px",
        alignItems: "start",
        maxWidth: "1080px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "16px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: "8px",
        flexWrap: "wrap"
      }
    }, STEPS.map((s, i) => /*#__PURE__*/React.createElement("button", {
      key: s,
      type: "button",
      onClick: () => setStep(i),
      style: {
        flex: "1 1 180px",
        display: "flex",
        alignItems: "center",
        gap: "10px",
        padding: "12px 14px",
        cursor: "pointer",
        textAlign: "left",
        background: "var(--surface-card)",
        borderRadius: "var(--r-md)",
        border: `1px solid ${i === step ? "var(--action-primary)" : "var(--border-default)"}`,
        boxShadow: i === step ? "var(--ring-focus)" : "var(--e-1)"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: "24px",
        height: "24px",
        flex: "none",
        borderRadius: "50%",
        display: "grid",
        placeItems: "center",
        background: i < step ? "var(--ok-500)" : i === step ? "var(--action-primary)" : "var(--n-100)",
        color: i <= step ? "#fff" : "var(--n-500)",
        font: "600 var(--f-xs)/1 var(--font-ui)"
      }
    }, i < step ? /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 14
    }) : i + 1), /*#__PURE__*/React.createElement("span", {
      style: {
        font: `${i === step ? 600 : 500} var(--f-sm)/1.3 var(--font-ui)`,
        color: i === step ? "var(--text-heading)" : "var(--text-muted)"
      }
    }, s)))), step === 0 ? /*#__PURE__*/React.createElement(Card, {
      title: "What are you labelling?",
      subtitle: "Pick the crop first \u2014 the product list narrows to match.",
      footer: /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        iconEnd: /*#__PURE__*/React.createElement(Icon, {
          name: "arrow-right",
          size: 16
        }),
        onClick: () => setStep(1)
      }, "Next: how many?")
    }, /*#__PURE__*/React.createElement(FieldSet, {
      columns: 2
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Crop",
      required: true,
      value: crop,
      onChange: e => setCrop(e.target.value),
      options: ["Avocado (01)", "Citrus (02)"]
    }), /*#__PURE__*/React.createElement(Select, {
      label: "Product",
      required: true,
      value: product,
      onChange: e => setProduct(e.target.value),
      options: ["Hass Loose (118)", "Valencia 88 (204)"]
    }), /*#__PURE__*/React.createElement(Select, {
      label: "Who is picking?",
      required: true,
      value: picker,
      onChange: e => setPicker(e.target.value),
      hint: "Start typing a number or a name.",
      options: ["4471 — J. Mokoena", "4472 — P. Naidoo", "4480 — T. van Wyk"]
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        alignSelf: "end",
        display: "flex",
        flexDirection: "column",
        gap: "4px"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: "400 var(--f-xs)/1.4 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, "This combination prints as"), /*#__PURE__*/React.createElement(Badge, {
      tone: "brand"
    }, "Grade 1 \xB7 Count 18 \xB7 Carton M4 \xB7 4.0 kg")))) : null, step === 1 ? /*#__PURE__*/React.createElement(Card, {
      title: "How many labels?",
      subtitle: "You can change this before printing.",
      footer: /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          gap: "8px"
        }
      }, /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "arrow-left",
          size: 16
        }),
        onClick: () => setStep(0)
      }, "Back"), /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        iconEnd: /*#__PURE__*/React.createElement(Icon, {
          name: "arrow-right",
          size: 16
        }),
        onClick: () => setStep(2)
      }, "Next: check and print"))
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "flex-end",
        gap: "12px",
        flexWrap: "wrap"
      }
    }, /*#__PURE__*/React.createElement(NumberField, {
      label: "Number of labels",
      value: count,
      min: 1,
      max: 200,
      onChange: e => setCount(+e.target.value),
      width: "180px"
    }), [6, 12, 24, 48].map(n => /*#__PURE__*/React.createElement(Button, {
      key: n,
      variant: count === n ? "primary" : "secondary",
      size: "sm",
      onClick: () => setCount(n)
    }, n))), /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: "16px"
      }
    }, /*#__PURE__*/React.createElement(Checkbox, {
      label: "Print two labels side by side",
      description: "Uses half as much label roll.",
      checked: twoUp,
      onChange: e => setTwoUp(e.target.checked)
    }))) : null, step === 2 ? /*#__PURE__*/React.createElement(Card, {
      title: "Check and print",
      subtitle: "Compare the preview with the label roll in the printer.",
      footer: /*#__PURE__*/React.createElement("div", {
        style: {
          display: "flex",
          gap: "8px",
          alignItems: "center",
          width: "100%"
        }
      }, /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "arrow-left",
          size: 16
        }),
        onClick: () => setStep(1)
      }, "Back"), /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "printer",
          size: 16
        }),
        onClick: print
      }, "Print ", count, " labels"), /*#__PURE__*/React.createElement("span", {
        style: {
          marginLeft: "auto",
          font: "400 var(--f-xs)/1.4 var(--font-ui)",
          color: "var(--text-muted)"
        }
      }, "Printer: Argox \xB7 192.168.1.44"))
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "auto 1fr",
        gap: "10px 20px"
      }
    }, [["Product", product], ["Crop", crop], ["Picker", picker], ["Labels", `${count}${twoUp ? " (two per row)" : ""}`]].map(([k, v]) => /*#__PURE__*/React.createElement(React.Fragment, {
      key: k
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: "400 var(--f-sm)/1.4 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, k), /*#__PURE__*/React.createElement("span", {
      style: {
        font: "500 var(--f-sm)/1.4 var(--font-ui)"
      }
    }, v)))), progress > 0 ? /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: "16px"
      }
    }, done ? /*#__PURE__*/React.createElement(Banner, {
      tone: "success",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "check-circle-2"
      }),
      title: `${count} labels sent to the printer`
    }, "If nothing comes out, check the roll and try again.") : /*#__PURE__*/React.createElement(ProgressBar, {
      value: progress,
      label: `Sending ${count} labels to the printer…`
    })) : null) : null, /*#__PURE__*/React.createElement(Card, {
      title: "Printer settings",
      subtitle: "Most people never need to change these.",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "ghost",
        iconEnd: /*#__PURE__*/React.createElement(Icon, {
          name: advanced ? "chevron-up" : "chevron-down",
          size: 16
        }),
        onClick: () => setAdvanced(!advanced)
      }, advanced ? "Hide" : "Show")
    }, advanced ? /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "20px"
      }
    }, /*#__PURE__*/React.createElement(FieldSet, {
      title: "Label size",
      description: "Match these to the label roll in the printer.",
      columns: 4
    }, /*#__PURE__*/React.createElement(NumberField, {
      label: "Width",
      defaultValue: 40,
      unit: "mm",
      width: "100%"
    }), /*#__PURE__*/React.createElement(NumberField, {
      label: "Height",
      defaultValue: 25,
      unit: "mm",
      width: "100%"
    }), /*#__PURE__*/React.createElement(NumberField, {
      label: "Gap",
      defaultValue: 3,
      unit: "mm",
      width: "100%"
    }), /*#__PURE__*/React.createElement(NumberField, {
      label: "Resolution",
      defaultValue: 203,
      unit: "dpi",
      width: "100%"
    })), /*#__PURE__*/React.createElement(FieldSet, {
      title: "Print quality",
      columns: 2
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Barcode type",
      options: ["Code 128", "Code 39", "EAN-13"]
    }), /*#__PURE__*/React.createElement(Select, {
      label: "Speed",
      options: ["2 ips", "4 ips", "6 ips"]
    })), /*#__PURE__*/React.createElement(Slider, {
      label: "Print darkness",
      value: darkness,
      onChange: e => setDarkness(+e.target.value),
      minLabel: "Lighter",
      maxLabel: "Darker",
      width: "320px"
    })) : /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: "400 var(--f-sm)/1.5 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, "40 \xD7 25 mm labels, Code 128, 203 dpi. Change these only if the printed labels come out wrong."))), /*#__PURE__*/React.createElement(Card, {
      title: "Preview",
      subtitle: "This is what will come out of the printer."
    }, /*#__PURE__*/React.createElement(Sticker, {
      picker: picker.split("—")[0].trim(),
      product: product,
      count: count,
      twoUp: twoUp
    }), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: "12px 0 0",
        font: "400 var(--f-xs)/1.5 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, "Shown at roughly actual size for a 40 \xD7 25 mm label.")));
  }
  Object.assign(window, {
    LabelsScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/LabelsScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/LoginScreen.jsx
try { (() => {
(() => {
  const {
    useState
  } = React;
  const {
    Button,
    TextField,
    Icon,
    Banner,
    Checkbox
  } = window.ScanLinkDesignSystem_c824b0;
  function LoginScreen({
    onLogin
  }) {
    const [email, setEmail] = useState("");
    const [pw, setPw] = useState("");
    const [reveal, setReveal] = useState(false);
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");
    const submit = e => {
      if (e) e.preventDefault();
      if (!email.trim()) {
        setErr("Enter the email address you use for ScanLink.");
        return;
      }
      setErr("");
      setBusy(true);
      setTimeout(() => {
        setBusy(false);
        onLogin();
      }, 850);
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        height: "100%",
        display: "grid",
        gridTemplateColumns: "minmax(0,1.05fr) minmax(420px,.95fr)",
        background: "var(--surface-app)"
      }
    }, /*#__PURE__*/React.createElement("aside", {
      style: {
        background: "var(--navy-900)",
        padding: "48px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        position: "relative",
        overflow: "hidden"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        position: "absolute",
        right: "-140px",
        top: "-120px",
        width: "440px",
        height: "440px",
        borderRadius: "50%",
        background: "rgba(77,74,234,.20)"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        position: "absolute",
        right: "60px",
        bottom: "-180px",
        width: "320px",
        height: "320px",
        borderRadius: "50%",
        background: "rgba(255,255,255,.035)"
      }
    }), /*#__PURE__*/React.createElement("img", {
      src: "../../assets/scanlink-logo-white.png",
      alt: "ScanLink",
      style: {
        width: "168px",
        position: "relative"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        position: "relative",
        display: "flex",
        flexDirection: "column",
        gap: "20px",
        maxWidth: "460px"
      }
    }, /*#__PURE__*/React.createElement("h2", {
      style: {
        margin: 0,
        font: "600 var(--f-4xl)/1.25 var(--font-ui)",
        color: "#fff"
      }
    }, "Every carton scanned, counted and labelled \u2014 without leaving this screen."), /*#__PURE__*/React.createElement("ul", {
      style: {
        margin: 0,
        padding: 0,
        listStyle: "none",
        display: "flex",
        flexDirection: "column",
        gap: "12px"
      }
    }, [["scan-line", "Watch scans land from every line, live"], ["printer", "Print barcode and carton labels in three clicks"], ["cloud-upload", "Nothing is lost if the internet drops"]].map(([ic, t]) => /*#__PURE__*/React.createElement("li", {
      key: t,
      style: {
        display: "flex",
        alignItems: "center",
        gap: "12px",
        color: "rgba(255,255,255,.78)",
        font: "400 var(--f-md)/1.5 var(--font-ui)"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: "30px",
        height: "30px",
        flex: "none",
        borderRadius: "var(--r-sm)",
        background: "rgba(255,255,255,.10)",
        color: "#fff",
        display: "grid",
        placeItems: "center"
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: ic,
      size: 16
    })), t)))), /*#__PURE__*/React.createElement("span", {
      style: {
        position: "relative",
        font: "400 var(--f-xs)/1.4 var(--font-ui)",
        color: "rgba(255,255,255,.42)"
      }
    }, "ScanLink for packhouses \xB7 v2.0")), /*#__PURE__*/React.createElement("main", {
      style: {
        display: "grid",
        placeItems: "center",
        padding: "40px 24px",
        overflowY: "auto"
      }
    }, /*#__PURE__*/React.createElement("form", {
      onSubmit: submit,
      style: {
        width: "100%",
        maxWidth: "380px",
        display: "flex",
        flexDirection: "column",
        gap: "20px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "6px"
      }
    }, /*#__PURE__*/React.createElement("h1", {
      style: {
        margin: 0,
        font: "600 var(--f-3xl)/1.25 var(--font-ui)",
        color: "var(--text-heading)"
      }
    }, "Sign in"), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: "400 var(--f-md)/1.5 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, "Use the email and password your manager set up for you.")), err ? /*#__PURE__*/React.createElement(Banner, {
      tone: "error",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "alert-circle"
      })
    }, err) : null, /*#__PURE__*/React.createElement(TextField, {
      label: "Email address",
      required: true,
      type: "email",
      placeholder: "you@packhouse.co",
      value: email,
      onChange: e => setEmail(e.target.value),
      size: "lg"
    }), /*#__PURE__*/React.createElement(TextField, {
      label: "Password",
      required: true,
      size: "lg",
      type: reveal ? "text" : "password",
      value: pw,
      onChange: e => setPw(e.target.value),
      suffix: /*#__PURE__*/React.createElement("button", {
        type: "button",
        onClick: () => setReveal(!reveal),
        "aria-label": reveal ? "Hide password" : "Show password",
        style: {
          border: "none",
          background: "transparent",
          cursor: "pointer",
          color: "var(--n-400)",
          display: "flex",
          padding: "6px"
        }
      }, /*#__PURE__*/React.createElement(Icon, {
        name: reveal ? "eye-off" : "eye",
        size: 18
      }))
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: "12px"
      }
    }, /*#__PURE__*/React.createElement(Checkbox, {
      label: "Keep me signed in",
      defaultChecked: true
    }), /*#__PURE__*/React.createElement("a", {
      href: "#help",
      style: {
        font: "500 var(--f-sm)/1 var(--font-ui)"
      }
    }, "Forgot password?")), /*#__PURE__*/React.createElement(Button, {
      type: "submit",
      variant: "primary",
      size: "lg",
      block: true,
      loading: busy,
      onClick: submit
    }, busy ? "Signing in…" : "Sign in"), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: "400 var(--f-sm)/1.6 var(--font-ui)",
        color: "var(--text-muted)",
        textAlign: "center"
      }
    }, "Trouble signing in? Ask your packhouse manager, or call support on 021 555 0142."))));
  }
  Object.assign(window, {
    LoginScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/LoginScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/OverviewScreen.jsx
try { (() => {
(() => {
  const {
    StatTile,
    Card,
    Badge,
    Banner,
    Button,
    Icon,
    DataTable,
    Toggle
  } = window.ScanLinkDesignSystem_c824b0;
  const RECENT = [{
    serial: "SC-0093-AA",
    time: "07:14",
    block: "14",
    picker: "J. Mokoena",
    state: "Synced",
    tone: "success"
  }, {
    serial: "SC-0093-AB",
    time: "07:15",
    block: "14",
    picker: "J. Mokoena",
    state: "Synced",
    tone: "success"
  }, {
    serial: "SC-0094-AC",
    time: "07:16",
    block: "07",
    picker: "P. Naidoo",
    state: "Waiting",
    tone: "warning"
  }, {
    serial: "SC-0094-AD",
    time: "07:17",
    block: "07",
    picker: "P. Naidoo",
    state: "Waiting",
    tone: "warning"
  }];
  const COLS = [{
    key: "serial",
    header: "Serial",
    mono: true
  }, {
    key: "time",
    header: "Time",
    muted: true
  }, {
    key: "block",
    header: "Block"
  }, {
    key: "picker",
    header: "Picked by"
  }, {
    key: "state",
    header: "Status",
    render: r => /*#__PURE__*/React.createElement(Badge, {
      tone: r.tone,
      dot: true
    }, r.state)
  }];
  const DEVICES = [{
    name: "Line 3 scanner",
    detail: "SL-HH-02 · COM3",
    state: "Working",
    tone: "success"
  }, {
    name: "Line 1 scanner",
    detail: "SL-HH-05 · COM4",
    state: "Working",
    tone: "success"
  }, {
    name: "Line 2 scanner",
    detail: "SL-HH-11 · COM7",
    state: "Offline",
    tone: "error"
  }, {
    name: "Label printer",
    detail: "Argox · 192.168.1.44",
    state: "Working",
    tone: "success"
  }];
  function OverviewScreen({
    onNavigate,
    onSync,
    isHome,
    onSetHome
  }) {
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "16px",
        maxWidth: "1180px"
      }
    }, /*#__PURE__*/React.createElement(Banner, {
      tone: "warning",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "cloud-off"
      }),
      title: "42 scans haven't reached the cloud yet",
      action: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "secondary",
        onClick: onSync
      }, "Sync now")
    }, "They're safely saved on this computer. Sync them before you clean up local scans."), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "repeat(4,minmax(0,1fr))",
        gap: "12px"
      }
    }, /*#__PURE__*/React.createElement(StatTile, {
      caption: "Scanned today",
      value: "1,284",
      delta: "up 12% on yesterday",
      deltaTone: "up",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "scan-line",
        size: 16
      })
    }), /*#__PURE__*/React.createElement(StatTile, {
      caption: "Last hour",
      value: "96",
      footnote: "Steady with this morning.",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "clock",
        size: 16
      })
    }), /*#__PURE__*/React.createElement(StatTile, {
      caption: "Labels printed",
      value: "310",
      footnote: "Across 4 lines.",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "printer",
        size: 16
      })
    }), /*#__PURE__*/React.createElement(StatTile, {
      caption: "Waiting to sync",
      value: "42",
      delta: "sync when you're ready",
      deltaTone: "neutral",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "cloud-upload",
        size: 16
      })
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "minmax(0,1.6fr) minmax(0,1fr)",
        gap: "16px",
        alignItems: "start"
      }
    }, /*#__PURE__*/React.createElement(Card, {
      title: "Latest scans",
      subtitle: "The four most recent cartons scanned on this site.",
      padding: "0",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "ghost",
        iconEnd: /*#__PURE__*/React.createElement(Icon, {
          name: "arrow-right",
          size: 16
        }),
        onClick: () => onNavigate("scans")
      }, "See all scans")
    }, /*#__PURE__*/React.createElement(DataTable, {
      rowKey: "serial",
      columns: COLS,
      rows: RECENT
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "16px"
      }
    }, /*#__PURE__*/React.createElement(Card, {
      title: "Equipment",
      subtitle: "Green means it's sending data right now.",
      actions: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        variant: "ghost",
        onClick: () => onNavigate("devices")
      }, "Manage"),
      padding: "8px"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column"
      }
    }, DEVICES.map(d => /*#__PURE__*/React.createElement("div", {
      key: d.detail,
      style: {
        display: "flex",
        alignItems: "center",
        gap: "12px",
        padding: "10px 12px"
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "1px",
        flex: 1,
        minWidth: 0
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: "500 var(--f-sm)/1.3 var(--font-ui)",
        color: "var(--text-body)"
      }
    }, d.name), /*#__PURE__*/React.createElement("span", {
      style: {
        font: "400 var(--f-xs)/1.3 var(--font-mono)",
        color: "var(--text-muted)"
      }
    }, d.detail)), /*#__PURE__*/React.createElement(Badge, {
      tone: d.tone,
      dot: true
    }, d.state))))), /*#__PURE__*/React.createElement(Card, {
      title: "Start here",
      subtitle: "The two things most people do first."
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "8px"
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      block: true,
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "printer",
        size: 16
      }),
      onClick: () => onNavigate("labels")
    }, "Print labels"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      block: true,
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "scan-line",
        size: 16
      }),
      onClick: () => onNavigate("scans")
    }, "Look up a scan")), /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: "16px",
        paddingTop: "16px",
        borderTop: "1px solid var(--border-subtle)"
      }
    }, /*#__PURE__*/React.createElement(Toggle, {
      label: "Open ScanLink on this page",
      description: "Otherwise ScanLink opens on Scans.",
      checked: isHome,
      onChange: onSetHome
    }))))));
  }
  Object.assign(window, {
    OverviewScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/OverviewScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/ScansScreen.jsx
try { (() => {
(() => {
  const {
    useState,
    useMemo
  } = React;
  const {
    Card,
    DataTable,
    Pagination,
    Badge,
    Button,
    SearchField,
    Select,
    DateField,
    Icon,
    EmptyState,
    Dialog
  } = window.ScanLinkDesignSystem_c824b0;
  const PICKERS = ["J. Mokoena", "P. Naidoo", "T. van Wyk", "A. Dlamini"];
  const SUPPLIERS = ["Rooidraai", "Kleinbos", "Vaalkop", "Môrester"];
  const SCANS = Array.from({
    length: 46
  }, (_, i) => {
    const citrus = i % 2 === 1;
    return {
      serial: `SC-${(93 + i).toString().padStart(4, "0")}-A${String.fromCharCode(65 + i % 26)}`,
      time: `07:${String(14 + i % 45).padStart(2, "0")}`,
      block: String(7 + i % 3 * 7).padStart(2, "0"),
      line: String(1 + i % 3),
      picker: PICKERS[i % 4],
      supplier: SUPPLIERS[i % 4],
      product: citrus ? "Valencia 88" : "Hass Loose",
      state: i % 7 === 3 ? "Waiting" : "Synced",
      tone: i % 7 === 3 ? "warning" : "success"
    };
  });
  const RANGES = ["Today", "Last 7 days", "This season", "Custom"];
  function ScansScreen({
    onStatus
  }) {
    const [q, setQ] = useState("");
    const [range, setRange] = useState("Today");
    const [crop, setCrop] = useState("All crops");
    const [page, setPage] = useState(1);
    const [row, setRow] = useState(null);
    const size = 12;
    const filtered = useMemo(() => SCANS.filter(s => {
      const t = `${s.serial} ${s.block} ${s.supplier} ${s.picker} ${s.product}`.toLowerCase();
      const okQ = t.includes(q.toLowerCase());
      const okC = crop === "All crops" || (crop === "Avocado" ? s.product === "Hass Loose" : s.product === "Valencia 88");
      return okQ && okC;
    }), [q, crop]);
    const pageCount = Math.max(1, Math.ceil(filtered.length / size));
    const rows = filtered.slice((page - 1) * size, page * size);
    const cols = [{
      key: "serial",
      header: "Serial",
      mono: true
    }, {
      key: "time",
      header: "Time",
      muted: true
    }, {
      key: "block",
      header: "Block"
    }, {
      key: "line",
      header: "Line"
    }, {
      key: "picker",
      header: "Picked by"
    }, {
      key: "supplier",
      header: "Supplier"
    }, {
      key: "product",
      header: "Product"
    }, {
      key: "state",
      header: "Status",
      render: r => /*#__PURE__*/React.createElement(Badge, {
        tone: r.tone,
        dot: true
      }, r.state)
    }];
    return /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "16px",
        maxWidth: "1180px"
      }
    }, /*#__PURE__*/React.createElement(Card, {
      padding: "14px 16px"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: "12px",
        flexWrap: "wrap"
      }
    }, /*#__PURE__*/React.createElement(SearchField, {
      width: "300px",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "search",
        size: 16
      }),
      value: q,
      onChange: e => {
        setQ(e.target.value);
        setPage(1);
      },
      onClear: () => setQ(""),
      placeholder: "Search serial, block, supplier or picker\u2026"
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: "4px",
        padding: "3px",
        background: "var(--n-100)",
        borderRadius: "var(--r-sm)"
      }
    }, RANGES.map(r => /*#__PURE__*/React.createElement("button", {
      key: r,
      type: "button",
      onClick: () => setRange(r),
      style: {
        padding: "6px 12px",
        border: "none",
        borderRadius: "var(--r-xs)",
        cursor: "pointer",
        background: range === r ? "var(--surface-card)" : "transparent",
        boxShadow: range === r ? "var(--e-1)" : "none",
        color: range === r ? "var(--text-heading)" : "var(--text-muted)",
        font: `${range === r ? 600 : 500} var(--f-sm)/1 var(--font-ui)`
      }
    }, r))), range === "Custom" ? /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: "8px"
      }
    }, /*#__PURE__*/React.createElement(DateField, {
      defaultValue: "2026-08-01",
      width: "150px"
    }), /*#__PURE__*/React.createElement(DateField, {
      defaultValue: "2026-08-10",
      width: "150px"
    })) : null, /*#__PURE__*/React.createElement(Select, {
      width: "160px",
      value: crop,
      onChange: e => {
        setCrop(e.target.value);
        setPage(1);
      },
      options: ["All crops", "Avocado", "Citrus"]
    }), /*#__PURE__*/React.createElement("span", {
      style: {
        marginLeft: "auto",
        font: "400 var(--f-sm)/1 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, filtered.length, " scan", filtered.length === 1 ? "" : "s"))), /*#__PURE__*/React.createElement(Card, {
      padding: "0",
      footer: /*#__PURE__*/React.createElement(Pagination, {
        page: page,
        pageCount: pageCount,
        total: filtered.length,
        pageSize: size,
        onPrevious: () => setPage(p => Math.max(1, p - 1)),
        onNext: () => setPage(p => Math.min(pageCount, p + 1))
      })
    }, /*#__PURE__*/React.createElement(DataTable, {
      rowKey: "serial",
      columns: cols,
      rows: rows,
      selectedId: row && row.serial,
      onRowClick: r => {
        setRow(r);
        onStatus(`Looking at ${r.serial}`, "info");
      },
      empty: /*#__PURE__*/React.createElement(EmptyState, {
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "search-x",
          size: 22
        }),
        title: "No scans match that search",
        description: "Try a shorter search, or widen the date range to Last 7 days.",
        action: /*#__PURE__*/React.createElement(Button, {
          variant: "secondary",
          size: "sm",
          onClick: () => {
            setQ("");
            setCrop("All crops");
          }
        }, "Clear filters")
      })
    })), row ? /*#__PURE__*/React.createElement(Dialog, {
      title: row.serial,
      description: "Everything recorded when this carton was scanned.",
      onClose: () => setRow(null),
      width: "460px",
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        onClick: () => setRow(null)
      }, "Close"), /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        icon: /*#__PURE__*/React.createElement(Icon, {
          name: "printer",
          size: 16
        })
      }, "Reprint this label"))
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "auto 1fr",
        gap: "10px 20px"
      }
    }, [["Time", row.time], ["Block", row.block], ["Line", row.line], ["Picked by", row.picker], ["Supplier", row.supplier], ["Product", row.product]].map(([k, v]) => /*#__PURE__*/React.createElement(React.Fragment, {
      key: k
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: "400 var(--f-sm)/1.4 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, k), /*#__PURE__*/React.createElement("span", {
      style: {
        font: "500 var(--f-sm)/1.4 var(--font-ui)",
        color: "var(--text-body)"
      }
    }, v)))), /*#__PURE__*/React.createElement(Badge, {
      tone: row.tone,
      dot: true
    }, row.state === "Synced" ? "Sent to the cloud" : "Waiting to be sent")) : null);
  }
  Object.assign(window, {
    ScansScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/ScansScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/scanlink-client/SiteSelectionScreen.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
(() => {
  const {
    useState
  } = React;
  const {
    SiteTile,
    SearchField,
    Icon,
    EmptyState
  } = window.ScanLinkDesignSystem_c824b0;
  const SITES = [{
    name: "Rooidraai Packhouse",
    id: "SL-2201",
    role: "Owner"
  }, {
    name: "Kleinbos Cold Store",
    id: "SL-2208",
    role: "Employee"
  }, {
    name: "Vaalkop Line 3",
    id: "SL-2214",
    role: "Employee"
  }, {
    name: "Môrester Citrus",
    id: "SL-2231",
    role: "Employee"
  }, {
    name: "Bergsig Avocado",
    id: "SL-2244",
    role: "Employee"
  }];
  function SiteSelectionScreen({
    onSelect
  }) {
    const [q, setQ] = useState("");
    const hits = SITES.filter(s => (s.name + s.id).toLowerCase().includes(q.toLowerCase()));
    return /*#__PURE__*/React.createElement("div", {
      style: {
        height: "100%",
        overflowY: "auto",
        background: "var(--surface-app)",
        display: "grid",
        placeItems: "start center",
        padding: "56px 24px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: "100%",
        maxWidth: "680px",
        display: "flex",
        flexDirection: "column",
        gap: "24px"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        flexDirection: "column",
        gap: "6px"
      }
    }, /*#__PURE__*/React.createElement("h1", {
      style: {
        margin: 0,
        font: "600 var(--f-3xl)/1.25 var(--font-ui)",
        color: "var(--text-heading)"
      }
    }, "Which site are you working on?"), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: "400 var(--f-md)/1.5 var(--font-ui)",
        color: "var(--text-muted)"
      }
    }, "You can switch site at any time from the bottom of the menu.")), /*#__PURE__*/React.createElement(SearchField, {
      width: "100%",
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "search",
        size: 16
      }),
      value: q,
      onChange: e => setQ(e.target.value),
      onClear: () => setQ(""),
      placeholder: "Search by site name or number\u2026"
    }), hits.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
      icon: /*#__PURE__*/React.createElement(Icon, {
        name: "search-x",
        size: 22
      }),
      title: "No sites match that",
      description: "Check the spelling, or clear the search to see all five sites."
    }) : /*#__PURE__*/React.createElement("div", {
      style: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: "12px"
      }
    }, hits.map(s => /*#__PURE__*/React.createElement(SiteTile, _extends({
      key: s.id
    }, s, {
      onClick: () => onSelect(s)
    }))))));
  }
  Object.assign(window, {
    SiteSelectionScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/scanlink-client/SiteSelectionScreen.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Button = __ds_scope.Button;

__ds_ns.Icon = __ds_scope.Icon;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.DataTable = __ds_scope.DataTable;

__ds_ns.Pagination = __ds_scope.Pagination;

__ds_ns.SiteTile = __ds_scope.SiteTile;

__ds_ns.StatTile = __ds_scope.StatTile;

__ds_ns.Banner = __ds_scope.Banner;

__ds_ns.ProgressBar = __ds_scope.ProgressBar;

__ds_ns.StatusBar = __ds_scope.StatusBar;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.DateField = __ds_scope.DateField;

__ds_ns.NumberField = __ds_scope.NumberField;

__ds_ns.SearchField = __ds_scope.SearchField;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Slider = __ds_scope.Slider;

__ds_ns.FieldLabel = __ds_scope.FieldLabel;

__ds_ns.TextField = __ds_scope.TextField;

__ds_ns.Toggle = __ds_scope.Toggle;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.Dialog = __ds_scope.Dialog;

__ds_ns.EmptyState = __ds_scope.EmptyState;

__ds_ns.FieldSet = __ds_scope.FieldSet;

__ds_ns.Sidebar = __ds_scope.Sidebar;

__ds_ns.SidebarItem = __ds_scope.SidebarItem;

__ds_ns.TopBar = __ds_scope.TopBar;

})();
