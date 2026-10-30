using System;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.gRPC;
using BiliLite.Services;

namespace Smart1Probe
{
    // SMART1 probe: compiles the real DynamicApiSelector.cs (and the real BiliLite.gRPC project)
    // and drives it with an injected clock, so every scenario is deterministic.
    internal static class Program
    {
        private static int _assertions;
        private static int _failures;
        private static long _attemptIds;

        private static int Main()
        {
            DefaultIsGrpcFirst();
            ReliabilityModel();
            ThresholdBoundaries();
            SpeedSwitch();
            SampleRetention();
            CooldownLadder();
            TransientCooldown();
            RecoveryTrial();
            TwoSourceCap();
            OutcomeClassification();
            NeutralOutcomesStayNeutral();
            ContextSeparation();
            CapturedContext();

            if (_failures > 0)
            {
                Console.WriteLine("FAILED " + _failures + " of " + _assertions + " assertions");
                return 1;
            }
            Console.WriteLine("PASS " + _assertions + " assertions");
            return 0;
        }

        // ---------------------------------------------------------------- cold start

        private static void DefaultIsGrpcFirst()
        {
            var env = new Env();
            var ctx = Ctx();
            var op = DynamicApiOperation.HomeVideoFeed;

            var decision = env.Selector.Choose(ctx, op);

            Check(decision.Source == DynamicApiSource.Grpc, "cold start: first source is gRPC");
            Check(decision.Reason == DynamicApiDecisionReason.DefaultGrpcFirst, "cold start: reason is DefaultGrpcFirst");
            Check(!decision.IsRecoveryTrial, "cold start: no recovery trial is reserved");
            Check(decision.FallbackSource == DynamicApiSource.Legacy, "cold start: legacy is exposed as the single fallback");
            Check(decision.Attempt.Source == DynamicApiSource.Grpc, "cold start: the attempt is bound to the chosen source");
            Check(decision.Attempt.Operation == op, "cold start: the attempt keeps the operation");
            CheckClose(0.5d, decision.GrpcReliability, 1e-9, "cold start: gRPC reliability is the 0.5 prior");
            CheckClose(0.5d, decision.LegacyReliability, 1e-9, "cold start: legacy reliability is the 0.5 prior");
            Check(decision.GrpcWeightedLatencyMs == null, "cold start: no latency before a success");
            Check(env.Bucket(ctx, op) != null, "cold start: the bucket is handed to the store");
            Check(env.Bucket(ctx, op).PreferredSource == DynamicApiSource.Grpc, "cold start: gRPC is stored as the preference");
        }

        // ---------------------------------------------------------------- scoring

        private static void ReliabilityModel()
        {
            var ctx = Ctx();
            var op = DynamicApiOperation.UserAllFeed;

            var env = new Env();
            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 120);
            var decision = env.Selector.Choose(ctx, op, false);
            CheckClose(2d / 3d, decision.GrpcReliability, 1e-9, "reliability: (1+1)/(2+1) after one success");
            CheckClose(120d, decision.GrpcWeightedLatencyMs ?? -1d, 1e-9, "latency: the recorded duration reaches the score");

            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            decision = env.Selector.Choose(ctx, op, false);
            CheckClose(2d / 4d, decision.GrpcReliability, 1e-9, "reliability: (1+1)/(2+2) after a failure");

            var half = new Env();
            half.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            half.Clock.Advance(TimeSpan.FromMinutes(30));
            decision = half.Selector.Choose(ctx, op, false);
            CheckClose(0.6d, decision.GrpcReliability, 1e-9, "half-life: a 30-minute-old sample counts half");
            CheckClose(100d, decision.GrpcWeightedLatencyMs ?? -1d, 1e-9, "half-life: latency weighting keeps the value");

            var older = new Env();
            older.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            older.Clock.Advance(TimeSpan.FromMinutes(90));
            decision = older.Selector.Choose(ctx, op, false);
            CheckClose(1.125d / 2.125d, decision.GrpcReliability, 1e-9, "half-life: 90 minutes quarters the sample weight");
        }

        private static void ThresholdBoundaries()
        {
            var ctx = Ctx();
            var op = DynamicApiOperation.UserVideoFeed;

            // 5/7 = 0.714 vs 6/7 = 0.857: a 0.143 gap must switch.
            var env = new Env();
            for (var i = 0; i < 4; i++)
                env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            for (var i = 0; i < 5; i++)
                env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.Success, 100);
            var decision = env.Selector.Choose(ctx, op, false);
            CheckClose(5d / 7d, decision.GrpcReliability, 1e-9, "threshold: gRPC reliability is 5/7");
            CheckClose(6d / 7d, decision.LegacyReliability, 1e-9, "threshold: legacy reliability is 6/7");
            Check(decision.Source == DynamicApiSource.Legacy, "threshold: a 0.143 reliability lead switches source");
            Check(decision.Reason == DynamicApiDecisionReason.ReliabilityAdvantage, "threshold: reason is ReliabilityAdvantage");

            // 3/5 = 0.600 vs 4/6 = 0.667: a 0.067 gap must keep gRPC.
            var close = new Env();
            close.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            close.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            close.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            close.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.Success, 100);
            close.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.Success, 100);
            close.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.Success, 100);
            close.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.TransportFailure, 0);
            decision = close.Selector.Choose(ctx, op, false);
            CheckClose(0.6d, decision.GrpcReliability, 1e-9, "threshold: gRPC reliability is 0.600");
            CheckClose(4d / 6d, decision.LegacyReliability, 1e-9, "threshold: legacy reliability is 0.667");
            CheckClose(1d / 15d, decision.LegacyReliability - decision.GrpcReliability, 1e-9, "threshold: the gap is 0.067");
            Check(decision.Source == DynamicApiSource.Grpc, "threshold: a 0.067 reliability lead keeps the preference");
            Check(decision.Reason == DynamicApiDecisionReason.PreferenceRetained, "threshold: reason is PreferenceRetained");
        }

        private static void SpeedSwitch()
        {
            var ctx = Ctx();
            var op = DynamicApiOperation.HomeVideoFeed;

            // Same reliability (4/5 both), legacy 30% faster: switch.
            var fast = new Env();
            Seed(env: fast, ctx: ctx, op: op, source: DynamicApiSource.Grpc, successes: 3, latencyMs: 100);
            Seed(env: fast, ctx: ctx, op: op, source: DynamicApiSource.Legacy, successes: 3, latencyMs: 70);
            var decision = fast.Selector.Choose(ctx, op, false);
            CheckClose(decision.GrpcReliability, decision.LegacyReliability, 1e-9, "speed: reliabilities are equal");
            Check(decision.Source == DynamicApiSource.Legacy, "speed: a 30% faster challenger wins at equal reliability");
            Check(decision.Reason == DynamicApiDecisionReason.LatencyAdvantage, "speed: reason is LatencyAdvantage");

            // Same reliability, legacy only 10% faster: keep gRPC.
            var slow = new Env();
            Seed(env: slow, ctx: ctx, op: op, source: DynamicApiSource.Grpc, successes: 3, latencyMs: 100);
            Seed(env: slow, ctx: ctx, op: op, source: DynamicApiSource.Legacy, successes: 3, latencyMs: 90);
            decision = slow.Selector.Choose(ctx, op, false);
            Check(decision.Source == DynamicApiSource.Grpc, "speed: a 10% latency lead is not enough");
            Check(decision.Reason == DynamicApiDecisionReason.PreferenceRetained, "speed: reason stays PreferenceRetained");

            // Faster challenger but only two successful measurements each: gate holds.
            var eager = new Env();
            Seed(env: eager, ctx: ctx, op: op, source: DynamicApiSource.Grpc, successes: 2, latencyMs: 100);
            Seed(env: eager, ctx: ctx, op: op, source: DynamicApiSource.Legacy, successes: 2, latencyMs: 40);
            decision = eager.Selector.Choose(ctx, op, false);
            Check(decision.Source == DynamicApiSource.Grpc, "speed: fewer than three successes per source blocks the switch");
            Check(decision.Reason == DynamicApiDecisionReason.PreferenceRetained, "speed: the gate keeps PreferenceRetained");

            // Exactly three successes but the challenger is only 19% faster: gate holds.
            var almost = new Env();
            Seed(env: almost, ctx: ctx, op: op, source: DynamicApiSource.Grpc, successes: 3, latencyMs: 100);
            Seed(env: almost, ctx: ctx, op: op, source: DynamicApiSource.Legacy, successes: 3, latencyMs: 81);
            decision = almost.Selector.Choose(ctx, op, false);
            Check(decision.Source == DynamicApiSource.Grpc, "speed: a 19% latency lead keeps the preference");
        }

        private static void SampleRetention()
        {
            var ctx = Ctx();

            var expired = new Env();
            var op = DynamicApiOperation.SpaceFeed;
            for (var i = 0; i < 8; i++)
                expired.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            Check(expired.Bucket(ctx, op).Grpc.Samples.Count == 8, "retention: eight failures are kept");
            var decision = expired.Selector.Choose(ctx, op, false);
            Check(decision.GrpcInCooldown, "retention: repeated failures cooled the source");
            expired.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1));
            decision = expired.Selector.Choose(ctx, op, false);
            Check(expired.Bucket(ctx, op).Grpc.Samples.Count == 0, "expiry: samples older than 24 hours are dropped");
            CheckClose(0.5d, decision.GrpcReliability, 1e-9, "expiry: an expired source falls back to the prior");
            Check(decision.Reason == DynamicApiDecisionReason.DefaultGrpcFirst, "expiry: expired history returns to the default order");
            Check(decision.Source == DynamicApiSource.Grpc, "expiry: the default order is gRPC first");

            var capped = new Env();
            var capOp = DynamicApiOperation.Reposts;
            for (var i = 0; i < 8; i++)
                capped.Record(ctx, capOp, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            capped.Record(ctx, capOp, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            var state = capped.Bucket(ctx, capOp).Grpc;
            Check(state.Samples.Count == 8, "cap: never more than eight samples per source");
            Check(state.Samples[7].Success == false, "cap: the newest sample survives");
            decision = capped.Selector.Choose(ctx, capOp, false);
            CheckClose(8d / 10d, decision.GrpcReliability, 1e-9, "cap: (1+7)/(2+8) proves the oldest success was evicted");
        }

        // ---------------------------------------------------------------- failures

        private static void CooldownLadder()
        {
            var env = new Env();
            var ctx = Ctx();
            var op = DynamicApiOperation.DynamicDetail;
            var ladder = new[] { 2d, 5d, 15d, 30d, 30d };

            for (var i = 0; i < ladder.Length; i++)
            {
                env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.EndpointRejection, 0);
                var state = env.Bucket(ctx, op).Legacy;
                CheckClose(ladder[i], MinutesUntil(state.CooldownUntil, env.Clock.Now), 1e-9,
                    "ladder: rejection #" + (i + 1) + " cools for " + ladder[i] + " minutes");
                Check(state.RejectionCount == i + 1, "ladder: rejection #" + (i + 1) + " is counted");

                var decision = env.Selector.Choose(ctx, op, false);
                Check(decision.LegacyInCooldown, "ladder: the decision reports the legacy cooldown");
                Check(decision.Source == DynamicApiSource.Grpc, "ladder: a cooling source is not selected");

                env.Clock.Advance(TimeSpan.FromMinutes(ladder[i]) + TimeSpan.FromSeconds(1));
                decision = env.Selector.Choose(ctx, op, false);
                Check(!decision.LegacyInCooldown, "ladder: the source returns after the cooldown");
            }

            env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.Success, 60);
            Check(env.Bucket(ctx, op).Legacy.CooldownUntil == null, "ladder: success clears the cooldown");
            Check(env.Bucket(ctx, op).Legacy.RejectionCount == 0, "ladder: success resets the ladder");
            env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.EndpointRejection, 0);
            CheckClose(2d, MinutesUntil(env.Bucket(ctx, op).Legacy.CooldownUntil, env.Clock.Now), 1e-9,
                "ladder: the next rejection after a success starts again at 2 minutes");

            var both = new Env();
            both.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.EndpointRejection, 0);
            both.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.EndpointRejection, 0);
            var blocked = both.Selector.Choose(ctx, op, false);
            Check(blocked.Reason == DynamicApiDecisionReason.BothSourcesInCooldown, "ladder: both cooling is reported");
            Check(blocked.GrpcInCooldown && blocked.LegacyInCooldown, "ladder: both sources are marked cooling");
            Check(blocked.FallbackSource != blocked.Source, "ladder: a blocked request still names one fallback");
        }

        private static void TransientCooldown()
        {
            var ctx = Ctx();
            var op = DynamicApiOperation.SpaceFeed;

            var env = new Env();
            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            Check(env.Bucket(ctx, op).Grpc.CooldownUntil == null, "transient: one failure does not cool the source");
            env.Clock.Advance(TimeSpan.FromMinutes(1));
            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.ServerError, 0);
            CheckClose(1d, MinutesUntil(env.Bucket(ctx, op).Grpc.CooldownUntil, env.Clock.Now), 1e-9,
                "transient: two failures within two minutes cool for one minute");
            env.Clock.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
            env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 90);
            Check(env.Bucket(ctx, op).Grpc.CooldownUntil == null, "transient: success clears the cooldown");
            Check(env.Bucket(ctx, op).Grpc.TransientFailureCount == 0, "transient: success clears the failure streak");

            var apart = new Env();
            apart.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            apart.Clock.Advance(TimeSpan.FromMinutes(3));
            apart.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Timeout, 0);
            Check(apart.Bucket(ctx, op).Grpc.CooldownUntil == null,
                "transient: failures three minutes apart do not trip the rule");

            var malformed = new Env();
            malformed.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.MalformedResponse, 0);
            malformed.Clock.Advance(TimeSpan.FromSeconds(30));
            malformed.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.MalformedResponse, 0);
            CheckClose(1d, MinutesUntil(malformed.Bucket(ctx, op).Legacy.CooldownUntil, malformed.Clock.Now), 1e-9,
                "transient: malformed responses count as transient failures");
        }

        // ---------------------------------------------------------------- recovery trials

        private static void RecoveryTrial()
        {
            var env = new Env();
            var ctx = Ctx();
            var op = DynamicApiOperation.UserAllFeed;

            var decision = env.Selector.Choose(ctx, op);
            Check(!decision.IsRecoveryTrial, "trial: a cold bucket does not probe an alternative");
            env.Selector.Record(decision.Attempt, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(50));

            env.Clock.Advance(TimeSpan.FromMinutes(4));
            decision = env.Selector.Choose(ctx, op);
            Check(!decision.IsRecoveryTrial, "trial: four minutes is not enough");
            Check(decision.Source == DynamicApiSource.Grpc, "trial: the preference still holds at four minutes");
            Check(decision.Reason == DynamicApiDecisionReason.PreferenceRetained, "trial: the reason is PreferenceRetained");

            env.Clock.Advance(TimeSpan.FromMinutes(1));
            decision = env.Selector.Choose(ctx, op);
            Check(decision.IsRecoveryTrial, "trial: five idle minutes make the alternative eligible");
            Check(decision.Source == DynamicApiSource.Legacy, "trial: the untried alternative is probed");
            Check(decision.Reason == DynamicApiDecisionReason.RecoveryTrial, "trial: reason is RecoveryTrial");
            var trialAttempt = decision.Attempt;

            decision = env.Selector.Choose(ctx, op);
            Check(!decision.IsRecoveryTrial, "trial: the reservation is taken exactly once");
            Check(decision.Source == DynamicApiSource.Grpc, "trial: a second boundary falls back to the preference");

            env.Selector.Record(trialAttempt, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(40));
            decision = env.Selector.Choose(ctx, op);
            Check(!decision.IsRecoveryTrial, "trial: a just-tried alternative is not probed again");

            env.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
            decision = env.Selector.Choose(ctx, op);
            Check(decision.IsRecoveryTrial, "trial: a fresh window allows another single trial");

            // Concurrent pages must not all probe the same window.
            var concurrent = new Env();
            var first = concurrent.Selector.Choose(ctx, op);
            concurrent.Selector.Record(first.Attempt, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(50));
            concurrent.Clock.Advance(TimeSpan.FromMinutes(6));
            var threads = 8;
            var tasks = new Task[threads];
            var decisions = new DynamicApiDecision[threads];
            var trials = 0;
            using (var gate = new ManualResetEventSlim(false))
            {
                for (var i = 0; i < threads; i++)
                {
                    var index = i;
                    tasks[i] = Task.Run(() =>
                    {
                        gate.Wait();
                        var chosen = concurrent.Selector.Choose(ctx, op);
                        decisions[index] = chosen;
                        if (chosen.IsRecoveryTrial)
                            Interlocked.Increment(ref trials);
                    });
                }
                gate.Set();
                Task.WaitAll(tasks);
            }
            Check(trials == 1, "trial: eight concurrent boundaries reserve exactly one trial (" + trials + ")");
            var trialSources = 0;
            for (var i = 0; i < decisions.Length; i++)
            {
                if (decisions[i] != null && decisions[i].IsRecoveryTrial)
                    trialSources++;
            }
            Check(trialSources == 1, "trial: only one of the concurrent decisions claims the trial");

            // A reservation that never gets recorded expires with the window.
            var stale = new Env();
            var start = stale.Selector.Choose(ctx, op);
            stale.Selector.Record(start.Attempt, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(50));
            stale.Clock.Advance(TimeSpan.FromMinutes(6));
            var reserved = stale.Selector.Choose(ctx, op);
            Check(reserved.IsRecoveryTrial, "trial: the window opens once");
            stale.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
            var reopened = stale.Selector.Choose(ctx, op);
            Check(reopened.IsRecoveryTrial, "trial: an unrecorded reservation expires instead of blocking forever");

            // A cooling alternative is never trialled.
            var cooling = new Env();
            var initial = cooling.Selector.Choose(ctx, op);
            cooling.Selector.Record(initial.Attempt, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(50));
            cooling.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.EndpointRejection, 0);
            cooling.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.EndpointRejection, 0);
            cooling.Clock.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));
            var stillCooling = cooling.Selector.Choose(ctx, op);
            Check(!stillCooling.IsRecoveryTrial, "trial: a source inside its 30-minute cooldown is not trialled");
            Check(stillCooling.LegacyInCooldown, "trial: the ladder cooldown is still active");
        }

        // ---------------------------------------------------------------- request shape

        private static void TwoSourceCap()
        {
            var env = new Env();
            var ctx = Ctx();
            var op = DynamicApiOperation.HomeVideoFeed;
            for (var i = 0; i < 8; i++)
                env.Record(ctx, op, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 100);
            for (var i = 0; i < 8; i++)
                env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.TransportFailure, 0);

            var decision = env.Selector.Choose(ctx, op, false);
            Check(decision.Source == DynamicApiSource.Grpc, "cap: the healthy source is chosen");
            Check(decision.FallbackSource == DynamicApiSource.Legacy,
                "cap: the alternative is offered once even with poor history");
            var fallbackAttempt = decision.AttemptFor(decision.FallbackSource);
            Check(fallbackAttempt.Source == DynamicApiSource.Legacy, "cap: the fallback attempt belongs to the fallback source");
            Check(fallbackAttempt.Id != decision.Attempt.Id, "cap: each attempt carries its own id");
            Check(decision.Source != decision.FallbackSource, "cap: a request never tries the same source twice");
        }

        // ---------------------------------------------------------------- classification

        private static void OutcomeClassification()
        {
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(0) == DynamicApiOutcome.Success, "classify: code 0 is a success");
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(-663) == DynamicApiOutcome.EndpointRejection,
                "classify: -663 is an explicit endpoint rejection");
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(-352) == DynamicApiOutcome.EndpointRejection,
                "classify: -352 is an explicit endpoint rejection");
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(-412) == DynamicApiOutcome.EndpointRejection,
                "classify: -412 is an explicit endpoint rejection");
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(-101) == DynamicApiOutcome.IgnoredFailure,
                "classify: login required is neutral");
            Check(DynamicApiOutcomeClassifier.FromBilibiliCode(-404) == DynamicApiOutcome.IgnoredFailure,
                "classify: a deleted item is neutral");
            var unknown = DynamicApiOutcomeClassifier.FromBilibiliCode(777001);
            Check(unknown == DynamicApiOutcome.UnknownBusinessError, "classify: an unknown code stays unknown");
            Check(!DynamicApiOutcomes.IsFailure(unknown), "classify: an unknown code is not a failure");
            Check(!DynamicApiOutcomes.CountsTowardAvailability(unknown),
                "classify: an unknown code does not lower availability");
            Check(!DynamicApiOutcomes.CountsTowardAvailability(DynamicApiOutcome.IgnoredFailure),
                "classify: a cancelled or superseded request does not lower availability");
            Check(DynamicApiOutcomes.IsFailure(DynamicApiOutcome.EndpointRejection)
                && DynamicApiOutcomes.IsFailure(DynamicApiOutcome.TransportFailure)
                && DynamicApiOutcomes.IsFailure(DynamicApiOutcome.Timeout)
                && DynamicApiOutcomes.IsFailure(DynamicApiOutcome.ServerError)
                && DynamicApiOutcomes.IsFailure(DynamicApiOutcome.MalformedResponse),
                "classify: every endpoint failure lowers availability");
            Check(DynamicApiOutcomes.IsTransientFailure(DynamicApiOutcome.ServerError)
                && DynamicApiOutcomes.IsTransientFailure(DynamicApiOutcome.Timeout)
                && !DynamicApiOutcomes.IsTransientFailure(DynamicApiOutcome.EndpointRejection),
                "classify: only transport-class failures feed the two-in-two-minutes rule");

            Check(DynamicApiOutcomeClassifier.FromLegacy(200, 0) == DynamicApiOutcome.Success, "legacy: 200 + code 0 is a success");
            Check(DynamicApiOutcomeClassifier.FromLegacy(200, -663) == DynamicApiOutcome.EndpointRejection,
                "legacy: a business rejection code is an endpoint rejection");
            Check(DynamicApiOutcomeClassifier.FromLegacy(503, 0) == DynamicApiOutcome.ServerError,
                "legacy: HTTP 5xx is a server error");
            Check(DynamicApiOutcomeClassifier.FromLegacy(429, 0) == DynamicApiOutcome.EndpointRejection,
                "legacy: HTTP 4xx is an endpoint rejection");
            Check(DynamicApiOutcomeClassifier.FromLegacy(200, -404) == DynamicApiOutcome.IgnoredFailure,
                "legacy: a missing item stays neutral");

            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult { status = true, message = "请求成功" })
                == DynamicApiOutcome.Success, "grpc: a valid envelope is a success");
            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult
            {
                status = false,
                failureKind = GrpcFailureKind.EndpointRejection,
                biliStatusCode = -663,
            }) == DynamicApiOutcome.EndpointRejection, "grpc: structured -663 metadata classifies without parsing text");
            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult { status = false, failureKind = GrpcFailureKind.Timeout })
                == DynamicApiOutcome.Timeout, "grpc: a timeout is classified as a timeout");
            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult { status = false, failureKind = GrpcFailureKind.Transport })
                == DynamicApiOutcome.TransportFailure, "grpc: a transport failure is classified as transport");
            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult { status = false, failureKind = GrpcFailureKind.HttpServerError })
                == DynamicApiOutcome.ServerError, "grpc: HTTP 5xx is classified as a server error");
            Check(DynamicApiOutcomeClassifier.FromGrpc(new GrpcResult { status = false, failureKind = GrpcFailureKind.MalformedResponse })
                == DynamicApiOutcome.MalformedResponse, "grpc: a malformed frame is classified as malformed");
            Check(DynamicApiOutcomeClassifier.FromGrpc(null) == DynamicApiOutcome.TransportFailure,
                "grpc: a missing result is a transport failure");

            var failure = new GrpcResult
            {
                status = false,
                code = -663,
                message = "请求失败,HTTP 200 OK,服务端拒绝: bili-status-code=-663,responseBytes=0",
                httpStatus = 200,
                biliStatusCode = -663,
                failureKind = GrpcFailureKind.EndpointRejection,
            };
            var typed = new GrpcRequestException(failure);
            Check(typed.Message == failure.message, "typed exception: the readable message is byte-identical");
            Check(typed.Result == failure, "typed exception: the original result is preserved");
            Check(typed.Code == -663, "typed exception: the code is preserved");
            Check(typed.HttpStatus == 200, "typed exception: the HTTP status is preserved");
            Check(typed.BiliStatusCode == -663, "typed exception: the Bilibili status is preserved");
            Check(typed.FailureKind == GrpcFailureKind.EndpointRejection, "typed exception: the failure kind is preserved");
            Check(DynamicApiOutcomeClassifier.FromGrpcException(typed) == DynamicApiOutcome.EndpointRejection,
                "typed exception: classification does not parse the message");
        }

        private static void NeutralOutcomesStayNeutral()
        {
            var env = new Env();
            var ctx = Ctx();
            var op = DynamicApiOperation.DynamicDetail;

            env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.UnknownBusinessError, 0, 777001);
            env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.UnknownBusinessError, 0, 777002);
            env.Record(ctx, op, DynamicApiSource.Legacy, DynamicApiOutcome.IgnoredFailure, 0);

            var state = env.Bucket(ctx, op).Legacy;
            Check(state.Samples.Count == 0, "neutral: unknown codes and cancellations add no scored sample");
            Check(state.CooldownUntil == null, "neutral: neutral outcomes never cool the source");
            Check(state.LastOutcome == DynamicApiOutcome.IgnoredFailure, "neutral: the last outcome is kept for diagnostics");
            Check(state.LastBusinessCode == 777002, "neutral: the unknown business code is kept for diagnostics");

            var decision = env.Selector.Choose(ctx, op, false);
            CheckClose(0.5d, decision.LegacyReliability, 1e-9, "neutral: availability is unchanged");
            Check(decision.Source == DynamicApiSource.Grpc, "neutral: the default order still holds");
            Check(decision.Reason == DynamicApiDecisionReason.DefaultGrpcFirst, "neutral: no history means gRPC first");
        }

        // ---------------------------------------------------------------- context

        private static void ContextSeparation()
        {
            var env = new Env();
            var account1 = new DynamicApiContext(1001, "login-a", "android-a");
            var account2 = new DynamicApiContext(1002, "login-a", "android-a");
            var anonymous = new DynamicApiContext(0, "login-a", "android-a");
            var otherLoginKey = new DynamicApiContext(1001, "login-b", "android-a");
            var otherLegacyKey = new DynamicApiContext(1001, "login-a", "android-b");

            env.Record(account1, DynamicApiOperation.HomeVideoFeed, DynamicApiSource.Grpc, DynamicApiOutcome.Success, 50);
            env.Record(account1, DynamicApiOperation.HomeVideoFeed, DynamicApiSource.Grpc, DynamicApiOutcome.TransportFailure, 0);
            Check(SampleCount(env, account1, DynamicApiOperation.HomeVideoFeed, DynamicApiSource.Grpc) == 2,
                "context: the account's own history is recorded");
            Check(env.Bucket(account2, DynamicApiOperation.HomeVideoFeed) == null,
                "context: another account gets its own bucket");
            Check(env.Bucket(anonymous, DynamicApiOperation.HomeVideoFeed) == null,
                "context: anonymous history is separate");
            Check(env.Bucket(otherLoginKey, DynamicApiOperation.HomeVideoFeed) == null,
                "context: the login app key separates buckets");
            Check(env.Bucket(otherLegacyKey, DynamicApiOperation.HomeVideoFeed) == null,
                "context: the legacy Android app key separates buckets");
            Check(env.Bucket(account1, DynamicApiOperation.UserVideoFeed) == null,
                "context: home and user video feeds share the RPC but not the history");
            Check(env.Bucket(account1, DynamicApiOperation.UserAllFeed) == null,
                "context: the user all feed has its own history");
            Check(env.Bucket(account1, DynamicApiOperation.DynamicDetail) == null,
                "context: detail has its own history");
            Check(env.Bucket(account1, DynamicApiOperation.SpaceFeed) == null,
                "context: the space feed has its own history");
            Check(env.Bucket(account1, DynamicApiOperation.Reposts) == null,
                "context: reposts have their own history");

            var other = env.Selector.Choose(account2, DynamicApiOperation.HomeVideoFeed, false);
            Check(other.Reason == DynamicApiDecisionReason.DefaultGrpcFirst,
                "context: a fresh bucket ignores the other account's history");
            CheckClose(0.5d, other.GrpcReliability, 1e-9, "context: a fresh bucket keeps the prior");

            Check(DynamicApiContext.BuildKey(DynamicApiOperation.HomeVideoFeed, 1001, "login-a", "android-a")
                != DynamicApiContext.BuildKey(DynamicApiOperation.UserVideoFeed, 1001, "login-a", "android-a"),
                "context: the bucket key contains the operation");
            Check(DynamicApiContext.BuildKey(DynamicApiOperation.HomeVideoFeed, 0, "login-a", "android-a")
                != DynamicApiContext.BuildKey(DynamicApiOperation.HomeVideoFeed, 1001, "login-a", "android-a"),
                "context: the bucket key contains the account state");
        }

        private static void CapturedContext()
        {
            var env = new Env();
            var account = new DynamicApiContext(1001, "login", "android");
            var decision = env.Selector.Choose(account, DynamicApiOperation.Reposts, false);
            var captured = decision.Attempt;

            // The signed-in account changes while the request is still in flight.
            var switched = new DynamicApiContext(2002, "login", "android");
            env.Selector.Record(decision.AttemptFor(DynamicApiSource.Legacy), DynamicApiOutcome.Success,
                TimeSpan.FromMilliseconds(40));
            env.Selector.Record(captured, DynamicApiOutcome.TransportFailure, TimeSpan.Zero);

            Check(SampleCount(env, account, DynamicApiOperation.Reposts, DynamicApiSource.Grpc) == 1,
                "captured context: the result lands in the bucket captured before awaiting");
            Check(SampleCount(env, account, DynamicApiOperation.Reposts, DynamicApiSource.Legacy) == 0,
                "captured context: the captured bucket stays free of the other source");
            Check(SampleCount(env, switched, DynamicApiOperation.Reposts, DynamicApiSource.Grpc) == 0,
                "captured context: the switched account's gRPC history stays untouched");
            Check(SampleCount(env, switched, DynamicApiOperation.Reposts, DynamicApiSource.Legacy) == 1,
                "captured context: the second request records into its own account");

            env.Selector.Record(captured, DynamicApiOutcome.Success, TimeSpan.FromMilliseconds(10));
            Check(SampleCount(env, account, DynamicApiOperation.Reposts, DynamicApiSource.Grpc) == 1,
                "captured context: one attempt is recorded exactly once");
            Check(env.Bucket(account, DynamicApiOperation.Reposts).Grpc.Samples[0].Success == false,
                "captured context: the first classified outcome wins");
        }

        // ---------------------------------------------------------------- helpers

        private static void Seed(Env env, DynamicApiContext ctx, DynamicApiOperation op, DynamicApiSource source,
            int successes, double latencyMs)
        {
            for (var i = 0; i < successes; i++)
                env.Record(ctx, op, source, DynamicApiOutcome.Success, latencyMs);
        }

        private static int SampleCount(Env env, DynamicApiContext ctx, DynamicApiOperation op, DynamicApiSource source)
        {
            var bucket = env.Bucket(ctx, op);
            if (bucket == null)
                return -1;
            var state = bucket.StateFor(source);
            return state == null ? -1 : state.Samples.Count;
        }

        private static double MinutesUntil(DateTimeOffset? until, DateTimeOffset now)
        {
            return until.HasValue ? (until.Value - now).TotalMinutes : double.NaN;
        }

        private static DynamicApiContext Ctx(long accountId = 1001, string loginAppKey = "login-appkey",
            string legacyAppKey = "android-appkey")
        {
            return new DynamicApiContext(accountId, loginAppKey, legacyAppKey);
        }

        private static void Check(bool condition, string label)
        {
            _assertions++;
            if (condition)
                return;
            _failures++;
            Console.WriteLine("FAIL " + label);
        }

        private static void CheckClose(double expected, double actual, double tolerance, string label)
        {
            _assertions++;
            if (!double.IsNaN(actual) && Math.Abs(expected - actual) <= tolerance)
                return;
            _failures++;
            Console.WriteLine("FAIL " + label + " (expected " + expected.ToString("0.##########") + ", actual "
                + actual.ToString("0.##########") + ")");
        }

        private sealed class TestClock
        {
            public DateTimeOffset Now = new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

            public DateTimeOffset Read()
            {
                return Now;
            }

            public void Advance(TimeSpan delta)
            {
                Now = Now.Add(delta);
            }
        }

        private sealed class Env
        {
            public readonly TestClock Clock = new TestClock();
            public readonly InMemoryDynamicApiHistoryStore Store = new InMemoryDynamicApiHistoryStore();
            public readonly DynamicApiSelector Selector;

            public Env()
            {
                Selector = new DynamicApiSelector(Store, Clock.Read);
            }

            public DynamicApiBucketState Bucket(DynamicApiContext ctx, DynamicApiOperation op)
            {
                return Store.Read(ctx.BuildKey(op));
            }

            public void Record(DynamicApiContext ctx, DynamicApiOperation op, DynamicApiSource source,
                DynamicApiOutcome outcome, double elapsedMs)
            {
                Record(ctx, op, source, outcome, elapsedMs, 0);
            }

            public void Record(DynamicApiContext ctx, DynamicApiOperation op, DynamicApiSource source,
                DynamicApiOutcome outcome, double elapsedMs, int businessCode)
            {
                Selector.Record(new DynamicApiAttempt(Interlocked.Increment(ref _attemptIds), ctx, op, source),
                    outcome, TimeSpan.FromMilliseconds(elapsedMs), businessCode);
            }
        }
    }
}
