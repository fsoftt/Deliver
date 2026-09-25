using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Deliver.ServiceDefaults.Observability;

/// <summary>
/// The outbox processor polls the database every second. Without this sampler each poll would become its own
/// trace and bury the traces of real business flows. Client spans (database, HTTP) are kept only when they are
/// part of a flow, i.e. when they have a parent; everything else is sampled normally.
/// </summary>
internal sealed class DropBackgroundNoiseSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
        samplingParameters.Kind == ActivityKind.Client && samplingParameters.ParentContext.TraceId == default
            ? new SamplingResult(SamplingDecision.Drop)
            : new SamplingResult(SamplingDecision.RecordAndSample);
}
