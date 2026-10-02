(() => {
const { useState } = React;
const { Button, TextField, Icon, Banner, Checkbox } = window.ScanLinkDesignSystem_c824b0;

function LoginScreen({ onLogin }) {
  const [email, setEmail] = useState("");
  const [pw, setPw] = useState("");
  const [reveal, setReveal] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState("");

  const submit = (e) => {
    if (e) e.preventDefault();
    if (!email.trim()) { setErr("Enter the email address you use for ScanLink."); return; }
    setErr(""); setBusy(true);
    setTimeout(() => { setBusy(false); onLogin(); }, 850);
  };

  return (
    <div style={{ height: "100%", display: "grid", gridTemplateColumns: "minmax(0,1.05fr) minmax(420px,.95fr)", background: "var(--surface-app)" }}>
      <aside style={{ background: "var(--navy-900)", padding: "48px", display: "flex", flexDirection: "column", justifyContent: "space-between", position: "relative", overflow: "hidden" }}>
        <div style={{ position: "absolute", right: "-140px", top: "-120px", width: "440px", height: "440px", borderRadius: "50%", background: "rgba(77,74,234,.20)" }} />
        <div style={{ position: "absolute", right: "60px", bottom: "-180px", width: "320px", height: "320px", borderRadius: "50%", background: "rgba(255,255,255,.035)" }} />
        <img src="../../assets/scanlink-logo-white.png" alt="ScanLink" style={{ width: "168px", position: "relative" }} />
        <div style={{ position: "relative", display: "flex", flexDirection: "column", gap: "20px", maxWidth: "460px" }}>
          <h2 style={{ margin: 0, font: "600 var(--f-4xl)/1.25 var(--font-ui)", color: "#fff" }}>
            Every carton scanned, counted and labelled — without leaving this screen.
          </h2>
          <ul style={{ margin: 0, padding: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: "12px" }}>
            {[["scan-line", "Watch scans land from every line, live"],
              ["printer", "Print barcode and carton labels in three clicks"],
              ["cloud-upload", "Nothing is lost if the internet drops"]].map(([ic, t]) => (
              <li key={t} style={{ display: "flex", alignItems: "center", gap: "12px", color: "rgba(255,255,255,.78)", font: "400 var(--f-md)/1.5 var(--font-ui)" }}>
                <span style={{ width: "30px", height: "30px", flex: "none", borderRadius: "var(--r-sm)", background: "rgba(255,255,255,.10)", color: "#fff", display: "grid", placeItems: "center" }}><Icon name={ic} size={16} /></span>
                {t}
              </li>
            ))}
          </ul>
        </div>
        <span style={{ position: "relative", font: "400 var(--f-xs)/1.4 var(--font-ui)", color: "rgba(255,255,255,.42)" }}>ScanLink for packhouses · v2.0</span>
      </aside>

      <main style={{ display: "grid", placeItems: "center", padding: "40px 24px", overflowY: "auto" }}>
        <form onSubmit={submit} style={{ width: "100%", maxWidth: "380px", display: "flex", flexDirection: "column", gap: "20px" }}>
          <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
            <h1 style={{ margin: 0, font: "600 var(--f-3xl)/1.25 var(--font-ui)", color: "var(--text-heading)" }}>Sign in</h1>
            <p style={{ margin: 0, font: "400 var(--f-md)/1.5 var(--font-ui)", color: "var(--text-muted)" }}>Use the email and password your manager set up for you.</p>
          </div>

          {err ? <Banner tone="error" icon={<Icon name="alert-circle" />}>{err}</Banner> : null}

          <TextField label="Email address" required type="email" placeholder="you@packhouse.co"
            value={email} onChange={(e) => setEmail(e.target.value)} size="lg" />

          <TextField label="Password" required size="lg" type={reveal ? "text" : "password"}
            value={pw} onChange={(e) => setPw(e.target.value)}
            suffix={<button type="button" onClick={() => setReveal(!reveal)} aria-label={reveal ? "Hide password" : "Show password"}
              style={{ border: "none", background: "transparent", cursor: "pointer", color: "var(--n-400)", display: "flex", padding: "6px" }}>
              <Icon name={reveal ? "eye-off" : "eye"} size={18} /></button>} />

          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "12px" }}>
            <Checkbox label="Keep me signed in" defaultChecked />
            <a href="#help" style={{ font: "500 var(--f-sm)/1 var(--font-ui)" }}>Forgot password?</a>
          </div>

          <Button type="submit" variant="primary" size="lg" block loading={busy} onClick={submit}>
            {busy ? "Signing in…" : "Sign in"}
          </Button>

          <p style={{ margin: 0, font: "400 var(--f-sm)/1.6 var(--font-ui)", color: "var(--text-muted)", textAlign: "center" }}>
            Trouble signing in? Ask your packhouse manager, or call support on 021 555 0142.
          </p>
        </form>
      </main>
    </div>
  );
}

Object.assign(window, { LoginScreen });
})();
