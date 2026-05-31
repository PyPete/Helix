import "allotment/dist/style.css";
import { Allotment } from "allotment";
import { useState } from "react";

/* ── Helix Shell Spike ──────────────────────────────────────────
   Validates: Allotment renders the Sketch-002A IDE shell layout
   inside React 18 + Vite.  Swap into App.tsx return to test.
────────────────────────────────────────────────────────────────── */

const css = `
  .spike-shell {
    height: 100vh;
    display: flex;
    flex-direction: column;
    background: #1e1e2e;
    color: #cdd6f4;
    font-family: 'Segoe UI', system-ui, sans-serif;
    font-size: 13px;
  }
  /* Ribbon stub */
  .spike-ribbon {
    height: 30px;
    background: #1a1a2a;
    border-bottom: 1px solid #3e3e5a;
    display: flex;
    align-items: center;
    padding: 0 10px;
    gap: 10px;
    flex-shrink: 0;
    font-size: 11px;
    color: #7f849c;
  }
  .spike-ribbon .logo { font-weight: 700; color: #9d7fff; }
  .spike-ribbon .rtab {
    padding: 3px 12px;
    border-radius: 2px 2px 0 0;
    cursor: pointer;
    transition: background 0.12s;
  }
  .spike-ribbon .rtab:hover { background: #252535; color: #cdd6f4; }
  .spike-ribbon .rtab.active { background: #2d2d42; color: #cdd6f4; }
  /* Workspace fills remaining height */
  .spike-workspace {
    flex: 1;
    min-height: 0;         /* critical for allotment in flex column */
    overflow: hidden;
  }
  /* Panel chrome */
  .spike-panel {
    height: 100%;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    background: #252535;
  }
  .spike-panel-title {
    background: #252535;
    border-bottom: 1px solid #3e3e5a;
    padding: 4px 8px;
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: #7f849c;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .spike-panel-body {
    flex: 1;
    overflow-y: auto;
    padding: 6px;
  }
  .spike-panel-body::-webkit-scrollbar { width: 5px; }
  .spike-panel-body::-webkit-scrollbar-thumb { background: #3e3e5a; border-radius: 3px; }
  /* Device items */
  .spike-dev { padding: 4px 8px; border-radius: 3px; cursor: pointer; display: flex; align-items: center; gap: 6px; transition: background 0.1s; }
  .spike-dev:hover { background: #2d2d42; }
  .spike-dev.sel { background: #3a2f6e; color: #9d7fff; }
  .spike-dot { width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0; }
  .spike-grp { font-size: 10px; color: #585b70; text-transform: uppercase; letter-spacing: .06em; padding: 5px 4px 2px; }
  /* Document tabs */
  .spike-doctabs {
    background: #252535;
    border-bottom: 1px solid #3e3e5a;
    display: flex;
    overflow-x: auto;
    flex-shrink: 0;
  }
  .spike-doctabs::-webkit-scrollbar { height: 3px; }
  .spike-doctab {
    display: flex; align-items: center; gap: 5px;
    padding: 5px 14px;
    font-size: 12px;
    color: #7f849c;
    cursor: pointer;
    border-right: 1px solid #3e3e5a;
    white-space: nowrap;
    transition: all 0.12s;
    flex-shrink: 0;
  }
  .spike-doctab:hover { background: #2d2d42; color: #cdd6f4; }
  .spike-doctab.active { background: #1e1e2e; color: #cdd6f4; border-top: 2px solid #9d7fff; }
  /* Canvas */
  .spike-canvas {
    flex: 1;
    position: relative;
    overflow: hidden;
    background: radial-gradient(ellipse at center, #2a2a3e 0%, #1e1e2e 70%);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    gap: 8px;
    color: #585b70;
  }
  .spike-canvas-grid {
    position: absolute; inset: 0;
    background-image: radial-gradient(circle, #3e3e5a 1px, transparent 1px);
    background-size: 24px 24px;
    opacity: 0.3;
    pointer-events: none;
  }
  /* Bottom log */
  .spike-btabs { background: #252535; display: flex; border-bottom: 1px solid #3e3e5a; flex-shrink: 0; }
  .spike-btab { padding: 3px 12px; font-size: 11px; color: #7f849c; cursor: pointer; border-right: 1px solid #3e3e5a; transition: all 0.12s; }
  .spike-btab:hover { color: #cdd6f4; background: #2d2d42; }
  .spike-btab.active { color: #cdd6f4; background: #1e1e2e; border-top: 2px solid #9d7fff; }
  .spike-blog { padding: 5px 10px; font-size: 11px; font-family: 'Cascadia Code', monospace; color: #7f849c; overflow-y: auto; flex: 1; }
  /* Status bar */
  .spike-status {
    height: 20px;
    background: #3a2f6e;
    border-top: 1px solid #9d7fff;
    display: flex;
    align-items: center;
    padding: 0 8px;
    gap: 12px;
    font-size: 11px;
    color: #9d7fff;
    flex-shrink: 0;
  }
  .spike-sdot { width: 6px; height: 6px; border-radius: 50%; display: inline-block; margin-right: 3px; }
  /* Override allotment sash color to match theme */
  .split-view-view > .sash-container > .sash { background: #3e3e5a !important; }
  .split-view-view > .sash-container > .sash:hover { background: #9d7fff !important; }
`;

const logs = [
  { c: "#89dceb", t: "[10:42:31] Agent connected — localhost:5000" },
  { c: "#a6e3a1", t: "[10:42:31] VapourtecR4 online (COM3)" },
  { c: "#a6e3a1", t: "[10:42:32] KnauerAzura online (192.168.1.101)" },
  { c: "#f9e2af", t: "[10:42:33] SystagOpcUa — connection timeout" },
  { c: "#89dceb", t: "[10:42:40] Capability registry: 24 capabilities loaded" },
];

export function HelixShellSpike() {
  const [activeTab, setActiveTab] = useState(0);
  const [activeDoc, setActiveDoc] = useState(0);
  const [activeBottom, setActiveBottom] = useState(0);
  const [selectedDev, setSelectedDev] = useState("VapourtecR4");

  const docs = ["⬡ Workflow Builder", "📈 Temperature", "🔬 VapourtecR4 Control", "⏱ Timeline"];
  const bottomTabs = ["Output", "Device Log", "Events", "Python"];
  const ribbonTabs = ["Home", "Devices", "Trends", "Workflows", "Charts", "Optimisation", "Settings"];

  return (
    <>
      <style>{css}</style>
      <div className="spike-shell">
        {/* Ribbon */}
        <div className="spike-ribbon">
          <span className="logo">Helix</span>
          {ribbonTabs.map((t, i) => (
            <span key={t} className={`rtab${activeTab === i ? " active" : ""}`} onClick={() => setActiveTab(i)}>{t}</span>
          ))}
        </div>

        {/* Allotment workspace */}
        <div className="spike-workspace">
          <Allotment proportionalLayout={false}>
            {/* Left: Device Explorer */}
            <Allotment.Pane preferredSize={220} minSize={100} snap>
              <div className="spike-panel">
                <div className="spike-panel-title">🔬 Device Explorer</div>
                <div className="spike-panel-body">
                  <div className="spike-grp">▾ Flow Controllers</div>
                  {["VapourtecR4", "KnauerAzura"].map(d => (
                    <div key={d} className={`spike-dev${selectedDev === d ? " sel" : ""}`} onClick={() => setSelectedDev(d)}>
                      <span className="spike-dot" style={{ background: "#a6e3a1" }} />
                      {d}
                    </div>
                  ))}
                  <div className="spike-grp" style={{ marginTop: 6 }}>▾ Spectroscopy</div>
                  <div className="spike-dev" onClick={() => setSelectedDev("SystagOpcUa")}>
                    <span className="spike-dot" style={{ background: "#f38ba8" }} />SystagOpcUa
                  </div>
                  <div className="spike-grp" style={{ marginTop: 6 }}>▾ Sensors</div>
                  {["TempProbe-01", "UVDetector"].map((d, i) => (
                    <div key={d} className="spike-dev" onClick={() => setSelectedDev(d)}>
                      <span className="spike-dot" style={{ background: i === 0 ? "#a6e3a1" : "#f9e2af" }} />{d}
                    </div>
                  ))}
                </div>
              </div>
            </Allotment.Pane>

            {/* Center: doc tabs + canvas + bottom strip */}
            <Allotment.Pane>
              <div className="spike-panel" style={{ background: "#1e1e2e" }}>
                {/* Document tabs */}
                <div className="spike-doctabs">
                  {docs.map((d, i) => (
                    <div key={d} className={`spike-doctab${activeDoc === i ? " active" : ""}`} onClick={() => setActiveDoc(i)}>
                      {d}
                    </div>
                  ))}
                </div>

                {/* Nested vertical allotment: canvas + bottom */}
                <div style={{ flex: 1, minHeight: 0 }}>
                  <Allotment vertical proportionalLayout={false}>
                    <Allotment.Pane>
                      <div className="spike-canvas">
                        <div className="spike-canvas-grid" />
                        <div style={{ fontSize: 32, opacity: 0.2, zIndex: 1 }}>⬡</div>
                        <span style={{ zIndex: 1, fontSize: 12 }}>
                          {docs[activeDoc]} — right-click to add nodes
                        </span>
                        <span style={{ zIndex: 1, fontSize: 11, color: "#3e3e5a" }}>
                          Resize handles: drag edges · Double-click to collapse
                        </span>
                      </div>
                    </Allotment.Pane>
                    <Allotment.Pane preferredSize={130} minSize={60} snap>
                      <div className="spike-panel" style={{ background: "#1e1e2e" }}>
                        <div className="spike-btabs">
                          {bottomTabs.map((t, i) => (
                            <div key={t} className={`spike-btab${activeBottom === i ? " active" : ""}`} onClick={() => setActiveBottom(i)}>{t}</div>
                          ))}
                        </div>
                        <div className="spike-blog">
                          {logs.map((l, i) => (
                            <div key={i} style={{ color: l.c, padding: "1px 0" }}>{l.t}</div>
                          ))}
                        </div>
                      </div>
                    </Allotment.Pane>
                  </Allotment>
                </div>
              </div>
            </Allotment.Pane>

            {/* Right: Trend Explorer */}
            <Allotment.Pane preferredSize={200} minSize={100} snap>
              <div className="spike-panel">
                <div className="spike-panel-title">📈 Trend Explorer</div>
                <div className="spike-panel-body">
                  <div className="spike-grp">▾ Raw Trends</div>
                  {["vapourtec.temperature", "vapourtec.flow.pumpA", "vapourtec.flow.pumpB", "knauer.pump.flowrate", "knauer.pump.pressure"].map(t => (
                    <div key={t} className="spike-dev">📈 {t}</div>
                  ))}
                  <div className="spike-grp" style={{ marginTop: 6 }}>▾ Spectral</div>
                  <div className="spike-dev">🌈 UV:300–800nm</div>
                  <div className="spike-dev">🌈 MIR:1000–4000cm⁻¹</div>
                  <div className="spike-grp" style={{ marginTop: 6 }}>▾ Safety</div>
                  <div className="spike-dev">🛡 temp.overtemp</div>
                  <div className="spike-dev">🛡 pressure.maxpsi</div>
                </div>
              </div>
            </Allotment.Pane>
          </Allotment>
        </div>

        {/* Status bar */}
        <div className="spike-status">
          <span><span className="spike-sdot" style={{ background: "#a6e3a1" }} />Agent: localhost:5000</span>
          <span><span className="spike-sdot" style={{ background: "#a6e3a1" }} />VapourtecR4</span>
          <span><span className="spike-sdot" style={{ background: "#f38ba8" }} />SystagOpcUa Offline</span>
          <span style={{ marginLeft: "auto" }}>Selected: {selectedDev} · Idle · Helix v0.1.0</span>
        </div>
      </div>
    </>
  );
}
