import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { getAgentDeviceCapabilities, getAgentDevices } from '../../lib/agentClient';
import type { CapabilityDescriptor } from '../../types/agent';
import { CapabilityPalette } from './CapabilityPalette';

vi.mock('../../lib/agentClient', () => ({
  getAgentDevices: vi.fn(),
  getAgentDeviceCapabilities: vi.fn(),
}));

afterEach(cleanup);

const setFlow: CapabilityDescriptor = {
  verbName: 'SetFlow',
  displayName: 'Set flow',
  description: 'Set a logical flow target.',
  category: 'Control',
  parameters: [{ name: 'flow', displayName: 'Flow', type: 'Numeric', unit: 'mL/min', defaultValue: 2 }],
  isDestructive: false,
  requiresActiveRunspace: false,
};

describe('CapabilityPalette', () => {
  it('shows only exact descriptors also declared by Agent device discovery', async () => {
    vi.mocked(getAgentDevices).mockResolvedValue([{ adapterId: 'simulator', capabilities: ['SetFlow'] }]);
    vi.mocked(getAgentDeviceCapabilities).mockResolvedValue({
      adapterId: 'simulator',
      capabilities: [setFlow, { ...setFlow, verbName: 'UnlistedCapability' }],
    });
    const onInspect = vi.fn();

    render(<CapabilityPalette onInspect={onInspect} />);

    expect(await screen.findByText(/Logical capability:/)).toBeInTheDocument();
    expect(screen.getByText('SetFlow')).toBeInTheDocument();
    expect(screen.queryByText('UnlistedCapability')).toBeNull();
    expect(screen.getByText('Flow: Numeric; unit mL/min; default 2')).toBeInTheDocument();
    expect(screen.getByText('Binding: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Provenance: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Quality and review state: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Resources and leases: unavailable')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Inspect SetFlow' }));
    expect(onInspect).toHaveBeenCalledWith(setFlow);
  });

  it('keeps capability declarations unavailable when the native read fails', async () => {
    vi.mocked(getAgentDevices).mockRejectedValue(new Error('native read unavailable'));
    render(<CapabilityPalette onInspect={vi.fn()} />);

    expect(await screen.findByText('Capability declarations are unavailable from the Agent.')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('region', { name: 'Agent capability palette' })).toHaveAttribute('data-status', 'unavailable'));
  });
});
