# Sibling discovery requirements and bounded handoff

Status: **COMPLETE — ready for independent requirements review**, 2026-11-25 UTC. Author: `api_siblings_requirements`, `gpt-6.1-sol` / `xhigh`. Source inventory was released before detailed comparison. [inventory.md](inventory.md) is the immutable custody manifest; [evidence.md](evidence.md) contains field-level request → transport → decoder → caller observations E1–E8. This document does not authorize implementation or report a main-repo defect before the contract expert's actual-source reproduction.

## Background and objective

The user requested pulling actively maintained Bilibili client/API projects, comparing actual usage and improving this client. The old 2026-11-09 route table is insufficient evidence for current defects. Discovery therefore obtained three distinct implementation lineages outside this repository: a current Flutter client, a current Kotlin/Compose client and a recently maintained API-focused Python fork. Public metadata, substantive change inspection, exact dates/branches/full commits/licenses and source-only clean checkouts are recorded. The API fork meets the 90-day preference narrowly and its broad inherited catalog is explicitly weaker evidence.

Goal: supply a reproducible source evidence set that lets the independent contract expert select **1–3 coherent current existing-feature repairs**, or record why a candidate is already correct / family-specific / unsupported. Compare actual wire shape and consumers, including host/method, request names/types, encoding, auth/signatures/cookies, envelope and scalar/null/array/ID/cursor/error/acknowledgement handling. Route parity alone is not acceptance.

Non-goals: new account modes/features, endpoint-count parity, blanket DTO/parser migration, copying foreign implementation, bulk library/proto changes, foreign execution, native-player or live protocol changes, remote mutations, credentials borrowing or challenge bypass.

## Source-derived interface/data requirements

| Requirement | Reliable observed fact | Required interpretation for our comparison |
| --- | --- | --- |
| SR1: named favorite/search text and one encoding boundary | E1: S1/S2/S3 all send favorite `keyword` as a named value; web search E4 uses named `keyword` and a signed query map. | Trace **our** constructor plus transport/signing caller. Reserved characters (`&`, `=`, `+`, `%`, Unicode, literal `?`, quotes) must decode to the original field value once; fields must stay separate and the signed representation must match the transmitted one. Do not change app/web field names by analogy. |
| SR2: source-specific request families | E3/E4/E6/E8 distinguish app/web/gRPC, including web `page`/`search_type` versus app `pn`/numeric `type`, and separate PGC payload keys. | Preserve the initiating source family, host, auth and payload key. A newer sibling route is a candidate for investigation, not grounds to replace our route. Missing required envelope key remains failure for that endpoint. |
| SR3: null collections and IDs are field-specific | E1: two actual favorite consumers accept nullable `medias`; decoder integer/string ID declarations differ. E4 deliberately tolerates numeric/string search counters. | Evaluate an exact shipped parser field, including absent/null/list/malformed variants and caller behavior. Valid optional empty collections should be usable; malformed present collections/required identifiers should not fabricate success. Do not infer universal coercions from DTO disagreement or lose large identifiers through floating point. |
| SR4: cursor tokens are opaque and owned by their source | E3/E5: opaque web `next_offset`/dynamic `offset` coexist with integer page/cursor and gRPC cursor contracts. | Serialize embedded JSON with a proper serializer, then apply the correct URI/form encoding once. Round-trip the original string including backslash/quote/control characters in synthetic tests. Preserve last accepted cursor on append failure and reset on source/filter change; never convert opaque strings into IDs or cross-source cursor state. |
| SR5: acknowledgements differ from data payloads | E2: form actions accept `code==0` without requiring a data model; E8: missing/nonzero code is rejected. | Keep existing typed action success and only mutate local state on accepted acknowledgement. Do not require nonempty `data` for a code-only action, or allow a missing/invalid code. A required-data list/detail endpoint still requires its declared payload. |
| SR6: error content can exist within an outer success envelope | E4: S1 treats a search `data.v_voucher` response as failure despite outer code0. | If our shipped consumer can misinterpret the same typed response, reproduce it and specify a recoverable error path preserving query/page/state. Do not bypass any challenge or claim its absence from a single sibling DTO proves normal empty results are invalid. |
| SR7: display strings and optional quality are not exact numeric truths | E7: `online.total` is consumed as display String; API fork handles missing stream quality. | Only improve an actual current consumer that fails on a supported shape. Preserve text where the field is display text; do not round or fabricate counts. No native/player/session ownership changes are authorized by these facts. |

## Prioritized contract candidates for independent reproduction

These are **investigation bundles**, deliberately not asserted defects. The contract owner chooses the final bounded set after tracing our actual sources and existing regression coverage.

1. **Favorite list/search request and optional-list shape (E1, SR1/SR3).** This has three independent named-keyword request implementations and two current typed null-list consumers, so it offers the strongest exact shared contract. Check current `Api/User/FavoriteAPI.cs`, its actual transport, favorite DTO/parsers and detail/search callers. Look for an omitted/misnamed/concatenated keyword, double encoding, or a null collection being misreported as malformed / success state being advanced on malformed payload. If repaired already, record it as verified current behavior and retain its existing fixtures.
2. **Comment/dynamic cursor serialization and accepted paging (E3/E5, SR4).** Inspect our actual web cursor builders/parsers/callers; proper embedded-JSON serialization is higher confidence than copying either sibling's quote-only replacement. A synthetic opaque token with a backslash, quote and newline can be a real-source counterexample to hand concatenation without requiring service access. No route, auth or gRPC migration is part of this candidate. Reject it if our current implementation already round-trips correctly.
3. **Typed web-search payload/counter/error shape (E4, SR3/SR6), or endpoint-specific PGC key (E6, SR2).** Check only a traced current caller: `data.result` list versus all-search grouped lists, exact numeric/string counter policy, and `v_voucher` handling. Preserve already-reviewed direct saved-BUVID / nonempty-area proxy behavior and app order signing. PGC key comparison is a fallback only when our actual endpoint declares a conflicting key and a deterministic parser fixture proves impact; no universal data/result fallback.

E2 acknowledgement behavior is already corroborating evidence for our recent favorite boundary work; it must be regression-preserved, not reopened as a new fix. E7 is lower priority / limited evidence and should not expand the round into the separately owned memory/player work.

## Existing dispositions that must remain intact

`document/BILIBILI_API_RESEARCH.md:15–44`, current maintenance memory and the round plan show U1, U5/U8, U9, PB1 and the 2026-11-25 favorite/boundary fixes already accepted. Discovery rechecks shape without treating historical descriptions as current bugs. Exact preserved behavior includes explicit encoded comment CSRF with raw Add/pre-escaped Reply conventions; separate folder timestamp/privacy before signing; saved real BUVID on direct web search; existing nonempty-area proxy compatibility; encoded app order inside actual signing; typed code0 favorite acknowledgements and initiating-detail ownership.

Account P4/P5 remain on hold. Keep the newer local OAuth `/api/v2/oauth2/refresh_token`, credential transaction/session provenance and cookies across existing origins; no sibling downgrade. Live U2 remains FAILED/BLOCKED pending genuine BUVID/app-fallback identity, token/host/auth/protocol fixtures and controlled integration evidence. No discovery step supplies those prerequisites. gRPC/protos, live messages and native transport/session changes are not candidate scope.

## Acceptance for this discovery and the next gate

Discovery acceptance:

- Three pinned useful independent lineages, including a current client and an API implementation, each nonarchived with an inspected substantive implementation change within 90 days, and honest narrow-maintenance qualification. **Met**; initial inventory contains activity, ref, dates, licenses, exact local paths and read-custody release.
- Actual request/transport/decoder/caller observations, uncertainty and source-family differences. **Met** for E1–E8 at the stated levels; table explicitly marks declarations and untraced builders rather than presenting them as active screen usage.
- Foreign source read as data; no foreign code execution, dependencies/hooks, authenticated/write requests or wholesale vendoring. **Met**.
- Only assigned `siblings/**` Markdown and exclusive private cache touched by this owner. **Met**; no main-repo index/ref or product/test/build/main-doc write, test-suite run or publication.

Before execution, each selected main-repo repair must have: exact stable local baseline; shipped caller trace; sibling evidence IDs and independently bounded contract; a meaningful **identical final synthetic fixture that fails original real source and passes repaired real source**; expected error/state outcome; exact proposed single file owner; owning affected regressions; and independent requirements PASS. Sibling fixtures or a reimplementation of our logic do not satisfy actual-source reproduction. Assertions must not be relaxed and the fixed 51 logical / 52 executable inventory must remain explicit.

After implementation, an executor must preserve the fixture unchanged, run owning/affected tests, obtain independent implementation/fresh audit, then participate in the serialized integrated/publication gate after the memory round closes. Windows UWP build, native behavior, real cookies/session, live endpoint acceptance and actual write outcomes remain separate unverified gates unless directly observed. No moving whole-tree run is requested during discovery.

## Expected change surface and risks

The final contract expert, not this source owner, assigns exact files after current reproduction. Likely bounded surfaces are one existing builder / one feature parser or mapper / its shipped caller policy plus the already-owning probe batch. Prefer existing feature contracts over shared `ApiEnvelope`, global transport/account/auth, gRPC schemas or player ownership. Main docs/provenance update follows implementation in its own assigned serial window.

Risks: stale inherited API catalog despite recent fork fix; sources sharing an upstream implementation; current client shipping an unused legacy builder; optional parameter policy differing by client; wrapper signing/cookie transforms hidden outside a builder; tolerant DTOs masking malformed replies; and falsely converting useful source evidence into live service claims. Inventory ancestry, E-level labels and caller traces explicitly bound those risks.

## Final custody

S1/S2/S3 source pins remain frozen at the initial manifest and released for read-only use by contract expert/reviewer/executor. This owner retains no ongoing mutation of caches. Any future re-fetch belongs in a new private directory and a separately released inventory, never an in-place pin update. Requirements/evidence are ready for independent review; this owner has no source implementation/publication grant.
