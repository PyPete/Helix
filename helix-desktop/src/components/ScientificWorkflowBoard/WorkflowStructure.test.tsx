import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import type { CommandEvidenceHistoryRecord } from '../../types/agent';
import { WorkflowStructure } from './WorkflowStructure';

afterEach(cleanup);

describe('WorkflowStructure', () => {
  it('renders distinct true and false ports plus explicit fork branches and join', () => {
    render(<WorkflowStructure canEdit={false} resourceLeaseDiagnostics={[]} />);

    expect(screen.getByRole('region', { name: 'True port' })).toHaveTextContent('Proposal path when the condition is true.');
    expect(screen.getByRole('region', { name: 'False port' })).toHaveTextContent('Proposal path when the condition is false.');
    expect(screen.getByText(/Condition evidence: unavailable/)).toBeInTheDocument();
    expect(screen.getByRole('list', { name: 'Fork branches' })).toHaveTextContent('Branch 1');
    expect(screen.getByRole('list', { name: 'Fork branches' })).toHaveTextContent('Branch 2');
    expect(screen.getByText('Join: all proposal branches rejoin here.')).toBeInTheDocument();
    expect(screen.getByText('Agent resource and lease diagnostics: unavailable')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Modify If/Else proposal structure' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Modify Fork/Join proposal structure' })).toBeDisabled();
  });

  it('passes through exact Agent resource and lease check evidence without treating it as admission', () => {
    const evidence: CommandEvidenceHistoryRecord['checks'] = [
      { check: 'resource-budget', status: 'Failed', code: 'resource_limit', reason: 'Exact Agent reason.' },
      { check: 'lease-owner', status: 'Unknown', code: 'lease_unavailable' },
      { check: 'capability', status: 'Passed', code: 'registered' },
    ];
    render(<WorkflowStructure canEdit={false} resourceLeaseDiagnostics={evidence} />);

    expect(screen.getByRole('region', { name: 'Agent resource and lease diagnostics' })).toHaveTextContent(
      'resource-budget: Failed; code resource_limit; Exact Agent reason.'
    );
    expect(screen.getByRole('region', { name: 'Agent resource and lease diagnostics' })).toHaveTextContent(
      'lease-owner: Unknown; code lease_unavailable'
    );
    expect(screen.getByText('Diagnostic evidence is display-only; it does not perform Agent admission checks.')).toBeInTheDocument();
    expect(screen.queryByText(/capability: Passed/)).toBeNull();
  });
});
