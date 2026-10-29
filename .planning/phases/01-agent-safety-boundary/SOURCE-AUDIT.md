# Phase 01 — Multi-source coverage audit

Status: planning coverage, not implementation verification. All four mandatory source types are audited. No in-scope source item is omitted. Source assumptions and descriptor-less enforcement remain honestly unverified.

## GOAL coverage

| ID | Source outcome | Plans/tasks | Status |
|---|---|---|---|
| GOAL-01 | Local Agent-only simulated execution with safety checks | 01 tracer, 02 checks, 10 checked compilation, 04/11 entry-point closure, 05/12 native callers, 08 effective deployment | COVERED |
| GOAL-02 | Pause/hold/abort controls | 03-01, 06-01/02, 08-02 | COVERED |
| GOAL-03 | Restart reconciliation without uncertain action replay | 01 tracer, 03-02, 09-02, 08-02 | COVERED |
| GOAL-04 | Attributable durable command timeline | 01 tracer, 07-01/02 mounted history, 13-01 complete presentation, 09-01 canonical storage, 14-01/02 outage audit/recovery, 08-02 | COVERED |

Approved exact user story is recorded in ROADMAP.md and SKELETON.md; no invented story replaces it.

## REQ coverage

| ID | Required observable behavior | Plans | Status |
|---|---|---|---|
| SAFE-01 | Dispatch only through local Agent | 01,02,04,05,06,07,08,10,11,12,14 | COVERED |
| SAFE-02 | Capability/safety/lease/resource/budget/termination all pass before command dispatch | 01,02,03,04,06,07,08,09,10,11,13 | COVERED |
| SAFE-03 | Scientist pause/abort/hold, Agent restart reconciliation | 01,03,04,06,08,09,11 | COVERED |
| SAFE-04 | Intent/observation/correlation/audit context | 01,03,04,05,07,08,09,11,12,13,14 | COVERED |

## CONTEXT decision coverage

| ID | Locked decision | Implementing tasks | Status |
|---|---|---|---|
| D-01 | Desktop-only loopback; no lab-network/Site Server requirement | 01-01-01/02, 11-02, 05-01/02, 12-02, 06-02, 08-01/02 | COVERED |
| D-02 | Validated scientist desktop session on every request, audited actor | 01-01-01/02, 04-01, 05-01, 06-02, 07-01 | COVERED |
| D-03 | Every API/read authenticates; denied attempts audited | 01-01-01/02, 11-02, 05-01/02, 12-01/02, 07-01, 14-01/02, 08-02 | COVERED |
| D-04 | Ordinary invalid proposal rejects with structured details | 02-01/02, 04-01/02, 06-01, 09-02 | COVERED |
| D-05 | Unsafe/unknown safety/lease/health holds for review | 02-01/02, 03-01, 06-01, 13-02 | COVERED |
| D-06 | Proposal/compile feedback plus fresh pre-dispatch checks | 01-01-01, 02-01/02, 10-01/02, 04-01/02, 11-01/02, 06-01, 09-02 | COVERED |
| D-07 | Intent unknown after restart is held, never blindly replayed | 01-01-01, 03-02, 04-02, 06-01/02, 09-02, 08-02 | COVERED |
| D-08 | Query simulator, matching expected state auto-resumes with recovery audit | 03-02, 06-01, 08-02 | COVERED |
| D-09 | Unreadable/mismatch remains held, no auto-retry | 03-02, 04-02, 06-01, 08-02 | COVERED |
| D-10 | Chronological proposal/check/dispatch/observation/recovery/hold/abort timeline | 01-01-01, 11-02, 07-01/02, 13-01, 14-02 | COVERED |
| D-11 | Outcome-first action/device-capability/time/actor/outcome; expandable details | 01-01-01, 07-01/02, 13-01 | COVERED |
| D-12 | Immutable original facts, attributable append-only annotations/decisions | 01-01-01, 03-01, 11-02, 07-01/02, 09-01, 13-01, 14-02 | COVERED |
| D-13 | Prominent rejected/held/aborted/safety/reconciliation, quiet ordinary success | 06-01, 13-01/02 | COVERED |

Discretion: Windows OS-user-restricted pipe session bootstrap and maintained OS WinHTTP transport; fixed destination/native memory, explicit same-account adversary limitation. Numerical simulation defaults are explicit and require scientist policy approval. No new one-way decision; costly decisions are labeled. The approved five-file exception applies only to first tracer. Deferred ideas: none; other-phase apparatus/results/campaign optimization and real hardware excluded by phase boundary.

## RESEARCH feature/constraint coverage

| Item | Research source | Plans/tasks | Status |
|---|---|---|---|
| R-01 | Architectural Responsibility Map; inward Core ports/Agent authority | 01 contracts and Infrastructure-to-Core ProjectReference, 11 executors; SKELETON responsibilities | COVERED |
| R-02 | Native broker, validated expiring/revocable scientist session, fixed destination, all routes/upgrade | 01-01-01/02, 05-01, 08-01/02 | COVERED |
| R-03 | Bootstrap/scientist identity question | Same-user Windows SID verified at pipe; disclosed same-user attacker limitation; native-only credentials and runbook 01/05/08 | COVERED |
| R-04 | Native generator feasibility; reuse dependencies/no guessed prost/install | Maintained Windows WinHTTP instead of unproven generator; 01 actual native smoke, 05 native tests, 08 native build/ACL acceptance | COVERED — preferred generator path not mandatory |
| R-05 | Seven-check matrix, finite bounded canonical params/units, freshness/approved explicit limits | 01-01-01 fast CommandAdmissionTests, 02-01/02, 10-02, 06-01 policy approval, 09-02 races | COVERED |
| R-06 | Ledger intent/actor/correlation/policy/revision/check/control/reservation/outcome fields | 01-01-01, 09-01, 07-01 details | COVERED |
| R-07 | Durable serialized controls/reservations/lease/budget, no lock across effect | 01-01-01, 03-01, 09-01/02 | COVERED |
| R-08 | Stable command/content/evidence identity, idempotency and per-stream commit order | 01-01-01, 07-01, 09-01/02 | COVERED |
| R-09 | Durable safe-boundary pause/review hold/terminal abort/cursor continuation | 03-01, 04-01/02, 06-01/02 | COVERED |
| R-10 | Independent persisted simulator setter/effect generation/postcondition observation | 01 tracer, 03-02 | COVERED |
| R-11 | Four crash windows/matching/mismatch/unreadable/independent-hold recovery | 03-02, 08-02 | COVERED |
| R-12 | Actual Program factory registry bypass | 01-01-01, 11-01/02 | COVERED |
| R-13 | Manual/raw REST/gRPC/alternate WSL executor bypass | 04-01/02 and 11-01/02 effect-count matrix | COVERED |
| R-14 | Variable read mutation/defaulting and unsupported graph constructs | 10-01/02, 11-01, 08-02 inventory | COVERED |
| R-15 | Simulator-only registration, no real-device reachability | 01-01-01, 03-02 canonical SetFlow/GetFlow, SetTemperature/GetTemperature, SetMode/GetMode registration replacing legacy SetPumpMode, 11-02, 12-02, 08-01/02 | COVERED |
| R-16 | Desktop compiled content not actually submitted and shell lifecycle mismatch | 04-01, 06-01/02, 08-02 | COVERED |
| R-17 | Best-effort evidence cannot be canonical; commit before notification | 01-01-01, 11-02, 07-01, 09-01, 14-02 commit-before-spool-ack | COVERED |
| R-18 | Durable timeline/completed/manual/security retrieval, native cursor reconnect/dedup | 12-01, 07-01/02 mounted consumer, 13-01, 14-02 recovered outage provenance | COVERED |
| R-19 | Legacy records/schema/unknown provenance and existing-volume migration | 01 schema, additive 09 migration, 07 labels, blocking 08 real-schema gate | COVERED |
| R-20 | Effective listeners, compose wildcard/probe, installed account/binaries/config secrets inventory | 11-02 startup validation, 08-01/02 | COVERED |
| R-21 | Database outage stops dispatch, auth floods bounded, secrets never logged | 01-01-01/02, 05-01, 09-01/02, 14-01 reserved bounded spool and fail-closed quiescence before exhaustion, 14-02 identity/time/provenance-preserving recovery, 08 runbook | COVERED |
| R-22 | Host startup initialization seam, real workers retained, fail-loud connected SQL tests | 01-01-01/02, 03-02, 09-01, 08-02 | COVERED |
| R-23 | Native packaged/build smoke, supported toolchain, warnings-as-errors/format/frontend builds | 01 native process, 05 native tests, 08 full acceptance/build/ACL | COVERED |
| R-24 | Security ASVS1 authentication/session/authorization/API/validation/config/logging/frontend/crypto | Every plan threat model; 01–36 distinct threats; high/critical blocks | COVERED |
| R-25 | No package audit for installs, no package recommendations | No install tasks; any discovered addition halts for official discovery/audit/human gate | COVERED |

Research assumptions A1/A4–A7 have explicit implementing contracts/tests; A2/A3/A8–A11 retain their stated compatibility/runtime/security confidence limits until execution proves them. Research out-of-scope work is excluded, not a missing item.

The six named canonical RESEARCH open questions now have explicit RESOLVED planning dispositions: Scientist identity/bootstrap (01/05/08), Native protocol build (01/05/08), Policy defaults and observability (02/03/10/06/09), Legacy runtime/data (09/11/08), Verification infrastructure (01/03/09/08), and ASVS individual controls (all threat models/08). These are decisions and executable preconditions, not live runtime, compliance or installed-schema evidence; each retains its unverified acceptance gate.

## Classified edge coverage and retained unresolved assumptions

| Requirement/category | Resolution | Must-have/verification | Status |
|---|---|---|---|
| SAFE-04 adjacency | Intent/checks commit before effect; outcome before notification; note is later fact | Plans 01/11/07/09/14 facts and barrier tests | COVERED explicitly |
| SAFE-04 empty | No command evidence yet; missing fields Not recorded/Unavailable, no fabricated provenance | Plans 07/13 truths and timeline tests; 14 recovered history | COVERED explicitly |
| SAFE-04 ordering | Serialized per-execution commit sequence/cursor and stable-ID merge | Plans 07/09/12/13/14 truths and concurrent/pagination/recovery tests | COVERED explicitly |
| SAFE-01 unclassified | Original edge assumption has no classified category | Preserve unresolved source assumption; not a backstop | UNRESOLVED — flagged-unverified |
| SAFE-02 unclassified | Original edge assumption has no classified category | Preserve unresolved source assumption; not a backstop | UNRESOLVED — flagged-unverified |
| SAFE-03 unclassified | Original edge assumption has no classified category | Preserve unresolved source assumption; not a backstop | UNRESOLVED — flagged-unverified |

The three classified SAFE-04 source edges are now explicitly planned; the three unclassified source rows are not silently “resolved” by generic safety tests. Planning coverage does not certify their enforcement.

## All UI Considerations lifted into must-haves

38 explicit applicable considerations, zero backstop. Categories below preserve all source element assignments rather than collapsing unresolved states into success.

| Category | Elements from UI-SPEC | Must-haves and tasks |
|---|---|---|
| empty | E2/E3/E5/E6 | 06 missing-check truth/tests; 07-02 history; 13-01/02 empty details/rules |
| loading | E1–E6 | 05/12 hook/discovery truths; 06 session/control/admission loading truths; 07/13 history/details/safety truth/tests |
| error | E1–E6 | 05/12 authenticated errors; 06 exact session/reject/held copy; 07/13 read-safe errors/partial availability |
| populated | E2/E3/E5/E6 | 06 checks Passed/Failed/Unknown; 13 summary/details/status labels |
| partial | E2/E3/E5/E6 | 06 no green absent-check truth; 07/13 missing fields/retained rows/unknown safety truths |
| overflow | E1/E2/E3/E5/E6 | 06 wrap visible critical session/checks; 13 bounded scroll/details/rule tests and visual supplement |
| zero-one-many | E2/E3/E5/E6 | 06 checklist cases; 13 empty/single/many evidence/rule list semantics |
| long-text | E1/E2/E3/E4/E5 | 06 diagnostic/session/control wrapping; 13 details/IDs/parameters selectable monospace and visible explanation |

UI-SPEC fixed copy, typography/color/spacing/no-package contract and controls/timeline semantics are cited in Plans 06/07/13. Exact Pause/Hold labels are retained with explanatory accessible text; nonblocking copy suggestion does not replace the approved labels.

## Prohibition recall contract

No SPEC Prohibitions section was supplied. Invoked the actual exported projectProhibitions serializer in C:/Users/u643664/.config/opencode/gsd-core/bin/lib/probe-core.cjs with six recalled resolved/test items and no check descriptors; copied its exact statement/status/verification projection into Plan 01 must_haves.prohibitions. The query router has no projectProhibitions command; that failed lookup was superseded by the real module export, not treated as absent prohibitions.

All six descriptor-less items remain flagged-unverified under enforcement; no fabricated node-test/lint descriptor and no auto-dismissal. Planned behavioral mitigation does not manufacture producer enforcement evidence. The orchestrator must retain this status through checker/verification handoff.

## Dependency/ownership audit

14 plans, 28 tasks, seven waves. Wave 1: 01. Wave 2: 02/03/05. Wave 3: 09/10/12. Wave 4: 04/14. Wave 5: 06/07/11. Wave 6: 13. Wave 7: 08. Same-wave modification sets must be disjoint. 06 explicitly depends on actual canonical submission producer 04; 10 consumes 02/03, 11 consumes 04/10/03, 12 consumes 05, 14 consumes 09/01, 07 consumes 10/03/05/12/14, and 13 consumes mounted history 07 and controls 06. 08 waits on every completed branch through 11/06/13/09/12/14. Additive 004 prevents changing the already-applied tracer migration 003. Every expansion task has at most five paths and every expansion plan at most nine; first tracer alone has the approved exception.

## Final planning verdict

Historical initial-planning contributions ran in declared array order: (1) API detector returned detected=true for own REST surface; COVERAGE.md records the reasoned no-external-service declaration. (2) assumption-delta returned detected=false. (3) ORM-pattern returned schemaPushRequired=false; real Npgsql migration remains mandatory. (4) ASVS1/security block high applied to the original nine models. These detector outcomes were not rerun for the revision. Revised plans retain ASVS1/high-critical blocking, 36 distinct numbered threats and reserved T-01-SC. No independent checker was run by the planner.

Initial frontmatter/structure results for nine plans are historical only. Revised 14-plan GSD frontmatter/structure queries returned valid, and the corrected read-only scan returned 28 tasks, seven waves, 36 unique numbered threats, all 13 D citations and zero manifest/dependency/same-wave/field/tail errors; details are recorded separately in VALIDATION.md. Native/build/SQL application tests remain execution work, never planning passes.

All in-scope GOAL/REQ/RESEARCH/CONTEXT items are COVERED. Unclassified assumptions and descriptor-less prohibition enforcement are flagged-unverified, not unplanned feature omissions. Application tests/builds/runtime safety have NOT run for these new deliverables. No source edits, commits, branch changes or STATE edits belong to this planning invocation.
