import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router";
import { SciChartSurface } from "scichart";
import { App } from "./App";
import "./themes/default.css";

// D-01: must be called once before any SciChartSurface is instantiated.
// Empty string = community mode (watermark). Set VITE_SCICHART_KEY for production.
SciChartSurface.setRuntimeLicenseKey(import.meta.env.VITE_SCICHART_KEY ?? "");

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
);
