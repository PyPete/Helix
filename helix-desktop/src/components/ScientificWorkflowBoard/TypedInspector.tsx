import type { CapabilityDescriptor, ParameterSchema, ParameterType } from '../../types/agent';

interface TypedInspectorProps {
  capability: CapabilityDescriptor | null;
  canEdit: boolean;
}

export function TypedInspector({ capability, canEdit }: TypedInspectorProps) {
  return (
    <section aria-label="Typed proposal inspector" data-editable={canEdit}>
      <h2>Typed proposal inspector</h2>
      <p>Binding: unavailable</p>
      <p>Provenance: unavailable</p>
      <p>Quality and review state: unavailable</p>
      <p>Resources and leases: unavailable</p>
      {!capability ? (
        <p>Select an Agent-declared capability to inspect its typed fields.</p>
      ) : (
        <>
          <p>Agent-declared capability: <code>{capability.verbName}</code></p>
          <fieldset disabled={!canEdit}>
            <legend>Proposal fields</legend>
            {capability.parameters.map((parameter) => (
              <TypedProposalField key={parameter.name} parameter={parameter} />
            ))}
            {capability.parameters.length === 0 && <p>No typed parameters are declared by the Agent.</p>}
          </fieldset>
          {!canEdit && <p>Fields are read-only because Agent reconciliation evidence is unavailable.</p>}
        </>
      )}
    </section>
  );
}

function TypedProposalField({ parameter }: { parameter: ParameterSchema }) {
  const label = `Proposal field: ${parameter.displayName}`;
  const unit = parameter.unit || 'unavailable';

  return (
    <div data-field-name={parameter.name} data-field-type={parameter.type}>
      <label>
        {label} ({parameter.type}; unit {unit})
        {renderTypedControl(parameter, label)}
      </label>
      <p>Default: {formatDefault(parameter)}</p>
    </div>
  );
}

function renderTypedControl(parameter: ParameterSchema, label: string) {
  switch (parameter.type satisfies ParameterType) {
    case 'Numeric':
      return <input type="number" aria-label={label} value={typeof parameter.defaultValue === 'number' ? parameter.defaultValue : ''} min={parameter.min} max={parameter.max} step={parameter.step} readOnly />;
    case 'Boolean':
      return <input type="checkbox" aria-label={label} checked={typeof parameter.defaultValue === 'boolean' ? parameter.defaultValue : false} readOnly />;
    case 'Enum':
      return (
        <select aria-label={label} value={enumDefault(parameter)}>
          <option value="">Unavailable</option>
          {(parameter.options ?? []).map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
      );
    case 'String':
    case 'Duration':
      return <input type="text" aria-label={label} value={typeof parameter.defaultValue === 'string' ? parameter.defaultValue : ''} readOnly />;
  }
}

function enumDefault(parameter: ParameterSchema): string {
  return typeof parameter.defaultValue === 'string' && parameter.options?.includes(parameter.defaultValue)
    ? parameter.defaultValue
    : '';
}

function formatDefault(parameter: ParameterSchema): string {
  if (parameter.defaultValue === undefined) return 'unavailable';
  if (parameter.defaultValue === null || ['string', 'number', 'boolean'].includes(typeof parameter.defaultValue)) {
    return String(parameter.defaultValue);
  }
  return 'unavailable (unsupported value)';
}
