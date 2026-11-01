import { useCallback, useEffect, useRef, useState } from "react";
import type { ScientificKind, ScientificReceipt } from "../types/scientific";
import { readScientificOperation } from "../lib/scientificClient";

export interface ScientificOperationContext {
  kind: ScientificKind;
  operationId: string;
  request: string;
}

export interface ScientificOperationResult {
  state: "idle" | "pending" | "uncertain" | "committed";
  context?: ScientificOperationContext;
  receipt?: ScientificReceipt;
  begin(context: ScientificOperationContext): void;
  markUncertain(): void;
  reconcile(): Promise<ScientificReceipt | undefined>;
}

/** Retains caller-owned mutation identity; local recovery is never scientific evidence. */
export function useScientificOperation(userId: string): ScientificOperationResult {
  const storageKey = `helix.scientific-operation.${userId}`;
  const [context, setContext] = useState<ScientificOperationContext | undefined>(() => {
    try {
      const value = sessionStorage.getItem(storageKey);
      return value ? JSON.parse(value) as ScientificOperationContext : undefined;
    } catch {
      return undefined;
    }
  });
  const [state, setState] = useState<ScientificOperationResult["state"]>(() => context ? "uncertain" : "idle");
  const contextRef = useRef(context);

  useEffect(() => {
    contextRef.current = context;
    if (context) sessionStorage.setItem(storageKey, JSON.stringify(context));
    else sessionStorage.removeItem(storageKey);
  }, [context, storageKey]);

  const begin = useCallback((next: ScientificOperationContext) => {
    setContext(next);
    setState("pending");
  }, []);

  const markUncertain = useCallback(() => setState("uncertain"), []);
  const reconcile = useCallback(async () => {
    const pending = contextRef.current;
    if (!pending) return;
    const receipt = await readScientificOperation(pending.kind, pending.operationId);
    setContext(undefined);
    setState("committed");
    return receipt;
  }, []);

  return { state, context, begin, markUncertain, reconcile };
}
