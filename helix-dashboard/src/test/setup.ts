import "@testing-library/jest-dom";
import React from "react";

vi.mock("scichart", () => ({
  SciChartSurface: {
    setRuntimeLicenseKey: vi.fn(),
    create: vi.fn().mockResolvedValue({
      sciChartSurface: {
        xAxes: { add: vi.fn() },
        yAxes: { add: vi.fn() },
        renderableSeries: { add: vi.fn() },
        delete: vi.fn(),
      },
      wasmContext: {},
    }),
    loadWasmFromCDN: vi.fn(),
  },
  NumericAxis: vi.fn(),
  FastLineRenderableSeries: vi.fn(),
  XyDataSeries: vi.fn(() => ({ append: vi.fn() })),
  SciChartJsNavyTheme: vi.fn(),
}));

vi.mock("scichart-react", () => ({
  SciChartReact: () =>
    React.createElement("div", { "data-testid": "scichart-surface" }),
}));
