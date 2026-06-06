import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { ExecutionPanel } from './ExecutionPanel';
import type { WslDocument, RunspaceEvent } from '../../types/agent';

const sampleDefinition: WslDocument = {
  documentId: 'doc-1',
  name: 'Test Workflow',
  schemaVersion: '1.0',
  nodes: [],
  edges: [],
};

const sampleRunspaceDef = {
  runspaceDefinitionId: 'def-1',
  name: 'Test Runspace',
};

afterEach(() => {
  vi.restoreAllMocks();
});

describe('ExecutionPanel', () => {
  it('renders Start button as enabled when definition is provided', () => {
    render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={[]}
      />
    );
    expect(screen.getByTestId('start-btn')).not.toBeDisabled();
  });

  it('renders Start button as disabled when definition is null', () => {
    render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={null}
        runspaceEvents={[]}
      />
    );
    expect(screen.getByTestId('start-btn')).toBeDisabled();
  });

  it('calls POST /workflows/compile then POST /experiments on Start click and shows experimentId', async () => {
    const mockFetch = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(sampleRunspaceDef),
      } as unknown as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ experimentId: 'exp-abc123', name: 'Test Workflow', lifecycle: 'Running', activePhase: 'main', activeRunspaceId: null }),
      } as unknown as Response);
    vi.stubGlobal('fetch', mockFetch);

    render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={[]}
      />
    );

    fireEvent.click(screen.getByTestId('start-btn'));

    await waitFor(() =>
      expect(screen.getByTestId('experiment-id')).toHaveTextContent('exp-abc123')
    );

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/workflows/compile'),
      expect.anything()
    );
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/experiments'),
      expect.anything()
    );
  });

  it('shows compile errors when POST /workflows/compile returns 422', async () => {
    const mockFetch = vi.fn().mockResolvedValueOnce({
      ok: false,
      status: 422,
      json: () => Promise.resolve([{ message: 'Unknown node type' }]),
    } as unknown as Response);
    vi.stubGlobal('fetch', mockFetch);

    render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={[]}
      />
    );

    fireEvent.click(screen.getByTestId('start-btn'));

    await waitFor(() =>
      expect(screen.getByRole('alert')).toBeInTheDocument()
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Compile errors');
    // POST /experiments must NOT have been called
    expect(mockFetch).not.toHaveBeenCalledWith(
      expect.stringContaining('/experiments'),
      expect.anything()
    );
  });

  it('calls POST /experiments/{id}/abort when Abort is clicked', async () => {
    const experimentId = 'exp-999';
    const events: RunspaceEvent[] = [
      {
        type: 'runspace-started',
        runspaceId: 'rs-999',
        timestamp: '2026-01-01T00:00:00Z',
        experimentId,
      },
    ];

    const mockFetch = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(sampleRunspaceDef),
      } as unknown as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ experimentId, name: 'Test Workflow', lifecycle: 'Running', activePhase: 'main', activeRunspaceId: null }),
      } as unknown as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({}),
      } as unknown as Response);
    vi.stubGlobal('fetch', mockFetch);

    const { rerender } = render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={[]}
      />
    );

    fireEvent.click(screen.getByTestId('start-btn'));
    await waitFor(() =>
      expect(screen.getByTestId('experiment-id')).toHaveTextContent(experimentId)
    );

    // Rerender with the runspace-started event so isRunning = true
    rerender(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={events}
      />
    );

    fireEvent.click(screen.getByTestId('abort-btn'));

    await waitFor(() =>
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/abort'),
        expect.objectContaining({ method: 'POST' })
      )
    );
  });

  it('filters step events by experimentId', () => {
    const experimentId = 'exp-display-test';
    const events: RunspaceEvent[] = [
      {
        type: 'step-started',
        runspaceId: 'rs-1',
        timestamp: '2026-01-01T00:00:00Z',
        experimentId,
      },
      {
        type: 'step-started',
        runspaceId: 'rs-2',
        timestamp: '2026-01-01T00:01:00Z',
        experimentId: 'exp-other',
      },
    ];

    const mockFetch = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(sampleRunspaceDef),
      } as unknown as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ experimentId, name: 'Test Workflow', lifecycle: 'Running', activePhase: 'main', activeRunspaceId: null }),
      } as unknown as Response);
    vi.stubGlobal('fetch', mockFetch);

    const { rerender } = render(
      <ExecutionPanel
        baseUrl='http://localhost:5080'
        definition={sampleDefinition}
        runspaceEvents={[]}
      />
    );

    fireEvent.click(screen.getByTestId('start-btn'));

    return waitFor(() =>
      expect(screen.getByTestId('experiment-id')).toHaveTextContent(experimentId)
    ).then(() => {
      rerender(
        <ExecutionPanel
          baseUrl='http://localhost:5080'
          definition={sampleDefinition}
          runspaceEvents={events}
        />
      );
      // Should show only 1 step-started (from the active experiment)
      const stepItems = screen.getAllByTestId('step-event-step-started');
      expect(stepItems).toHaveLength(1);
    });
  });
});
