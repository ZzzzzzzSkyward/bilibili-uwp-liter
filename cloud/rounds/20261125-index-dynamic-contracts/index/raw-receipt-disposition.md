# Full-receipt archive disposition — COMPLETE / FROZEN / RELEASED

Archive-only owner `archive_evidence_executor`, 2026-11-25T07:37:18.337848+00:00. Source E `d11dbf867c1896bce7d41956a4b78fc2580d5a5c` and tree `c395ab79d96242b244cc169f7b60bc67e360714a` were already pushed and independently postchecked; publisher custody was released before this action. No test or Git command ran.

Exactly four owner-generated untracked full runtime assertion receipts were absent from the current index, published E tree and frozen 31-path source manifest. Each was copied byte-for-byte into exclusive private storage, independently re-read for identity, then only the untracked repository duplicate was removed.

| Historical repository path | Full cases | Bytes | SHA256 | Preserved private path |
| --- | ---: | ---: | --- | --- |
| `cloud/rounds/20261125-index-dynamic-contracts/index/evidence/original-candidate-receipt.json` | 118 | 27476 | `81ea47800696ddbcf508cbdff9439a63a150c7aa739deb77690a1dabcca23d5e` | `/workspace/work/index-dynamic-contracts/index/archive-raw-receipts/evidence/original-candidate-receipt.json` |
| `cloud/rounds/20261125-index-dynamic-contracts/index/evidence/supplement-candidate-receipt.json` | 34 | 5595 | `0c5e364fc24bcd23b8460bc19cb69d0487602b97ec0c65e18d66284c1af18887` | `/workspace/work/index-dynamic-contracts/index/archive-raw-receipts/evidence/supplement-candidate-receipt.json` |
| `cloud/rounds/20261125-index-dynamic-contracts/index/evidence-history/initial/original-candidate-receipt.json` | 118 | 27476 | `81ea47800696ddbcf508cbdff9439a63a150c7aa739deb77690a1dabcca23d5e` | `/workspace/work/index-dynamic-contracts/index/archive-raw-receipts/evidence-history/initial/original-candidate-receipt.json` |
| `cloud/rounds/20261125-index-dynamic-contracts/index/evidence-history/initial/supplement-candidate-receipt.json` | 34 | 5595 | `0c5e364fc24bcd23b8460bc19cb69d0487602b97ec0c65e18d66284c1af18887` | `/workspace/work/index-dynamic-contracts/index/archive-raw-receipts/evidence-history/initial/supplement-candidate-receipt.json` |

Old source-proof paths and hashes describe the actual receipts at execution time. The private paths above now supersede those historical locations, with exact original bytes, assertion rows and results retained. This does not invalidate execution-time hashes. No committed binding, execution, release, grant or first-history metadata was rewritten.

All **31 source payload files / 11 runnable files** retain their frozen SHA256 values. All **39 other index files** retain their bytes. Direct read-only parsing of the index and loose E tree proved all four receipts untracked and absent; Git index/HEAD/refs/config remained unchanged. No full runtime assertion arrays remain in `index/evidence/**` or `index/evidence-history/**`. Other private fixtures/results/history remain untouched.

Exact preservation/check hashes and custody are in [raw-receipt-disposition.json](raw-receipt-disposition.json). Private preservation manifest: `/workspace/work/index-dynamic-contracts/index/archive-raw-receipts/preservation-manifest.json`, SHA256 `a19f49b717030fde9cc520818bb36f3e15efff34516616603522e88c1bf52f7c`.

**FROZEN / RELEASED:** all disposition outputs, private preserved receipts and read/write custody. Later archive must include this concise disposition and exclude all full receipts; no source acceptance or suite rerun follows from this packaging action.
