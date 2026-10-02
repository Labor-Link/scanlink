(() => {
const { useState } = React;
const { Card, FieldSet, Select, NumberField, Slider, Checkbox, Button, Icon, Badge, ProgressBar, Banner } = window.ScanLinkDesignSystem_c824b0;

const STEPS = ["What are you labelling?", "How many?", "Check and print"];

function Sticker({ picker, product, count, twoUp }) {
  const one = (
    <div style={{ width: "196px", background: "#fff", border: "1px solid var(--border-strong)", borderRadius: "var(--r-xs)", padding: "12px", display: "flex", flexDirection: "column", gap: "8px" }}>
      <div style={{ font: "700 11px/1.4 var(--font-mono)", color: "var(--n-900)" }}>{product}<br />PICKER {picker}</div>
      <div style={{ display: "flex", gap: "1px", alignItems: "flex-end", height: "38px" }}>
        {Array.from({ length: 40 }, (_, i) => <span key={i} style={{ width: i % 3 ? "2px" : "3px", height: "100%", background: i % 4 === 0 ? "transparent" : "#111" }} />)}
      </div>
      <div style={{ font: "700 10px/1 var(--font-mono)", letterSpacing: ".1em", color: "var(--n-900)" }}>SC-0093-A{count % 10}</div>
    </div>
  );
  return <div style={{ display: "flex", gap: "12px", justifyContent: "center", padding: "20px", background: "var(--n-100)", borderRadius: "var(--r-md)" }}>{one}{twoUp ? one : null}</div>;
}

function LabelsScreen({ onStatus }) {
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
    setProgress(20); setDone(false);
    setTimeout(() => setProgress(70), 250);
    setTimeout(() => { setProgress(100); setDone(true); onStatus(`${count} labels sent to the printer`, "success"); }, 700);
  };

  return (
    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(340px,1fr))", gap: "16px", alignItems: "start", maxWidth: "1080px" }}>
      <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
          {STEPS.map((s, i) => (
            <button key={s} type="button" onClick={() => setStep(i)}
              style={{ flex: "1 1 180px", display: "flex", alignItems: "center", gap: "10px", padding: "12px 14px", cursor: "pointer", textAlign: "left",
                background: "var(--surface-card)", borderRadius: "var(--r-md)",
                border: `1px solid ${i === step ? "var(--action-primary)" : "var(--border-default)"}`,
                boxShadow: i === step ? "var(--ring-focus)" : "var(--e-1)" }}>
              <span style={{ width: "24px", height: "24px", flex: "none", borderRadius: "50%", display: "grid", placeItems: "center",
                background: i < step ? "var(--ok-500)" : i === step ? "var(--action-primary)" : "var(--n-100)",
                color: i <= step ? "#fff" : "var(--n-500)", font: "600 var(--f-xs)/1 var(--font-ui)" }}>
                {i < step ? <Icon name="check" size={14} /> : i + 1}
              </span>
              <span style={{ font: `${i === step ? 600 : 500} var(--f-sm)/1.3 var(--font-ui)`, color: i === step ? "var(--text-heading)" : "var(--text-muted)" }}>{s}</span>
            </button>
          ))}
        </div>

        {step === 0 ? (
          <Card title="What are you labelling?" subtitle="Pick the crop first — the product list narrows to match."
            footer={<Button variant="primary" iconEnd={<Icon name="arrow-right" size={16} />} onClick={() => setStep(1)}>Next: how many?</Button>}>
            <FieldSet columns={2}>
              <Select label="Crop" required value={crop} onChange={(e) => setCrop(e.target.value)} options={["Avocado (01)", "Citrus (02)"]} />
              <Select label="Product" required value={product} onChange={(e) => setProduct(e.target.value)} options={["Hass Loose (118)", "Valencia 88 (204)"]} />
              <Select label="Who is picking?" required value={picker} onChange={(e) => setPicker(e.target.value)}
                hint="Start typing a number or a name." options={["4471 — J. Mokoena", "4472 — P. Naidoo", "4480 — T. van Wyk"]} />
              <div style={{ alignSelf: "end", display: "flex", flexDirection: "column", gap: "4px" }}>
                <span style={{ font: "400 var(--f-xs)/1.4 var(--font-ui)", color: "var(--text-muted)" }}>This combination prints as</span>
                <Badge tone="brand">Grade 1 · Count 18 · Carton M4 · 4.0 kg</Badge>
              </div>
            </FieldSet>
          </Card>
        ) : null}

        {step === 1 ? (
          <Card title="How many labels?" subtitle="You can change this before printing."
            footer={<div style={{ display: "flex", gap: "8px" }}>
              <Button variant="secondary" icon={<Icon name="arrow-left" size={16} />} onClick={() => setStep(0)}>Back</Button>
              <Button variant="primary" iconEnd={<Icon name="arrow-right" size={16} />} onClick={() => setStep(2)}>Next: check and print</Button>
            </div>}>
            <div style={{ display: "flex", alignItems: "flex-end", gap: "12px", flexWrap: "wrap" }}>
              <NumberField label="Number of labels" value={count} min={1} max={200} onChange={(e) => setCount(+e.target.value)} width="180px" />
              {[6, 12, 24, 48].map((n) => (
                <Button key={n} variant={count === n ? "primary" : "secondary"} size="sm" onClick={() => setCount(n)}>{n}</Button>
              ))}
            </div>
            <div style={{ marginTop: "16px" }}>
              <Checkbox label="Print two labels side by side" description="Uses half as much label roll." checked={twoUp} onChange={(e) => setTwoUp(e.target.checked)} />
            </div>
          </Card>
        ) : null}

        {step === 2 ? (
          <Card title="Check and print" subtitle="Compare the preview with the label roll in the printer."
            footer={<div style={{ display: "flex", gap: "8px", alignItems: "center", width: "100%" }}>
              <Button variant="secondary" icon={<Icon name="arrow-left" size={16} />} onClick={() => setStep(1)}>Back</Button>
              <Button variant="primary" icon={<Icon name="printer" size={16} />} onClick={print}>Print {count} labels</Button>
              <span style={{ marginLeft: "auto", font: "400 var(--f-xs)/1.4 var(--font-ui)", color: "var(--text-muted)" }}>Printer: Argox · 192.168.1.44</span>
            </div>}>
            <div style={{ display: "grid", gridTemplateColumns: "auto 1fr", gap: "10px 20px" }}>
              {[["Product", product], ["Crop", crop], ["Picker", picker], ["Labels", `${count}${twoUp ? " (two per row)" : ""}`]].map(([k, v]) => (
                <React.Fragment key={k}>
                  <span style={{ font: "400 var(--f-sm)/1.4 var(--font-ui)", color: "var(--text-muted)" }}>{k}</span>
                  <span style={{ font: "500 var(--f-sm)/1.4 var(--font-ui)" }}>{v}</span>
                </React.Fragment>
              ))}
            </div>
            {progress > 0 ? (
              <div style={{ marginTop: "16px" }}>
                {done ? <Banner tone="success" icon={<Icon name="check-circle-2" />} title={`${count} labels sent to the printer`}>If nothing comes out, check the roll and try again.</Banner>
                  : <ProgressBar value={progress} label={`Sending ${count} labels to the printer…`} />}
              </div>
            ) : null}
          </Card>
        ) : null}

        <Card title="Printer settings" subtitle="Most people never need to change these."
          actions={<Button size="sm" variant="ghost" iconEnd={<Icon name={advanced ? "chevron-up" : "chevron-down"} size={16} />} onClick={() => setAdvanced(!advanced)}>{advanced ? "Hide" : "Show"}</Button>}>
          {advanced ? (
            <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>
              <FieldSet title="Label size" description="Match these to the label roll in the printer." columns={4}>
                <NumberField label="Width" defaultValue={40} unit="mm" width="100%" />
                <NumberField label="Height" defaultValue={25} unit="mm" width="100%" />
                <NumberField label="Gap" defaultValue={3} unit="mm" width="100%" />
                <NumberField label="Resolution" defaultValue={203} unit="dpi" width="100%" />
              </FieldSet>
              <FieldSet title="Print quality" columns={2}>
                <Select label="Barcode type" options={["Code 128", "Code 39", "EAN-13"]} />
                <Select label="Speed" options={["2 ips", "4 ips", "6 ips"]} />
              </FieldSet>
              <Slider label="Print darkness" value={darkness} onChange={(e) => setDarkness(+e.target.value)} minLabel="Lighter" maxLabel="Darker" width="320px" />
            </div>
          ) : (
            <p style={{ margin: 0, font: "400 var(--f-sm)/1.5 var(--font-ui)", color: "var(--text-muted)" }}>
              40 × 25 mm labels, Code 128, 203 dpi. Change these only if the printed labels come out wrong.
            </p>
          )}
        </Card>
      </div>

      <Card title="Preview" subtitle="This is what will come out of the printer.">
        <Sticker picker={picker.split("—")[0].trim()} product={product} count={count} twoUp={twoUp} />
        <p style={{ margin: "12px 0 0", font: "400 var(--f-xs)/1.5 var(--font-ui)", color: "var(--text-muted)" }}>
          Shown at roughly actual size for a 40 × 25 mm label.
        </p>
      </Card>
    </div>
  );
}

Object.assign(window, { LabelsScreen });
})();
