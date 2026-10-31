import type { ScientificKind } from "../../types/scientific";

export interface ProcedurePlansProps { kind: ScientificKind; selectedId?: string; }

/** Procedures and plans remain separate Agent-backed scientific records. */
export function ProcedurePlans({ kind }: ProcedurePlansProps) {
  const label = kind === "procedure" ? "Procedures" : "Plans";
  return <section aria-labelledby={`${kind}-title`}><h2 id={`${kind}-title`}>{label}</h2><p>Loading Agent-backed scientific records…</p></section>;
}
