# Bilibili API 字段形状对照与窄修复

> PREPARATORY DRAFT — intended destination: `document/BILIBILI_API_SHAPE_COMPARISON.md`. Links are relative to that destination. Requirements/provenance are frozen; API implementation, owning/fresh reviews, combined source publication, complete gate and hosted result remain **PENDING**. This draft does not accept moving code.

2026-11-25 的新比较从三个维护中的独立源码谱系检查真实 request→transport→decoder→caller，寻找当前已发布功能的具体字段问题。它补充 [2026-11-09 旧研究](BILIBILI_API_RESEARCH.md)，不重算旧 **203 HTTP method/route pairs /165已有/38无精确对应**，也不重生成 [旧端点目录](BILIBILI_API_ENDPOINTS.md)。路由数量、仓库活跃或兄弟客户端使用某字段，均不证明官方承诺或当前服务接受。

## 1. 不可变来源、license 与维护证据

下列仓库公开元数据在2026-11-25观察为 nonarchived/non-disabled，源码缓存以完整commit detached、clean冻结。首选90日窗口从2026-08-27起；检查了具体实现变更，不以stars、issue、bot/README时间代替。完整库存/拒绝候选见 [inventory](../cloud/rounds/20261125-api-shape-comparison/siblings/inventory.md)。

| 来源/谱系 | 分支、不可变pin与pin日期UTC | License at pin | 实质维护依据与限制 |
| --- | --- | --- | --- |
| Flutter客户端 [PiliPlus](https://github.com/bggRGjQaUbCoE/PiliPlus/tree/217fb54f51f6764176319bbdedab2a7522d75401) | main；`217fb54f51f6764176319bbdedab2a7522d75401`；2026-11-14 08:04:43 | LICENSE GNU GPLv3；GitHub SPDX GPL-3.0 | [c7412790](https://github.com/bggRGjQaUbCoE/PiliPlus/commit/c741279080fa90571d59ff9558640beb88861492)，2026-10-31，favorite/subscription page order的请求/DTO/caller改动；另有11月API实现变更 |
| Kotlin/Compose客户端 [bilimiao2](https://github.com/10miaomiao/bilimiao2/tree/d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b) | master；`d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b`；2026-11-20 16:10:43 | LICENSE GNU GPLv3；GitHub SPDX GPL-3.0 | [8ea47ff8](https://github.com/10miaomiao/bilimiao2/commit/8ea47ff800d7c5927a180f129e236f43737dde8f)，2026-09-09，gRPC/KMP实现与playlist适配；11-20另有player键盘实现。不是REST端点刚经服务器验证的证据 |
| Python API实现 [nooblong/bilibili-api](https://github.com/nooblong/bilibili-api/tree/4789508a93f3a39b7d3c896273309db892a9cc8b) | main；`4789508a93f3a39b7d3c896273309db892a9cc8b`；2026-09-28 19:50:14 | LICENSE GNU GPLv3；GitHub SPDX GPL-3.0 | [425f5200](https://github.com/nooblong/bilibili-api/commit/425f5200f5fa17a316b93cdf21643513d9a3075f)，2026-08-28，stream comparator四处缺quality保护，09-28合入pin；89日窄维护，不能称整份继承catalog已更新 |

Python来源是已归档 Nemo2011 分支的维护fork，只计一个谱系；三个源不是同一实现的三个镜像。旧ywmoyue客户端与本项目有共同谱系，不计新独立佐证。全部GPL源码只读作事实/接口证据，窄修复和synthetic fixtures独立实现；未移植外国实现、vendor代码或执行foreign build/test/example/hook/dependencies。没有Bilibili服务请求、认证/写入、借用凭据或challenge bypass。

证据分为 implementation usage、只有声明的DTO/schema、字段级独立佐证、service-verified。**本次没有service-verified**；DTO分歧不自动证明服务器实际返回两种类型。维护日期不自动属于每个endpoint。

## 2. 字段级观察与不同请求家族

完整路径/行号/强度见 [sibling evidence E1–E8](../cloud/rounds/20261125-api-shape-comparison/siblings/evidence.md) 与 [本地caller矩阵](../cloud/rounds/20261125-api-shape-comparison/contracts/matrix.md)。以下是观察到的实现期望，非通用API schema：

| 字段/家族 | 固定源码中的观察 | 本地处理边界 |
| --- | --- | --- |
| Personal-space/following文本 | [PiliPlus member](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/http/member.dart#L349) 与 [Python user](https://github.com/nooblong/bilibili-api/blob/4789508a93f3a39b7d3c896273309db892a9cc8b/bilibili_api/user.py#L429) 将keyword作为named value；following name也有独立named-value用法 | 本地UserInfoPage→UserSubmitVideoVM→SubmitVideos(Wbi alias)与UserFollowVM→Followings在parser/signing前保护文本，不改route/host/cookie/gRPC选择 |
| Comment cursor | [Python comment](https://github.com/nooblong/bilibili-api/blob/4789508a93f3a39b7d3c896273309db892a9cc8b/bilibili_api/comment.py#L469) 使用 `/x/v2/reply/wbi/main`、mode/oid/type、JSON pagination_str与WBI；[PiliPlus reply](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/http/reply.dart#L21) 匿名non-WBI main与登录pn route不同，主评论屏另用gRPC | 本地保留正确JSON serializer，只把序列化后JSON作为一个query value编码；pn/next/next_offset/gRPC cursor各自归源，不迁route。兄弟quote-only JSON手拼算法不复制 |
| Favorite list | [PiliPlus fav](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/http/fav.dart#L54) 与 [bilimiao UserApi](https://github.com/10miaomiao/bilimiao2/blob/d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b/bilimiao-comm/src/commonMain/kotlin/com/a10miaomiao/bilimiao/comm/apis/UserApi.kt#L206) 用 `/x/v3/fav/resource/list`、named keyword/pn/ps；两者decoder/caller处理nullable medias与boolean has_more，Python只佐证request字典 | 本地nullable bool/type校验本已存在；AS3只把正常source有效flag用于paging，保持Type21与absent/null兼容。ID的int/string DTO分歧不足以扩大所有scalar coercion |
| Action vs data payload | favorite batch-deal/comment action的form字段与code0-only ack有caller；single deal与batch-deal字段不同 | 保留typed acknowledgement，不要求action有非空data；list/detail依其declared payload，missing/malformed code不是success |
| Web/app搜索 | web `/x/web-interface/wbi/search/type` 与 `/all/v2` 用page/page_size/search_type字符串及WBI，typed结果list不同于all分组；app `/x/v2/search` 用pn/ps与numeric type，当前部分屏已用gRPC | 不互抄参数/签名家族。S1容忍部分数值/字符串counter不证明中文格式数值应精确转换；v_voucher失败不能作普通empty或绕过challenge |
| Dynamic/PGC/display | web dynamic offset是opaque string，与legacy ID/gRPC不同；`/pgc/view/web/season` 读result、PUGV读data，app V2/websection各有envelope；online.total被实际caller作display String | 不把offset变整数，不全局 `data ?? result`，不把display计数伪造成精确整数。streamquality缺失观察不授权native/player改动 |

## 3. AS1/AS2：编码在query parser之前，保留当前signer

AS1的目标是 `UserDetailAPI.SubmitVideos`（含SubmitVideosWbi alias）与现有Followings name-bearing search branch；AS2是 `CommentApi.CommentV2`。独立 [初审](../cloud/rounds/20261125-api-shape-comparison/review.md) 接受其源码缺陷与范围；最终需求由 [AS3复审PASS](../cloud/rounds/20261125-api-shape-comparison/as3-review.md) 闭合。实现/最终review仍 **PENDING: API_QUERY_EXECUTION / API_QUERY_FINAL_REVIEW**。

| 原行为/真实源码synthetic反例 | 目标行为 |
| --- | --- |
| raw `C++` 在HttpUtility.ParseQueryString后变空格；literal `%2B` 被提前解码；`A&B`或`&pn=...`裂成额外query字段 | raw文本在进入query parser前编码一次；最终decoded keyword/name保留literal +/%/&/Unicode，不改变pn等控制。`%2B`的raw文本可见 `%252B` 中间编码是保护literal `%`，不是对已编码输入重复编码 |
| 已正确序列化的pagination JSON被raw插入query；其中+/%/&可改变JSON/text或ps | 保留JSON serializer，把整个JSON编码为**一个**pagination_str值；decode query再parse JSON得到原offset，受现有WBI规则限制 |

“编码一次”描述每个边界，不是移除后续签名URL编码：builder先保护raw值；真实signer解析成values并按现有规则输出签名query。全局ApiHelper/WbiKeyService、WBI `!'()*` sanitation与known digest vector保持；不承诺被该既有规则过滤的字符原样通过。检查最终decoded字段与实际generated wts对应digest，不能只看builder字符串。

AS1保留null interpolation的empty效果和null/empty keyword原分支、tid=-1/0/positive、order/order_type、mid/vmid/pn/ps/tid、configuredhost/referer/cookie/alias。AS2保留hot3/new2、oid/type/ps、login cookie/optionalcsrf、异步signing、source fallback与失败时last-good cursor。U1 Add raw/Reply preescaped及encodedcsrf writes不变；全局SearchAPI七条web路径的BUVID/nonempty-area兼容与app order既有修复不重开。

## 4. AS3：正常favorite boolean优先，兼容fallback明确

正常source是 Type!=21 `/x/v3/fav/resource/list`；Type21是 `/x/space/fav/season/list`，没有已捕获shape授权改变其paging。原正常VM忽略已解码的nullable has_more而仅比较folder media_count，导致filtered terminal页继续翻、旧总数恰等当前row数时提前停止。目标不是重写parser/mapper或把缺失flag变false。

令 N=**null-row过滤后**本次decoded rows，C=`Videos.Count != FavoriteInfo.media_count`（在成功发布/append后，以已接受info计算）。ON=ShowLoadMore true、Page恰增1；OFF=false、Page保持所请求值；COUNT=按C选择ON/OFF。成功append忽略incoming info，保留已接受info及原有row身份/顺序。

| Source | has_more | 成功非空首/append | 成功empty append | 成功empty第一页（有info） |
| --- | --- | --- | --- | --- |
| Normal | true | ON | OFF | FIRST-EMPTY |
| Normal | false | OFF | OFF | FIRST-EMPTY |
| Normal | absent | COUNT | COUNT | FIRST-EMPTY |
| Normal | null | COUNT | COUNT | FIRST-EMPTY |
| Type21 | true | COUNT | COUNT | FIRST-EMPTY |
| Type21 | false | COUNT | COUNT | FIRST-EMPTY |
| Type21 | absent | COUNT | COUNT | FIRST-EMPTY |
| Type21 | null | COUNT | COUNT | FIRST-EMPTY |

这等价于 [审定16-row矩阵](../cloud/rounds/20261125-api-shape-comparison/contracts/requirements.md#as3--favorite-detail-consumes-its-endpoints-has_more-p1) 的empty/nonempty分开情况。FIRST-EMPTY沿用既有提前返回：发布有效info/ownership/collect，Nothing=true，保留pre-call Videos引用/内容、Page1不动、隐藏Load more、Loading=false；不再计算COUNT。第一页missinginfo则拒绝publication并保留旧info/rows/Page1，Nothing=false、隐藏Load more、Loading=false与既有notice。

正常presentboolean empty append才新增terminal规则。例如Page2/prior1row/count42/empty：normal true/false →OFF/Page2；normal absent/null与Type21任意validflag →COUNT真/Page3（原行为）。count相等则COUNT false/Page2。不能用无条件empty早退覆盖这些兼容分支。

Malformed presentboolean仍typed Payload失败，包含Type21既有校验。transport/business/envelope/payload失败append保留rows/info/Page，沿用ShowLoadMore=false/Nothing=false/Loading=false及分类notice；不执行新paging决定，retry仍请求同Page。stringIDs/null-row/optionalpayload/code0action与现有encodedkeyword/pn保持。最终实现/review **PENDING: API_FAVORITE_EXECUTION / API_FAVORITE_FINAL_REVIEW**。

## 5. 实际证据、保留项与下一门禁

Discovery基线是 `7356b94893377558191b0d70576a52bae704c1f9` 的12个冻结API/transport/parser snapshot，另有同时进行的memory改动；不是把moving whole tree当稳定commit。初审独立重现相同fixtures：query25checks中15个原红、favorite9checks中6个原红。AS3补充124原绿compatibility checks独立PASS；presentboolean empty两例原红（8checks中的2个失败），均为完整本地源码+syntheticdelivery，不是服务器capture或最终修复绿。

最终待绑定 `COMBINED_SOURCE_COMMIT / COMBINED_SOURCE_BINDING / API_QUERY_FINAL_REVIEW / API_FAVORITE_FINAL_REVIEW / API_FRESH_REVIEW / COMBINED_GATE_LINK / COMBINED_GATE_RESULT / HOSTED_SHA_STATUS`。代码owners当前active，未在本稿接受。将来最终证明必须用完全相同fixture/stubs/tools执行原真实源码红→修复绿，保留API02原12+search1258、comments、c2/c3、相关auth1/API01及原124compatibility，不放宽断言。仅在所有memory/API owners/reviews冻结后跑一次combined51logical/52executable、complete_required=true gate；工具/runner14/package15独立，不混分母。提交之后按blob identity绑定，不为Markdown或commit标签重复跑unchanged suites。

U1/U5/U8/U9/PB1、typedfavoriteack/optionalpayload/boundedinteger/nullrow/initiating-detail所有权保持。OAuth `/api/v2/oauth2/refresh_token`与credential/session/cookie provenance不降级；账号P4/P5仍hold。Live U2仍FAILED/BLOCKED：先取得可信existingBUVID或验证appfallback、token/hosts/auth/protocol/cancel及受控session，再审查完整活动路径，保留BR-63重连；本研究不提供这些条件。未确认的watchlater签名/alternative route不作新修复。

Windows VS/UWP build、真实cookie/account/session、搜索/评论/收藏服务结果、写入、liveprotocol与安装启动均UNVERIFIED/FAILED-BLOCKED于Linux。下一步在Windows跑 `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump -Restore`，再按 [新增API场景](../plan/VALIDATION.md#2026-11-25--api字段形状的新增-windows与服务跟进未运行) 验证UI输入/分页/断网重试与实际response。受控fixture要标synthetic，不能称livecapture；不为补齐研究进行未授权认证/写请求。Memory BR-21的50真实视频/nativeRAM/root/heap/GPU/pixel门禁仍由独立 [VIDEO_MEMORY](VIDEO_MEMORY.md) 流程约束，API离线通过不能替代。
