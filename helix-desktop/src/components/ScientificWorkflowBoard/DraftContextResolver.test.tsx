import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useDraftContextResolver, type ContextChangeTarget } from '../../hooks/useDraftContextResolver';
import { DraftContextDialog } from './DraftContextDialog';

afterEach(cleanup);

function ResolverHarness({ dirty, save, discard }: {
  dirty: boolean;
  save: () => Promise<unknown | null>;
  discard: () => Promise<boolean>;
}) {
  const resolver = useDraftContextResolver();
  const request = (target: ContextChangeTarget, event: React.MouseEvent<HTMLButtonElement>) => {
    resolver.requestDraftContextChange(target, dirty, event.currentTarget);
  };
  return (
    <>
      <output aria-label="Current context">{resolver.context.kind}:{resolver.context.value}</output>
      <button onClick={(event) => request({ kind: 'revision', value: 'revision-2' }, event)}>Request revision</button>
      <button onClick={(event) => request({ kind: 'route', value: 'route-2' }, event)}>Request route</button>
      <button onClick={(event) => request({ kind: 'procedure', value: 'procedure-2' }, event)}>Request procedure</button>
      {resolver.pendingTarget && (
        <DraftContextDialog
          target={resolver.pendingTarget}
          status={resolver.status}
          error={resolver.error}
          onSave={() => { void resolver.saveAndApply(save); }}
          onDiscard={() => { void resolver.discardAndApply(discard); }}
          onKeepEditing={resolver.keepEditing}
        />
      )}
      {resolver.error && !resolver.pendingTarget && <p role="alert">{resolver.error}</p>}
    </>
  );
}

describe('typed draft context resolver', () => {
  it.each([
    ['revision', 'revision-2', 'Request revision'],
    ['route', 'route-2', 'Request route'],
    ['procedure', 'procedure-2', 'Request procedure'],
  ] as const)('applies a clean %s target directly', (kind, value, requestName) => {
    render(<ResolverHarness dirty={false} save={async () => ({})} discard={async () => true} />);

    fireEvent.click(screen.getByRole('button', { name: requestName }));

    expect(screen.getByLabelText('Current context')).toHaveTextContent(`${kind}:${value}`);
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('applies a dirty proposal target only after Save resolves with acknowledgement', async () => {
    let acknowledge!: (result: object | null) => void;
    const save = vi.fn(() => new Promise<object | null>((resolve) => { acknowledge = resolve; }));
    render(<ResolverHarness dirty save={save} discard={async () => true} />);
    fireEvent.click(screen.getByRole('button', { name: 'Request revision' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save proposal and switch' }));

    expect(screen.getByLabelText('Current context')).toHaveTextContent('revision:current');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(save).toHaveBeenCalledOnce();

    acknowledge({ exact: true });
    await waitFor(() => expect(screen.getByLabelText('Current context')).toHaveTextContent('revision:revision-2'));
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('retains target and original context after save failure and refuses replacement targets', async () => {
    const save = vi.fn(async () => null);
    render(<ResolverHarness dirty save={save} discard={async () => false} />);
    fireEvent.click(screen.getByRole('button', { name: 'Request revision' }));
    fireEvent.click(screen.getByRole('button', { name: 'Request route' }));
    expect(screen.getByRole('dialog')).toHaveTextContent('revision: revision-2');
    expect(screen.getByRole('alert')).toHaveTextContent('already pending');
    fireEvent.click(screen.getByRole('button', { name: 'Save proposal and switch' }));

    await waitFor(() => expect(screen.getByRole('dialog')).toHaveTextContent('The proposal was not acknowledged as saved'));
    expect(screen.getByLabelText('Current context')).toHaveTextContent('revision:current');
    expect(screen.getByRole('dialog')).toHaveTextContent('revision: revision-2');
  });

  it('applies a target only after scoped discard is confirmed', async () => {
    const discard = vi.fn(async () => true);
    render(<ResolverHarness dirty save={async () => null} discard={discard} />);
    fireEvent.click(screen.getByRole('button', { name: 'Request procedure' }));
    fireEvent.click(screen.getByRole('button', { name: 'Discard proposal and switch' }));

    await waitFor(() => expect(screen.getByLabelText('Current context')).toHaveTextContent('procedure:procedure-2'));
    expect(discard).toHaveBeenCalledOnce();
  });

  it('keeps the original context and restores focus when the user keeps editing', async () => {
    render(<ResolverHarness dirty save={async () => null} discard={async () => false} />);
    const request = screen.getByRole('button', { name: 'Request route' });
    fireEvent.click(request);
    fireEvent.click(screen.getByRole('button', { name: 'Keep editing' }));

    await waitFor(() => expect(document.activeElement).toBe(request));
    expect(screen.getByLabelText('Current context')).toHaveTextContent('revision:current');
    expect(screen.queryByRole('dialog')).toBeNull();
  });
});
