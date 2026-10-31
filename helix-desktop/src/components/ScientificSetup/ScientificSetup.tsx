import { useRef, useState } from "react";
import { ApparatusDraft, ApparatusDraftState, readApparatusDraft, saveApparatusDraft } from "../../lib/scientificApparatusClient";

export interface ScientificSetupProps {
  apparatusId: string;
  initialDraft: ApparatusDraftState;
}

/** Draft editor conflict controls retain local input until the Agent acknowledges a new result. */
export function ScientificSetup({ apparatusId, initialDraft }: ScientificSetupProps) {
  const [draft, setDraft] = useState<ApparatusDraftState>(initialDraft);
  const [localName, setLocalName] = useState(initialDraft.apparatus.name);
  const [current, setCurrent] = useState<ApparatusDraftState>();
  const [conflict, setConflict] = useState<string>();
  const operationId = useRef<string>();

  async function save() {
    operationId.current ??= crypto.randomUUID();
    setConflict(undefined);
    try {
      const result = await saveApparatusDraft(apparatusId, {
        ...draft.apparatus,
        name: localName,
        operationId: operationId.current,
        expectedToken: draft.token,
      });
      setDraft({ apparatus: { ...draft.apparatus, name: localName }, token: result.token });
      operationId.current = undefined;
    } catch (reason) {
      setConflict(reason instanceof Error ? reason.message : "Draft changed elsewhere.");
    }
  }

  async function reload() {
    const latest = await readApparatusDraft(apparatusId);
    setDraft(latest);
    setLocalName(latest.apparatus.name);
    setCurrent(undefined);
    setConflict(undefined);
  }

  async function compare() {
    setCurrent(await readApparatusDraft(apparatusId));
  }

  return <section aria-labelledby="scientific-setup-title">
    <h2 id="scientific-setup-title">Scientific setup draft</h2>
    <label htmlFor="scientific-setup-name">Name</label>
    <input id="scientific-setup-name" value={localName} onChange={(event) => setLocalName(event.target.value)} />
    <button type="button" onClick={save}>Save draft</button>
    {conflict && <div role="alert">Draft changed elsewhere. <button type="button" onClick={reload}>Reload</button> <button type="button" onClick={compare}>Compare</button> <span>Operation {operationId.current}</span></div>}
    {current && <dl aria-label="Draft comparison"><dt>Local name</dt><dd>{localName}</dd><dt>Current name</dt><dd>{current.apparatus.name}</dd><dt>Current token</dt><dd>{current.token}</dd></dl>}
  </section>;
}
