# Local Proposal Draft Contract

**Status: APPROVED FOR PLANNING — NOT IMPLEMENTED**  
**Approval:** The user explicitly approved the local-only proposal contract and defaults in the planning request on 2026-11-03. This approval authorizes planning and implementation of this contract only; it does not authorize execution, validation, publication, approval, or promotion.

## Purpose and scope

Define a narrow local-only contract for composing and persisting an untrusted workflow proposal in the desktop board, and for resolving that proposal before an explicitly typed revision, route, or procedure context change. These are new board seams to implement, not claims about existing support. The board remains read-only for proposal mutation until a user invokes the explicit local Create Proposal or Edit Proposal action.

## Approved defaults

1. **Explicit local editing intent:** Create Proposal and Edit Proposal open local proposal editing. This is renderer-local intent, independent of Agent health or session presence. It grants no authority beyond editing an untrusted local proposal. Health/session status continues to inform Agent evidence and other safety policy; it does not grant or revoke local proposal-edit permission.
2. **Distinct persistence:** Store proposals in a new namespace under Tauri `BaseDirectory.AppData`, scoped by the current OS user's application-data profile. Do not reuse `workflowLibrary`/legacy workflow files, dock-layout storage, `sessionStorage`, or Agent scientific APIs/receipts. A successfully acknowledged Save persists across application restarts.
3. **Exact Save acknowledgement:** Save succeeds only after the exact proposal ID, schema version, content revision, and content have been written and read back. Compare canonical JSON: object keys are recursively sorted for comparison; array order is preserved. The acknowledgement is local persistence evidence only.
4. **Scoped Discard acknowledgement:** Discard succeeds only when deletion of this proposal ID is confirmed and a read/check confirms that its scoped file is absent. It cannot delete another proposal.
5. **Typed context resolution:** Only explicit, typed revision/route/procedure context requests invoke the resolver. Ordinary dock, ribbon, tab, and presentation navigation bypasses it. The picker/caller is part of the new board seam; no existing caller is assumed.
6. **Failure and authority boundaries:** Save, read-back, parse, acknowledgement, or deletion failure preserves the current context and all dirty in-memory data. Save/Discard never apply the pending target before acknowledgement. There are no Agent, driver, or device calls; no scientific record or receipt; and no validation, approval, publication, dispatch, or execution authority.

## Proposal data and dirty state

A proposal document contains only a stable locally generated `proposalId`, positive `schemaVersion`, monotonically increasing `contentRevision`, board-owned proposal content, optional exact references only when already available from a source, and a local `updatedAt` display/recovery value. It must not contain or imply a scientific identity, lifecycle, receipt, approval, validation result, or execution state. Local proposal identity and references are not scientific facts.

Distinct content changes advance the content revision. Dirty state compares current board-owned content/revision with the last exact locally acknowledged baseline. No silent autosave occurs. A context request carries a discriminated revision/route/procedure target from the new board picker; a target is navigation intent only and must not be presented as a verified scientific record. Optional proposal source references are included only when an exact source-backed value is already available.

## Persistence, recovery, and conservative limits

Use a dedicated namespace such as `scientific-workflow-proposals/{proposalId}.json` under Tauri AppData. The OS profile scopes the directory; this does not add encryption or cross-user sharing. Do not store credentials or secrets in proposal content.

Save writes then reads/parses the same document and acknowledges success only if ID, schema version, content revision, and canonicalized content all match the request. A retry is limited to the same proposal ID/revision/content; it cannot overwrite a newer revision. Discard targets only the current proposal file and succeeds only after scoped absence is confirmed. A second context request while one is pending cannot silently replace its target; reject it accessibly while retaining the original request.

Malformed or unsupported-schema files fail closed, remain byte-for-byte preserved, are surfaced with an accessible manual-recovery error, and are never silently migrated or overwritten. No automatic migration is included. Unsaved in-memory edits are preserved on operation failure but are **not guaranteed to survive a process crash**. Successfully acknowledged saves survive application restart. No export/recovery workflow beyond preserving and surfacing the file is authorized here.

## Save / Discard / Keep editing

| Choice | Required transition |
|---|---|
| **Save** | Wait for exact ID/schema/content-revision/content write/read-back acknowledgement. Only then mark that revision clean and apply the queued target. |
| **Discard** | Wait for scoped deletion and absence acknowledgement. Only then discard this proposal's in-memory content and apply the queued target. |
| **Keep editing** | Cancel the pending target, close the decision UI, and retain the original context, all current content, and dirty baseline. |

Selecting an action or starting I/O is not success. Any failure leaves the target unapplied, retains dirty memory, and exposes a user-visible accessible error with retry of the same operation or Keep editing.

## Keyboard and screen-reader parity

Pointer, keyboard, and assistive-technology entry points use the same explicit local editability hook and typed request/resolver boundary. The Save/Discard/Keep editing dialog has dialog semantics, accessible action names, announced pending/success/failure status, and predictable focus entry and restoration. A Save success announcement follows only exact local acknowledgement. These controls must not resemble Agent approval or execution controls. Automated DOM accessibility tests do not substitute for the installed-Tauri screen-reader UAT retained by Plan 03-05.

## Prohibited scope

- Reusing workflow-library, layout, session-storage, or Agent scientific receipt persistence.
- Calling Agent, driver, or device APIs from the proposal subsystem.
- Creating a scientific record/receipt or granting validation, approval, publication, dispatch, or execution authority.
- Letting health/session state grant or deny local proposal editing.
- Routing ordinary dock/ribbon/tab navigation through the dirty-proposal resolver.
- Silent migration, overwrite of malformed/unsupported files, automatic save, generated scientific references, collaboration, synchronization, or cross-user sharing.
- Any expansion beyond the local-only contract above; stop for explicit user approval before adding it.

## Planning acceptance criteria

1. Tests prove the explicit local action enables proposal-only editing and no action leaves it disabled, for healthy, unhealthy, pending, failed, and absent Agent health/session observations.
2. Save success requires exact ID/schema/content-revision/content write/read-back equality; each write, read, parse, or mismatch failure preserves context and dirty memory.
3. Discard success requires scoped deletion and absence acknowledgement; failed/uncertain deletion preserves context and data and never deletes another proposal.
4. Keep editing cancels the pending typed target and preserves content and dirty baseline; repeated requests cannot replace a pending target.
5. Only explicitly typed revision/route/procedure requests use the resolver; ordinary navigation does not.
6. Malformed and unsupported-schema data is preserved and surfaced without migration or overwrite; tests verify acknowledged restart persistence and document that unsaved memory is not crash-durable.
7. Keyboard and pointer paths share the hook/resolver; tests verify names, status, and focus behavior.
8. Proposal-operation tests prove no Agent/driver/device calls and no scientific record, receipt, validation, approval, publication, dispatch, or execution authority.
