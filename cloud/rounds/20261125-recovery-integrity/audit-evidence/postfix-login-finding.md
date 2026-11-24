# FA-08 / P2 — recommendation login result is interpreted backwards

A final bounded inspection of the touched recommendation action's real login caller found an additional directly supported recovery failure. `RecommendVM.Dislike` tests `!SettingHelper.Account.Logined && await Utils.ShowLoginDialog()`. The actual helper returns true when the dialog leaves the account logged in and false when it remains logged out (`Utils.cs:241–257`). The action therefore refuses the just-successfully authenticated user with `请先登录`, while a cancelled login continues to the action request.

The actual RecommendPage menu/reason handlers await this same Dislike method for dislike/feedback. This is an ordinary logged-out user activating an existing action, not an unsupported direct VM state mutation or native rendering assumption. The request fixture records delivery only; the proof does not claim a real unauthenticated service accepts the request.

A separate scratch project compiles full actual RecommendVM and the **complete exact actual ShowLoginDialog body**, with a primitive LoginDialog.ShowAsync callback that either sets the account-presence boolean true or leaves it false. The actual helper, rather than a fake bool-return policy, computes its return. The feed is loaded through real parsing first; the action then uses an ordinary code0/no-data response primitive. Observed:

```text
LOGIN_RESULT success=True;logined=True;requests=0;rows=1;toasts=请先登录;contract_pass=False
LOGIN_RESULT success=False;logined=False;requests=1;rows=0;toasts=;contract_pass=False
LOGIN_AUDIT failures=2
```

Command is the same exact SDK/cache environment documented in postfix-caller-findings.md, with project `/workspace/work/recovery-integrity/fresh-audit-postfix/login/CallerAudit.csproj`; **exit1** after successful compilation. There are no held gates/background transfers in this focused driver; native login/credentials/network are never accessed.

**Required minimal amendment/owner:** original recovery executor, the RecommendVM login guard and additive owning recovery action checks. Proceed after successful login; return without action request/mutation if login is cancelled/unsuccessful; preserve already-logged-in behavior, feed collection/state ownership and feedback/dislike request fields. Test success, cancellation, and ordinary already-logged-in controls through the real action, with the actual helper's independently established bool contract. No Utils/credential/account-parser/schema change or new physical stage is requested. Preserve the final failing fixture and show the same predicates green after the repair. This P2 must join the file-based caller amendment before a source grant.

Source bindings remain the current frozen915-input manifest3cb4d3c1ca57b935a738ab882943f1086c6f280587c56d58c61ccf1667842ff7. The login driver binds19 inputs/eight exact slices; all current source hashes match. Exact hashes are appended in postfix-closure.md; raw inputs/program/log remain in the scratch directory above. No source/test/index/ref/main-doc mutation occurred.
