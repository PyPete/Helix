import { useEffect, useRef, useState } from "react";
import type React from "react";
import { NodeEditor } from "rete";
import { AreaPlugin, AreaExtensions } from "rete-area-plugin";
import { ConnectionPlugin, Presets as ConnectionPresets } from "rete-connection-plugin";
import { ReactPlugin, Presets as ReactPresets } from "rete-react-plugin";
import type { ClassicScheme, ReactArea2D } from "rete-react-plugin";
import { createRoot } from "react-dom/client";
import type { WslDocument } from "../../types/agent";
import { exportToWsl } from "./nodes/HelixNodes";

type AreaExtra = ReactArea2D<ClassicScheme>;

export interface UseReteEditorResult {
  editor: NodeEditor<ClassicScheme> | null;
  area: AreaPlugin<ClassicScheme, AreaExtra> | null;
  exportWsl: (name: string) => WslDocument | null;
}

export function useReteEditor(
  containerRef: React.RefObject<HTMLDivElement | null>
): UseReteEditorResult {
  const editorRef = useRef<NodeEditor<ClassicScheme> | null>(null);
  const areaRef = useRef<AreaPlugin<ClassicScheme, AreaExtra> | null>(null);
  const [, forceRender] = useState(0);

  useEffect(() => {
    if (!containerRef.current) return;

    const editor = new NodeEditor<ClassicScheme>();
    const area = new AreaPlugin<ClassicScheme, AreaExtra>(containerRef.current);
    const connection = new ConnectionPlugin<ClassicScheme, AreaExtra>();
    const render = new ReactPlugin<ClassicScheme, AreaExtra>({ createRoot });

    render.addPreset(ReactPresets.classic.setup());
    connection.addPreset(ConnectionPresets.classic.setup());

    editor.use(area);
    area.use(connection);
    area.use(render);

    AreaExtensions.zoomAt(area, editor.getNodes());

    editorRef.current = editor;
    areaRef.current = area;
    forceRender((n) => n + 1);

    return () => {
      area.destroy();
      editorRef.current = null;
      areaRef.current = null;
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [containerRef]);

  const exportWslFn = (name: string): WslDocument | null => {
    if (!editorRef.current) return null;
    return exportToWsl(editorRef.current, name);
  };

  return {
    editor: editorRef.current,
    area: areaRef.current,
    exportWsl: exportWslFn,
  };
}
