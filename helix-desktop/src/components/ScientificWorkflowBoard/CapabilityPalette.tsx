import { useEffect, useState } from 'react';
import { getAgentDeviceCapabilities, getAgentDevices } from '../../lib/agentClient';
import type { AgentDeviceDiscovery } from '../../lib/agentClient';
import type { CapabilityDescriptor, ParameterSchema } from '../../types/agent';

export interface AgentCapabilityDeclaration {
  adapterId: string;
  descriptor: CapabilityDescriptor;
}

type PaletteState =
  | { status: 'loading'; declarations: AgentCapabilityDeclaration[]; message: string }
  | { status: 'unavailable'; declarations: AgentCapabilityDeclaration[]; message: string }
  | { status: 'available'; declarations: AgentCapabilityDeclaration[]; message: string };

interface CapabilityPaletteProps {
  onInspect: (descriptor: CapabilityDescriptor) => void;
}

const unavailableState: PaletteState = {
  status: 'unavailable',
  declarations: [],
  message: 'Capability declarations are unavailable from the Agent.',
};

export function CapabilityPalette({ onInspect }: CapabilityPaletteProps) {
  const [state, setState] = useState<PaletteState>({
    status: 'loading',
    declarations: [],
    message: 'Loading Agent-declared logical capabilities.',
  });

  useEffect(() => {
    let active = true;

    async function loadDeclarations() {
      try {
        const devices = await getAgentDevices();
        if (!Array.isArray(devices)) throw new Error('Malformed Agent device discovery.');

        const discovered = await Promise.all(devices.map(async (device) => {
          if (!isDeviceDiscovery(device)) throw new Error('Malformed Agent device discovery.');
          const result = await getAgentDeviceCapabilities(device.adapterId);
          if (!result || result.adapterId !== device.adapterId || !Array.isArray(result.capabilities)) {
            throw new Error('Malformed Agent capability discovery.');
          }

          return result.capabilities
            .filter(isCapabilityDescriptor)
            .filter((descriptor) => device.capabilities.includes(descriptor.verbName))
            .map((descriptor) => ({ adapterId: device.adapterId, descriptor }));
        }));

        const declarations = discovered.flat();
        if (active) {
          setState({
            status: declarations.length === 0 ? 'unavailable' : 'available',
            declarations,
            message: declarations.length === 0
              ? 'Capability declarations are unavailable from the Agent.'
              : 'Agent-declared logical capabilities.',
          });
        }
      } catch {
        if (active) setState(unavailableState);
      }
    }

    void loadDeclarations();
    return () => { active = false; };
  }, []);

  return (
    <section aria-label="Agent capability palette" aria-busy={state.status === 'loading'} data-status={state.status}>
      <h2>Agent capability palette</h2>
      <p>{state.message}</p>
      {state.declarations.length > 0 && (
        <ul>
          {state.declarations.map(({ adapterId, descriptor }) => (
            <li key={`${adapterId}:${descriptor.verbName}`}>
              <h3>{descriptor.displayName}</h3>
              <p>Logical capability: <code>{descriptor.verbName}</code></p>
              <p>Agent adapter: <code>{adapterId}</code></p>
              <p>{descriptor.description || 'Description: unavailable'}</p>
              <p>Category: {descriptor.category}</p>
              <p>Binding: unavailable</p>
              <p>Provenance: unavailable</p>
              <p>Quality and review state: unavailable</p>
              <p>Resources and leases: unavailable</p>
              {descriptor.parameters.map((parameter) => (
                <p key={parameter.name}>
                  {parameter.displayName}: {parameter.type}; unit {parameter.unit || 'unavailable'}; default {formatDefault(parameter)}
                </p>
              ))}
              <button type="button" onClick={() => onInspect(descriptor)} aria-label={`Inspect ${descriptor.verbName}`}>
                Inspect capability fields
              </button>
            </li>
          ))}
        </ul>
      )}
      <p>Adding a capability to a proposal is read-only while Agent reconciliation evidence is unavailable.</p>
    </section>
  );
}

function isDeviceDiscovery(value: unknown): value is AgentDeviceDiscovery {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as AgentDeviceDiscovery;
  return typeof candidate.adapterId === 'string' && candidate.adapterId.length > 0 &&
    Array.isArray(candidate.capabilities) && candidate.capabilities.every((name) => typeof name === 'string');
}

function isCapabilityDescriptor(value: unknown): value is CapabilityDescriptor {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as CapabilityDescriptor;
  return typeof candidate.verbName === 'string' && candidate.verbName.length > 0 &&
    typeof candidate.displayName === 'string' &&
    typeof candidate.description === 'string' &&
    ['Control', 'Measurement', 'Status', 'Utility'].includes(candidate.category) &&
    typeof candidate.isDestructive === 'boolean' &&
    typeof candidate.requiresActiveRunspace === 'boolean' &&
    Array.isArray(candidate.parameters) && candidate.parameters.every(isParameterSchema);
}

function isParameterSchema(value: unknown): value is ParameterSchema {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as ParameterSchema;
  return typeof candidate.name === 'string' && typeof candidate.displayName === 'string' &&
    ['Numeric', 'Boolean', 'Enum', 'String', 'Duration'].includes(candidate.type) &&
    typeof candidate.unit === 'string' &&
    (candidate.options === undefined || (Array.isArray(candidate.options) && candidate.options.every((option) => typeof option === 'string')));
}

function formatDefault(parameter: ParameterSchema): string {
  if (parameter.defaultValue === undefined) return 'unavailable';
  if (parameter.defaultValue === null || ['string', 'number', 'boolean'].includes(typeof parameter.defaultValue)) {
    return String(parameter.defaultValue);
  }
  return 'unavailable (unsupported value)';
}
