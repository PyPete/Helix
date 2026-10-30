import "@testing-library/jest-dom";
import React from "react";

// Clear all mock call histories before each test so that RTL's deferred cleanup
// (which runs after user afterEach but before the next test's beforeEach) does not
// bleed call counts across tests within the same file. Implementations are preserved.
beforeEach(() => {
  vi.clearAllMocks();
});

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
