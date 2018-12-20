using System;
using System.Threading;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Traces an Elasticsearch pipeline
    /// </summary>
    public static class ElasticsearchNetIntegration
    {
        private const string OperationName = "elasticsearch.query";
        private const string ServiceName = "elasticsearch";
        private const string SpanType = "elasticsearch";
        private const string ComponentKey = "component";
        private const string ComponentValue = "elasticsearch-net";
        private const string SpanKindKey = "span.kind";
        private const string SpanKindValue = "client";
        private const string ElasticsearchActionKey = "elasticsearch.action";
        private const string ElasticsearchMethodKey = "elasticsearch.method";
        private const string ElasticsearchUrlKey = "elasticsearch.url";
        private const string ElasticsearchParamsKey = "elasticsearch.params";

        private static readonly Type CancellationTokenType = typeof(CancellationToken);

        private static Type _requestDataType;

        /// <summary>
        /// Traces a synchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            CallerAssembly = "Elasticsearch.Net",
            TargetAssembly = "Elasticsearch.Net",
            TargetType = "Elasticsearch.Net.IRequestPipeline")]
        public static object CallElasticsearch<TResponse>(object pipeline, object requestData)
        {
            var originalMethod = DynamicMethodBuilder<Func<object, object, TResponse>>
               .GetOrCreateMethodCallDelegate(
                    pipeline.GetType(),
                    "CallElasticsearch",
                    methodGenericArguments: new[] { typeof(TResponse) });

            using (var scope = CreateScope(pipeline, requestData))
            {
                try
                {
                    return originalMethod(pipeline, requestData);
                }
                catch (Exception ex)
                {
                    scope.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Traces an asynchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="TResponse">Type type of the response</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationTokenSource">A cancellation token</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            CallerAssembly = "Elasticsearch.Net",
            TargetAssembly = "Elasticsearch.Net",
            TargetType = "Elasticsearch.Net.IRequestPipeline")]
        public static object CallElasticsearchAsync<TResponse>(object pipeline, object requestData, object cancellationTokenSource)
        {
            var cancellationToken = ((CancellationTokenSource)cancellationTokenSource)?.Token ?? CancellationToken.None;
            return CallElasticsearchAsyncInternal<TResponse>(pipeline, requestData, cancellationToken);
        }

        /// <summary>
        /// Traces an asynchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="TResponse">Type type of the response</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The original result</returns>
        private static async Task<TResponse> CallElasticsearchAsyncInternal<TResponse>(object pipeline, object requestData, CancellationToken cancellationToken)
        {
            if (_requestDataType == null)
            {
                _requestDataType = requestData.GetType();
            }

            var originalMethod = DynamicMethodBuilder<Func<object, object, CancellationToken, Task<TResponse>>>
               .GetOrCreateMethodCallDelegate(
                    pipeline.GetType(),
                    "CallElasticsearchAsync",
                    methodParameterTypes: new[] { _requestDataType, CancellationTokenType },
                    methodGenericArguments: new[] { typeof(TResponse) });

            using (var scope = CreateScope(pipeline, requestData))
            {
                try
                {
                    return await originalMethod(pipeline, requestData, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope.Span.SetException(ex);
                    throw;
                }
            }
        }

        private static Scope CreateScope(object pipeline, dynamic requestData)
        {
            string requestName = null;
            try
            {
                var requestParameters = DynamicMethodBuilder<Func<object, object>>
                   .GetOrCreateMethodCallDelegate(
                        pipeline.GetType(),
                        "get_RequestParameters")(pipeline);

                requestName = requestParameters?.GetType().Name.Replace("RequestParameters", string.Empty);
            }
            catch
            {
            }

            string pathAndQuery = null;
            try
            {
                pathAndQuery = requestData?.PathAndQuery;
            }
            catch
            {
            }

            string method = null;
            try
            {
                method = requestData?.Method?.ToString();
            }
            catch
            {
            }

            string url = null;
            try
            {
                url = requestData?.Uri.ToString();
            }
            catch
            {
            }

            var serviceName = string.Join("-", Tracer.Instance.DefaultServiceName, ServiceName);

            var scope = Tracer.Instance.StartActive(OperationName, serviceName: serviceName);
            scope.Span.ResourceName = requestName ?? pathAndQuery ?? string.Empty;
            scope.Span.Type = SpanType;
            scope.Span.SetTag(ComponentKey, ComponentValue);
            scope.Span.SetTag(SpanKindKey, SpanKindValue);
            scope.Span.SetTag(ElasticsearchActionKey, requestName);
            scope.Span.SetTag(ElasticsearchMethodKey, method);
            scope.Span.SetTag(ElasticsearchUrlKey, url);

            return scope;
        }
    }
}
