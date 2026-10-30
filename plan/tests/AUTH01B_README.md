# AUTH-01b offline checks

Run `./plan/tests/run-auth01b.sh`, or use the aggregate `run.sh`.
The standalone .NET SDK location is the same as the existing API-01 runner.
These commands compile focused source fragments, not the UWP application.

The probes compile the production session validator/coordinator/committer and
isolated verification/exchange transport. The runner also extracts the actual
Account cookie-commit adapter from `Modules/User/Account.cs`, rather than copying
its persistence implementation into tests. HTTP, Windows cookies, settings and
notification boundaries are stubbed and failures can be injected there.

`AccountApi` is stubbed in the transport harness. Its production request builders,
including the independent cookie-to-app signature vector, are covered separately
by `run-api01.sh`. Both runners are included in the aggregate offline checks.

The LoginVM harness extracts the production password and secondary-verification
method bodies. Only the generated test signatures change from `async void` to
`async Task`, allowing tests to await and drain gated responses deterministically.
Property notification, account persistence, request builders, XAML events and
Windows APIs remain boundary stubs there. Constructor subscription and XAML
event wiring are source-reviewed separately; the harness exercises the actual
logout handler and cancellation methods.

Final reviewed results: 70 API compatibility assertions, 67 session/transport/
Account adapter assertions, and 39 LoginVM lifecycle assertions passed, along
with the existing LM-01–05 aggregate probes.

The checks cover unsuccessful and incomplete exchange responses, staged cookie
validation, explicit cookie headers and disabled automatic cookies/redirects,
callback validation, stale operations, one-time session commit, and rollback when
cookie or settings persistence fails. The Account adapter checks include
restoration of the prior cookie settings and removal of newly staged cookies.

These probes do not exercise a Windows WebView, the actual cookie manager,
network authentication, captcha service, or UWP XAML compilation. Real password,
SMS, QR, secondary verification, session restoration and logout still require
application testing on Windows. Project/XAML XML and explicit source/content
entries are checked separately by source review and XML parsing.
