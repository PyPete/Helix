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

  it('calls POST /workflows/compile then POST /runspaces on Start click and shows runspaceId', async () => {
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
        json: () => Promise.resolve({ runspaceId: 'abc123' }),
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
      expect(screen.getByTestId('runspace-id')).toHaveTextContent('abc123')
    );

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/workflows/compile'),
      expect.anything()
    );
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/runspaces'),
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
    // POST /runspaces must NOT have been called
    expect(mockFetch).not.toHaveBeenCalledWith(
      expect.stringContaining('/runspaces'),
      expect.anything()
    );
  });

  it('calls POST /runspaces/{id}/abort on Abort click', async () => {
    const runspaceId = 'rs-999';
    const events: RunspaceEvent[] = [
      {
        type: 'runspace-started',
        runspaceId,
        timestamp: '2026-01-01T00:00:00Z',
      },
    ];

    // First render: mock compile + start so we get a runspaceId
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
        json: () => Promise.resolve({ runspaceId }),
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
      expect(screen.getByTestId('runspace-id')).toHaveTextContent(runspaceId)
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

  it('displays RunspaceEvents in step list', () => {
    const runspaceId = 'rs-display-test';
    const events: RunspaceEvent[] = [
      {
        type: 'step-started',
        runspaceId,
        timestamp: '2026-01-01T00:00:00Z',
      },
    ];

    // Render without a runspaceId set -- events filter to nothing until we simulate the start
    // Instead, test the event display directly by giving a started runspace id via initial state
    // We achieve this by mocking a successful start flow then rerendering with events

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
        json: () => Promise.resolve({ runspaceId }),
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
      expect(screen.getByTestId('runspace-id')).toHaveTextContent(runspaceId)
    ).then(() => {
      rerender(
        <ExecutionPanel
          baseUrl='http://localhost:5080'
          definition={sampleDefinition}
          runspaceEvents={events}
        />
      );
      expect(screen.getByTestId('step-event-step-started')).toBeInTheDocument();
    });
  });
});