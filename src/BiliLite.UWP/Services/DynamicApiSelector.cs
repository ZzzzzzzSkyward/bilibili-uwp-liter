using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BiliLite.gRPC;

namespace BiliLite.Services
{
    /// <summary>动态流的两套数据来源</summary>
    public enum DynamicApiSource
    {
        Grpc = 0,
        Legacy = 1,
    }

    /// <summary>
    /// 需要预测来源的操作。首页视频流与用户视频流共用 DynVideo RPC,
    /// 但转换规则和 REST 分页不同,所以必须是两个独立的桶。
    /// </summary>
    public enum DynamicApiOperation
    {
        HomeVideoFeed = 0,
        UserAllFeed = 1,
        UserVideoFeed = 2,
        SpaceFeed = 3,
        DynamicDetail = 4,
        Reposts = 5,
    }

    /// <summary>一次请求的结果分类</summary>
    public enum DynamicApiOutcome
    {
        /// <summary>完整解码且信封有效,空列表也算</summary>
        Success = 0,
        /// <summary>服务端明确拒绝(含 bili-status-code=-663)</summary>
        EndpointRejection = 1,
        TransportFailure = 2,
        Timeout = 3,
        /// <summary>HTTP 5xx</summary>
        ServerError = 4,
        MalformedResponse = 5,
        /// <summary>未知业务码:只留诊断,不影响可用性,也不制造"不支持"结论</summary>
        UnknownBusinessError = 6,
        /// <summary>取消、被取代、需要登录、已删除/私密/不存在、本地映射失败:与端点可用性无关</summary>
        IgnoredFailure = 7,
    }

    public enum DynamicApiDecisionReason
    {
        /// <summary>没有历史,保持 gRPC 优先</summary>
        DefaultGrpcFirst = 0,
        PreferenceRetained = 1,
        ReliabilityAdvantage = 2,
        LatencyAdvantage = 3,
        RecoveryTrial = 4,
        /// <summary>原先的首选在冷却,暂时只能用另一路</summary>
        PreferredSourceInCooldown = 5,
        /// <summary>两路都在冷却,取冷却先结束的一路</summary>
        BothSourcesInCooldown = 6,
    }

    public static class DynamicApiOutcomes
    {
        /// <summary>降低端点可用性的结果</summary>
        public static bool IsFailure(DynamicApiOutcome outcome)
        {
            switch (outcome)
            {
                case DynamicApiOutcome.EndpointRejection:
                case DynamicApiOutcome.TransportFailure:
                case DynamicApiOutcome.Timeout:
                case DynamicApiOutcome.ServerError:
                case DynamicApiOutcome.MalformedResponse:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>计入两分钟内连续失败次数的结果</summary>
        public static bool IsTransientFailure(DynamicApiOutcome outcome)
        {
            switch (outcome)
            {
                case DynamicApiOutcome.TransportFailure:
                case DynamicApiOutcome.Timeout:
                case DynamicApiOutcome.ServerError:
                case DynamicApiOutcome.MalformedResponse:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>成功与失败都会进入样本;其余结果只记诊断,不动可用性</summary>
        public static bool CountsTowardAvailability(DynamicApiOutcome outcome)
        {
            return outcome == DynamicApiOutcome.Success || IsFailure(outcome);
        }
    }

    /// <summary>
    /// 把结构化失败信息翻译成结果分类。只看状态码与失败类型,不解析 message 文本。
    /// </summary>
    public static class DynamicApiOutcomeClassifier
    {
        // 平台的风控/签名拒绝:换一路请求往往就通了
        private static readonly int[] EndpointRejectionCodes = { -663, -352, -412 };
        // 内容/权限条件:换源解决不了,不降低可用性
        private static readonly int[] NeutralBusinessCodes = { -101, -400, -403, -404, -10403 };

        public static DynamicApiOutcome FromFailureKind(GrpcFailureKind kind)
        {
            switch (kind)
            {
                case GrpcFailureKind.None:
                    return DynamicApiOutcome.Success;
                case GrpcFailureKind.EndpointRejection:
                    return DynamicApiOutcome.EndpointRejection;
                case GrpcFailureKind.Timeout:
                    return DynamicApiOutcome.Timeout;
                case GrpcFailureKind.HttpServerError:
                    return DynamicApiOutcome.ServerError;
                case GrpcFailureKind.MalformedResponse:
                    return DynamicApiOutcome.MalformedResponse;
                case GrpcFailureKind.Transport:
                    return DynamicApiOutcome.TransportFailure;
                default:
                    return DynamicApiOutcome.TransportFailure;
            }
        }

        /// <summary>gRPC 结果分类:status 为 true 就是成功(信封有效性由调用方解码后再判定)</summary>
        public static DynamicApiOutcome FromGrpc(GrpcResult result)
        {
            if (result == null)
                return DynamicApiOutcome.TransportFailure;
            if (result.status)
                return DynamicApiOutcome.Success;
            if (result.failureKind != GrpcFailureKind.None)
                return FromFailureKind(result.failureKind);
            if (result.biliStatusCode.HasValue)
                return FromBilibiliCode(result.biliStatusCode.Value);
            return DynamicApiOutcome.MalformedResponse;
        }

        /// <summary>动态 GrpcService 抛出的异常分类</summary>
        public static DynamicApiOutcome FromGrpcException(GrpcRequestException exception)
        {
            if (exception == null)
                return DynamicApiOutcome.TransportFailure;
            if (exception.Result != null)
                return FromGrpc(exception.Result);
            return FromFailureKind(exception.FailureKind);
        }

        /// <summary>B 站业务码分类(HTTP 已成功的 REST 响应,或响应头里的 bili-status-code)</summary>
        public static DynamicApiOutcome FromBilibiliCode(int code)
        {
            if (code == 0)
                return DynamicApiOutcome.Success;
            for (var i = 0; i < EndpointRejectionCodes.Length; i++)
            {
                if (EndpointRejectionCodes[i] == code)
                    return DynamicApiOutcome.EndpointRejection;
            }
            for (var i = 0; i < NeutralBusinessCodes.Length; i++)
            {
                if (NeutralBusinessCodes[i] == code)
                    return DynamicApiOutcome.IgnoredFailure;
            }
            return DynamicApiOutcome.UnknownBusinessError;
        }

        /// <summary>只看 HTTP 状态码</summary>
        public static DynamicApiOutcome FromHttpStatus(int httpStatus)
        {
            if (httpStatus <= 0)
                return DynamicApiOutcome.TransportFailure;
            if (httpStatus < 400)
                return DynamicApiOutcome.Success;
            if (httpStatus < 500)
                return DynamicApiOutcome.EndpointRejection;
            return DynamicApiOutcome.ServerError;
        }

        /// <summary>REST 请求:HTTP 状态与 JSON 业务码分开判断</summary>
        public static DynamicApiOutcome FromLegacy(int httpStatus, int businessCode)
        {
            var transport = FromHttpStatus(httpStatus);
            if (transport != DynamicApiOutcome.Success)
                return transport;
            return FromBilibiliCode(businessCode);
        }
    }

    /// <summary>
    /// 选择来源时的上下文快照。必须在 await 之前捕获,请求回来时才用它记录。
    /// 桶的键 = 操作 + 账号(或匿名)+ 登录 app key + 旧版 Android app key。
    /// </summary>
    public sealed class DynamicApiContext
    {
        public DynamicApiContext(long accountId, string loginAppKey, string legacyAndroidAppKey)
        {
            AccountId = accountId;
            LoginAppKey = loginAppKey ?? string.Empty;
            LegacyAndroidAppKey = legacyAndroidAppKey ?? string.Empty;
        }

        /// <summary>已登录用户 ID,未登录为 0</summary>
        public long AccountId { get; private set; }

        public bool IsAnonymous
        {
            get { return AccountId <= 0; }
        }

        public string LoginAppKey { get; private set; }

        public string LegacyAndroidAppKey { get; private set; }

        public static string BuildKey(DynamicApiOperation operation, long accountId, string loginAppKey, string legacyAndroidAppKey)
        {
            var account = accountId > 0
                ? "u" + accountId.ToString(CultureInfo.InvariantCulture)
                : "anon";
            return operation.ToString()
                + "|" + account
                + "|" + (loginAppKey ?? string.Empty)
                + "|" + (legacyAndroidAppKey ?? string.Empty);
        }

        public string BuildKey(DynamicApiOperation operation)
        {
            return BuildKey(operation, AccountId, LoginAppKey, LegacyAndroidAppKey);
        }
    }

    /// <summary>一条打分样本</summary>
    public sealed class DynamicApiSample
    {
        public DateTimeOffset At { get; set; }
        public bool Success { get; set; }
        /// <summary>请求到响应解码完成的毫秒数,不含 UI 渲染</summary>
        public double ElapsedMs { get; set; }
    }

    /// <summary>单个来源在单个桶里的历史与失败状态</summary>
    public sealed class DynamicApiSourceState
    {
        public DynamicApiSourceState()
        {
            Samples = new List<DynamicApiSample>();
            RecentAttemptIds = new List<long>();
        }

        public DynamicApiSource Source { get; set; }
        public List<DynamicApiSample> Samples { get; set; }
        /// <summary>最近记录过的尝试 ID,保证同一次尝试只记一条</summary>
        public List<long> RecentAttemptIds { get; set; }
        public DateTimeOffset? LastAttemptAt { get; set; }
        public DateTimeOffset? CooldownUntil { get; set; }
        /// <summary>连续被明确拒绝的次数,决定冷却梯子走到哪一级</summary>
        public int RejectionCount { get; set; }
        /// <summary>两分钟窗口内的连续传输类失败次数</summary>
        public int TransientFailureCount { get; set; }
        public DateTimeOffset? LastTransientFailureAt { get; set; }
        /// <summary>最近一次分类结果,含中性结果,供诊断</summary>
        public DynamicApiOutcome? LastOutcome { get; set; }
        public DateTimeOffset? LastOutcomeAt { get; set; }
        /// <summary>最近一次业务码,未知业务码保留下来供诊断</summary>
        public int LastBusinessCode { get; set; }
    }

    /// <summary>一个 操作+上下文 的桶</summary>
    public sealed class DynamicApiBucketState
    {
        public DynamicApiBucketState()
        {
            Grpc = new DynamicApiSourceState { Source = DynamicApiSource.Grpc };
            Legacy = new DynamicApiSourceState { Source = DynamicApiSource.Legacy };
        }

        public string Key { get; set; }
        public DynamicApiOperation Operation { get; set; }
        public long AccountId { get; set; }
        public string LoginAppKey { get; set; }
        public string LegacyAndroidAppKey { get; set; }
        public DynamicApiSource? PreferredSource { get; set; }
        public DynamicApiSourceState Grpc { get; set; }
        public DynamicApiSourceState Legacy { get; set; }
        public DateTimeOffset? LastAttemptAt { get; set; }
        /// <summary>最近一次被选择的时间,持久化时用于 LRU 淘汰</summary>
        public DateTimeOffset? LastUsedAt { get; set; }
        /// <summary>已预约的试用来源</summary>
        public DynamicApiSource? TrialReservedSource { get; set; }
        public DateTimeOffset? TrialReservedAt { get; set; }

        public DynamicApiSourceState StateFor(DynamicApiSource source)
        {
            return source == DynamicApiSource.Grpc ? Grpc : Legacy;
        }
    }

    /// <summary>
    /// 历史存储适配器。本批次只提供进程内实现,持久化在后续批次接到同一个接口后面。
    /// </summary>
    public interface IDynamicApiHistoryStore
    {
        DynamicApiBucketState Read(string bucketKey);
        void Write(DynamicApiBucketState bucket);
    }

    public sealed class InMemoryDynamicApiHistoryStore : IDynamicApiHistoryStore
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, DynamicApiBucketState> _buckets =
            new Dictionary<string, DynamicApiBucketState>(StringComparer.Ordinal);

        public DynamicApiBucketState Read(string bucketKey)
        {
            if (string.IsNullOrEmpty(bucketKey))
                return null;
            lock (_gate)
            {
                DynamicApiBucketState bucket;
                return _buckets.TryGetValue(bucketKey, out bucket) ? bucket : null;
            }
        }

        public void Write(DynamicApiBucketState bucket)
        {
            if (bucket == null || string.IsNullOrEmpty(bucket.Key))
                return;
            lock (_gate)
            {
                _buckets[bucket.Key] = bucket;
            }
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _buckets.Count;
                }
            }
        }
    }

    /// <summary>一次尝试:记录时必须落到 Choose 时捕获的那个上下文上</summary>
    public sealed class DynamicApiAttempt
    {
        internal DynamicApiAttempt(long id, DynamicApiContext context, DynamicApiOperation operation, DynamicApiSource source)
        {
            Id = id;
            Context = context;
            Operation = operation;
            Source = source;
            BucketKey = context.BuildKey(operation);
        }

        public long Id { get; private set; }
        public DynamicApiContext Context { get; private set; }
        public DynamicApiOperation Operation { get; private set; }
        public DynamicApiSource Source { get; private set; }
        public string BucketKey { get; private set; }
    }

    /// <summary>一次选择的结论</summary>
    public sealed class DynamicApiDecision
    {
        private readonly Func<DynamicApiSource, DynamicApiAttempt> _attemptFactory;

        internal DynamicApiDecision(
            DynamicApiSource source,
            DynamicApiDecisionReason reason,
            bool isRecoveryTrial,
            double grpcReliability,
            double legacyReliability,
            double? grpcWeightedLatencyMs,
            double? legacyWeightedLatencyMs,
            bool grpcInCooldown,
            bool legacyInCooldown,
            Func<DynamicApiSource, DynamicApiAttempt> attemptFactory)
        {
            Source = source;
            Reason = reason;
            IsRecoveryTrial = isRecoveryTrial;
            GrpcReliability = grpcReliability;
            LegacyReliability = legacyReliability;
            GrpcWeightedLatencyMs = grpcWeightedLatencyMs;
            LegacyWeightedLatencyMs = legacyWeightedLatencyMs;
            GrpcInCooldown = grpcInCooldown;
            LegacyInCooldown = legacyInCooldown;
            _attemptFactory = attemptFactory;
            Attempt = attemptFactory(source);
        }

        /// <summary>首选来源</summary>
        public DynamicApiSource Source { get; private set; }

        /// <summary>首选失败后最多再试这一路,一个逻辑请求里两路各试一次即止</summary>
        public DynamicApiSource FallbackSource
        {
            get { return Source == DynamicApiSource.Grpc ? DynamicApiSource.Legacy : DynamicApiSource.Grpc; }
        }

        public DynamicApiDecisionReason Reason { get; private set; }

        /// <summary>本次选择是否预占了一次恢复试用</summary>
        public bool IsRecoveryTrial { get; private set; }

        public double GrpcReliability { get; private set; }
        public double LegacyReliability { get; private set; }
        public double? GrpcWeightedLatencyMs { get; private set; }
        public double? LegacyWeightedLatencyMs { get; private set; }
        public bool GrpcInCooldown { get; private set; }
        public bool LegacyInCooldown { get; private set; }

        /// <summary>与 Source 绑定的尝试,拿去调 Record</summary>
        public DynamicApiAttempt Attempt { get; private set; }

        /// <summary>首选失败、改试备用来源时用这个拿到它的尝试</summary>
        public DynamicApiAttempt AttemptFor(DynamicApiSource source)
        {
            return _attemptFactory(source);
        }

        /// <summary>诊断日志用的一行摘要,不含任何凭据或条目内容</summary>
        public string Describe()
        {
            var builder = new StringBuilder();
            builder.Append(Source == DynamicApiSource.Grpc ? "grpc" : "legacy");
            builder.Append(" reason=").Append(Reason.ToString());
            builder.Append(" grpc=").Append(FormatScore(GrpcReliability));
            builder.Append(" legacy=").Append(FormatScore(LegacyReliability));
            if (GrpcWeightedLatencyMs.HasValue)
                builder.Append(" grpcMs=").Append(FormatScore(GrpcWeightedLatencyMs.Value));
            if (LegacyWeightedLatencyMs.HasValue)
                builder.Append(" legacyMs=").Append(FormatScore(LegacyWeightedLatencyMs.Value));
            builder.Append(" fallback=").Append(FallbackSource == DynamicApiSource.Grpc ? "grpc" : "legacy");
            builder.Append(" trial=").Append(IsRecoveryTrial ? "1" : "0");
            return builder.ToString();
        }

        private static string FormatScore(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 动态流来源选择器:按最近的成功率与延迟预测下一路该用哪套接口。
    /// 不依赖 UWP、网络、文件或 UI,时间与存储都从外面注入。
    /// </summary>
    public sealed class DynamicApiSelector
    {
        /// <summary>每个来源保留的打分样本条数</summary>
        public const int MaxSamplesPerSource = 8;

        /// <summary>每个来源保留的尝试 ID 条数,用于同一次尝试只记一次</summary>
        public const int MaxAttemptIdsPerSource = 16;

        /// <summary>挑战者的可靠性要比当前首选高出这么多才换源</summary>
        public const double ReliabilityAdvantageThreshold = 0.10;

        /// <summary>可靠性持平时,挑战者的加权延迟至少要低这么多比例才为速度换源</summary>
        public const double LatencyAdvantageRatio = 0.20;

        /// <summary>为速度换源前,两路各自至少要有这么多次成功测量</summary>
        public const int MinimumSuccessfulMeasurementsForLatencySwitch = 3;

        /// <summary>样本超过这个年龄就丢弃</summary>
        public static readonly TimeSpan SampleMaxAge = TimeSpan.FromHours(24);

        /// <summary>样本权重半衰期</summary>
        public static readonly TimeSpan SampleHalfLife = TimeSpan.FromMinutes(30);

        /// <summary>备用来源闲置这么久之后,可以在新的一屏边界做一次试用</summary>
        public static readonly TimeSpan RecoveryTrialInterval = TimeSpan.FromMinutes(5);

        /// <summary>两分钟内连续两次传输类失败就算连续失败</summary>
        public static readonly TimeSpan TransientFailureWindow = TimeSpan.FromMinutes(2);

        /// <summary>连续传输类失败的冷却时长</summary>
        public static readonly TimeSpan TransientFailureCooldown = TimeSpan.FromMinutes(1);

        /// <summary>连续被拒绝的冷却梯子:2 → 5 → 15 → 30 分钟</summary>
        public static readonly TimeSpan[] EndpointRejectionCooldownLadder =
        {
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
        };

        private readonly object _gate = new object();
        private readonly IDynamicApiHistoryStore _store;
        private readonly Func<DateTimeOffset> _clock;
        private readonly Dictionary<string, DynamicApiBucketState> _buckets =
            new Dictionary<string, DynamicApiBucketState>(StringComparer.Ordinal);
        private long _attemptCounter;

        public DynamicApiSelector(IDynamicApiHistoryStore store, Func<DateTimeOffset> clock)
        {
            _store = store ?? new InMemoryDynamicApiHistoryStore();
            _clock = clock ?? DefaultClock;
        }

        /// <summary>进程内历史,等价于挂一个内存存储</summary>
        public static DynamicApiSelector CreateInMemory()
        {
            return new DynamicApiSelector(null, null);
        }

        private static DateTimeOffset DefaultClock()
        {
            return DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 选择第一路来源。只有新的一屏/详情边界才应该调用它。
        /// </summary>
        public DynamicApiDecision Choose(DynamicApiContext context, DynamicApiOperation operation)
        {
            return Choose(context, operation, true);
        }

        /// <summary>
        /// 选择第一路来源。allowRecoveryTrial 为 false 时不做恢复试用预约。
        /// </summary>
        public DynamicApiDecision Choose(DynamicApiContext context, DynamicApiOperation operation, bool allowRecoveryTrial)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            lock (_gate)
            {
                var now = _clock();
                var bucket = LoadBucket(context, operation, now);
                bucket.LastUsedAt = now;
                PruneSamples(bucket.Grpc, now);
                PruneSamples(bucket.Legacy, now);

                var preferred = bucket.PreferredSource ?? DynamicApiSource.Grpc;
                var grpcCooling = InCooldown(bucket.Grpc, now);
                var legacyCooling = InCooldown(bucket.Legacy, now);
                var hasHistory = HasHistory(bucket, now);

                DynamicApiDecision decision;
                if (grpcCooling && legacyCooling)
                {
                    // 两路都在冷却:取冷却先结束的一路,不据此改偏好
                    var pick = bucket.Grpc.CooldownUntil.GetValueOrDefault() <= bucket.Legacy.CooldownUntil.GetValueOrDefault()
                        ? DynamicApiSource.Grpc
                        : DynamicApiSource.Legacy;
                    decision = BuildDecision(bucket, context, operation, pick, DynamicApiDecisionReason.BothSourcesInCooldown, false, now);
                }
                else if (grpcCooling || legacyCooling)
                {
                    var only = grpcCooling ? DynamicApiSource.Legacy : DynamicApiSource.Grpc;
                    var reason = only == preferred
                        ? (hasHistory ? DynamicApiDecisionReason.PreferenceRetained : DynamicApiDecisionReason.DefaultGrpcFirst)
                        : DynamicApiDecisionReason.PreferredSourceInCooldown;
                    //冷却只是"临时排除"这一路:不据此改写偏好,否则冷却一结束首选就再也回不到原来的来源,
                    //只能靠一次恢复试用才回来。偏好仍然只由可靠性/延迟判断改写。
                    decision = BuildDecision(bucket, context, operation, only, reason, false, now);
                }
                else if (hasHistory && allowRecoveryTrial && TryReserveTrial(bucket, Opposite(preferred), now))
                {
                    // 试用不动偏好:只是一次探测,结果由下一次 Choose 用可靠性判断
                    decision = BuildDecision(bucket, context, operation, Opposite(preferred), DynamicApiDecisionReason.RecoveryTrial, true, now);
                }
                else
                {
                    decision = ChooseByScore(bucket, context, operation, preferred, now);
                }

                _store.Write(bucket);
                return decision;
            }
        }

        private DynamicApiDecision ChooseByScore(DynamicApiBucketState bucket, DynamicApiContext context, DynamicApiOperation operation, DynamicApiSource preferred, DateTimeOffset now)
        {
            if (!HasHistory(bucket, now))
            {
                bucket.PreferredSource = DynamicApiSource.Grpc;
                return BuildDecision(bucket, context, operation, DynamicApiSource.Grpc, DynamicApiDecisionReason.DefaultGrpcFirst, false, now);
            }

            var challenger = Opposite(preferred);
            var preferredReliability = Reliability(bucket.StateFor(preferred), now);
            var challengerReliability = Reliability(bucket.StateFor(challenger), now);
            var reliabilityGap = challengerReliability - preferredReliability;

            if (reliabilityGap >= ReliabilityAdvantageThreshold)
            {
                bucket.PreferredSource = challenger;
                return BuildDecision(bucket, context, operation, challenger, DynamicApiDecisionReason.ReliabilityAdvantage, false, now);
            }

            // 可靠性差不多时只为速度换源,而且两边都要有足够的成功测量
            if (Math.Abs(reliabilityGap) < ReliabilityAdvantageThreshold && HasLatencyAdvantage(bucket, challenger, preferred, now))
            {
                bucket.PreferredSource = challenger;
                return BuildDecision(bucket, context, operation, challenger, DynamicApiDecisionReason.LatencyAdvantage, false, now);
            }

            bucket.PreferredSource = preferred;
            return BuildDecision(bucket, context, operation, preferred, DynamicApiDecisionReason.PreferenceRetained, false, now);
        }

        /// <summary>
        /// 为已固定的来源生成一次尝试。分页沿用初始加载选中的来源时用它:
        /// 不重新打分、不动偏好,但上下文仍在这一刻捕获(await 之前)。
        /// </summary>
        public DynamicApiAttempt CreateAttempt(DynamicApiContext context, DynamicApiOperation operation, DynamicApiSource source)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            return new DynamicApiAttempt(NextAttemptId(), context, operation, source);
        }

        private long NextAttemptId()
        {
            return System.Threading.Interlocked.Increment(ref _attemptCounter);
        }

        /// <summary>
        /// 记录一次尝试的结果。attempt 携带 Choose 时捕获的上下文,
        /// 所以中途换账号也不会落到别人的桶里。同一次尝试重复记录只算第一条。
        /// </summary>
        public void Record(DynamicApiAttempt attempt, DynamicApiOutcome outcome, TimeSpan elapsed)
        {
            Record(attempt, outcome, elapsed, 0);
        }

        /// <summary>
        /// 记录一次尝试的结果,businessCode 仅在需要留作诊断时传(未知业务码等)。
        /// </summary>
        public void Record(DynamicApiAttempt attempt, DynamicApiOutcome outcome, TimeSpan elapsed, int businessCode)
        {
            if (attempt == null)
                throw new ArgumentNullException("attempt");

            lock (_gate)
            {
                var now = _clock();
                var bucket = LoadBucket(attempt.Context, attempt.Operation, now);
                bucket.LastUsedAt = now;
                PruneSamples(bucket.Grpc, now);
                PruneSamples(bucket.Legacy, now);

                var state = bucket.StateFor(attempt.Source);
                if (state.RecentAttemptIds.Contains(attempt.Id))
                    return;
                state.RecentAttemptIds.Add(attempt.Id);
                if (state.RecentAttemptIds.Count > MaxAttemptIdsPerSource)
                    state.RecentAttemptIds.RemoveAt(0);

                state.LastAttemptAt = now;
                state.LastOutcome = outcome;
                state.LastOutcomeAt = now;
                if (businessCode != 0)
                    state.LastBusinessCode = businessCode;
                bucket.LastAttemptAt = now;
                if (bucket.TrialReservedSource.HasValue && bucket.TrialReservedSource.Value == attempt.Source)
                {
                    bucket.TrialReservedSource = null;
                    bucket.TrialReservedAt = null;
                }

                var elapsedMs = elapsed <= TimeSpan.Zero ? 0d : elapsed.TotalMilliseconds;
                if (DynamicApiOutcomes.CountsTowardAvailability(outcome))
                    AddSample(state, now, outcome == DynamicApiOutcome.Success, elapsedMs);

                switch (outcome)
                {
                    case DynamicApiOutcome.Success:
                        state.RejectionCount = 0;
                        state.TransientFailureCount = 0;
                        state.LastTransientFailureAt = null;
                        state.CooldownUntil = null;
                        break;
                    case DynamicApiOutcome.EndpointRejection:
                        state.RejectionCount++;
                        var ladderIndex = state.RejectionCount - 1;
                        if (ladderIndex >= EndpointRejectionCooldownLadder.Length)
                            ladderIndex = EndpointRejectionCooldownLadder.Length - 1;
                        ExtendCooldown(state, now + EndpointRejectionCooldownLadder[ladderIndex]);
                        break;
                    default:
                        if (DynamicApiOutcomes.IsTransientFailure(outcome))
                        {
                            state.TransientFailureCount = state.LastTransientFailureAt.HasValue
                                && now - state.LastTransientFailureAt.Value <= TransientFailureWindow
                                ? state.TransientFailureCount + 1
                                : 1;
                            state.LastTransientFailureAt = now;
                            if (state.TransientFailureCount >= 2)
                                ExtendCooldown(state, now + TransientFailureCooldown);
                        }
                        // 中性结果不碰连续失败与冷却
                        break;
                }

                _store.Write(bucket);
            }
        }

        private DynamicApiBucketState LoadBucket(DynamicApiContext context, DynamicApiOperation operation, DateTimeOffset now)
        {
            var key = context.BuildKey(operation);
            DynamicApiBucketState bucket;
            if (_buckets.TryGetValue(key, out bucket))
                return bucket;

            bucket = _store.Read(key);
            if (bucket == null)
            {
                bucket = new DynamicApiBucketState
                {
                    Key = key,
                    Operation = operation,
                    AccountId = context.AccountId,
                    LoginAppKey = context.LoginAppKey,
                    LegacyAndroidAppKey = context.LegacyAndroidAppKey,
                };
            }
            if (bucket.Grpc == null)
                bucket.Grpc = new DynamicApiSourceState { Source = DynamicApiSource.Grpc };
            if (bucket.Legacy == null)
                bucket.Legacy = new DynamicApiSourceState { Source = DynamicApiSource.Legacy };
            if (bucket.Grpc.Samples == null)
                bucket.Grpc.Samples = new List<DynamicApiSample>();
            if (bucket.Legacy.Samples == null)
                bucket.Legacy.Samples = new List<DynamicApiSample>();
            if (bucket.Grpc.RecentAttemptIds == null)
                bucket.Grpc.RecentAttemptIds = new List<long>();
            if (bucket.Legacy.RecentAttemptIds == null)
                bucket.Legacy.RecentAttemptIds = new List<long>();

            _buckets[key] = bucket;
            return bucket;
        }

        private DynamicApiDecision BuildDecision(
            DynamicApiBucketState bucket,
            DynamicApiContext context,
            DynamicApiOperation operation,
            DynamicApiSource source,
            DynamicApiDecisionReason reason,
            bool isRecoveryTrial,
            DateTimeOffset now)
        {
            return new DynamicApiDecision(
                source,
                reason,
                isRecoveryTrial,
                Reliability(bucket.Grpc, now),
                Reliability(bucket.Legacy, now),
                WeightedLatency(bucket.Grpc, now),
                WeightedLatency(bucket.Legacy, now),
                InCooldown(bucket.Grpc, now),
                InCooldown(bucket.Legacy, now),
                s => new DynamicApiAttempt(NextAttemptId(), context, operation, s));
        }

        private static DynamicApiSource Opposite(DynamicApiSource source)
        {
            return source == DynamicApiSource.Grpc ? DynamicApiSource.Legacy : DynamicApiSource.Grpc;
        }

        private static bool InCooldown(DynamicApiSourceState state, DateTimeOffset now)
        {
            return state.CooldownUntil.HasValue && now < state.CooldownUntil.Value;
        }

        private static void ExtendCooldown(DynamicApiSourceState state, DateTimeOffset until)
        {
            if (!state.CooldownUntil.HasValue || state.CooldownUntil.Value < until)
                state.CooldownUntil = until;
        }

        private static bool HasHistory(DynamicApiBucketState bucket, DateTimeOffset now)
        {
            return HasSamples(bucket.Grpc, now) || HasSamples(bucket.Legacy, now);
        }

        private static bool HasSamples(DynamicApiSourceState state, DateTimeOffset now)
        {
            for (var i = 0; i < state.Samples.Count; i++)
            {
                if (now - state.Samples[i].At <= SampleMaxAge)
                    return true;
            }
            return false;
        }

        private static void PruneSamples(DynamicApiSourceState state, DateTimeOffset now)
        {
            for (var i = state.Samples.Count - 1; i >= 0; i--)
            {
                if (now - state.Samples[i].At > SampleMaxAge)
                    state.Samples.RemoveAt(i);
            }
            if (state.Samples.Count > MaxSamplesPerSource)
                state.Samples.RemoveRange(0, state.Samples.Count - MaxSamplesPerSource);
        }

        private static void AddSample(DynamicApiSourceState state, DateTimeOffset now, bool success, double elapsedMs)
        {
            state.Samples.Add(new DynamicApiSample { At = now, Success = success, ElapsedMs = elapsedMs });
            if (state.Samples.Count > MaxSamplesPerSource)
                state.Samples.RemoveRange(0, state.Samples.Count - MaxSamplesPerSource);
        }

        /// <summary>可靠性 = (1 + 加权成功) / (2 + 加权尝试),30 分钟半衰,24 小时外的样本不算</summary>
        private static double Reliability(DynamicApiSourceState state, DateTimeOffset now)
        {
            double attempts = 0d;
            double successes = 0d;
            for (var i = 0; i < state.Samples.Count; i++)
            {
                var sample = state.Samples[i];
                var age = now - sample.At;
                if (age > SampleMaxAge)
                    continue;
                var weight = Weight(age);
                attempts += weight;
                if (sample.Success)
                    successes += weight;
            }
            return (1d + successes) / (2d + attempts);
        }

        /// <summary>只按成功样本算加权延迟,没有成功样本时为 null</summary>
        private static double? WeightedLatency(DynamicApiSourceState state, DateTimeOffset now)
        {
            double weightSum = 0d;
            double latencySum = 0d;
            for (var i = 0; i < state.Samples.Count; i++)
            {
                var sample = state.Samples[i];
                if (!sample.Success)
                    continue;
                var age = now - sample.At;
                if (age > SampleMaxAge)
                    continue;
                var weight = Weight(age);
                weightSum += weight;
                latencySum += weight * sample.ElapsedMs;
            }
            return weightSum > 0d ? latencySum / weightSum : (double?)null;
        }

        private static int SuccessfulMeasurements(DynamicApiSourceState state, DateTimeOffset now)
        {
            var count = 0;
            for (var i = 0; i < state.Samples.Count; i++)
            {
                if (state.Samples[i].Success && now - state.Samples[i].At <= SampleMaxAge)
                    count++;
            }
            return count;
        }

        private static double Weight(TimeSpan age)
        {
            if (age <= TimeSpan.Zero)
                return 1d;
            return Math.Pow(0.5d, age.TotalMilliseconds / SampleHalfLife.TotalMilliseconds);
        }

        private bool HasLatencyAdvantage(DynamicApiBucketState bucket, DynamicApiSource challenger, DynamicApiSource preferred, DateTimeOffset now)
        {
            var challengerState = bucket.StateFor(challenger);
            var preferredState = bucket.StateFor(preferred);
            if (SuccessfulMeasurements(challengerState, now) < MinimumSuccessfulMeasurementsForLatencySwitch)
                return false;
            if (SuccessfulMeasurements(preferredState, now) < MinimumSuccessfulMeasurementsForLatencySwitch)
                return false;
            var challengerLatency = WeightedLatency(challengerState, now);
            var preferredLatency = WeightedLatency(preferredState, now);
            if (!challengerLatency.HasValue || !preferredLatency.HasValue)
                return false;
            return challengerLatency.Value <= preferredLatency.Value * (1d - LatencyAdvantageRatio);
        }

        /// <summary>
        /// 恢复试用:备用来源闲置满五分钟、且不在冷却里,就给一次试用。
        /// 预约在锁内完成,并发页面拿不到第二次。
        /// </summary>
        private bool TryReserveTrial(DynamicApiBucketState bucket, DynamicApiSource source, DateTimeOffset now)
        {
            var state = bucket.StateFor(source);
            if (InCooldown(state, now))
                return false;
            if (bucket.TrialReservedSource.HasValue)
            {
                var stillActive = !bucket.TrialReservedAt.HasValue
                    || now - bucket.TrialReservedAt.Value < RecoveryTrialInterval;
                if (stillActive)
                    return false;
            }
            var reference = state.LastAttemptAt ?? bucket.LastAttemptAt;
            if (!reference.HasValue || now - reference.Value < RecoveryTrialInterval)
                return false;
            bucket.TrialReservedSource = source;
            bucket.TrialReservedAt = now;
            return true;
        }
    }
}
