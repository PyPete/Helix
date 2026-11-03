import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import type { CapabilityDescriptor } from '../../types/agent';
import { TypedInspector } from './TypedInspector';

afterEach(cleanup);

const descriptor: CapabilityDescriptor = {
  verbName: 'SetFlow',
  displayName: 'Set flow',
  description: 'Set a logical flow target.',
  category: 'Control',
  parameters: [
    { name: 'flow', displayName: 'Flow', type: 'Numeric', unit: 'mL/min', min: 0, max: 20, step: 0.5, defaultValue: 2 },
    { name: 'mode', displayName: 'Mode', type: 'Enum', unit: '', options: ['Manual', 'Auto'] },
  ],
  isDestructive: false,
  requiresActiveRunspace: false,
};

describe('TypedInspector', () => {
  it('renders exact typed defaults and units while denying edits and unsupported metadata', () => {
    render(<TypedInspector capability={descriptor} canEdit={false} />);

    const flow = screen.getByRole('spinbutton', { name: 'Proposal field: Flow' });
    const mode = screen.getByRole('combobox', { name: 'Proposal field: Mode' });
    expect(flow).toBeDisabled();
    expect(flow).toHaveValue(2);
    expect(mode).toBeDisabled();
    expect(screen.getByText('Default: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Binding: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Provenance: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Quality and review state: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Resources and leases: unavailable')).toBeInTheDocument();
    expect(screen.getByText(/read-only because Agent reconciliation evidence is unavailable/)).toBeInTheDocument();
  });

  it('shows an explicit unavailable state without a selected capability', () => {
    render(<TypedInspector capability={null} canEdit={false} />);
    expect(screen.getByText('Select an Agent-declared capability to inspect its typed fields.')).toBeInTheDocument();
  });
});
