#if NET45

using System;
using System.Collections.Generic;
using System.Threading;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// AspNetWeb5Integration wraps the Web API.
    /// </summary>
    public static class AspNetWebApi2Integration
    {
        internal const string OperationName = "aspnet-web-api.request";

        private static readonly Type HttpControllerContextType = Type.GetType("System.Web.Http.Controllers.HttpControllerContext, System.Web.Http", throwOnError: false);

        /// <summary>
        /// ExecuteAsync calls the underlying ExecuteAsync and traces the request.
        /// </summary>
        /// <param name="this">The Api Controller</param>
        /// <param name="controllerContext">The controller context for the call</param>
        /// <param name="cancellationTokenSource">The cancellation token source</param>
        /// <returns>A task with the result</returns>
        public static object ExecuteAsync(object @this, object controllerContext, object cancellationTokenSource)
        {
            Type controllerType = @this.GetType();

            // in some cases, ExecuteAsync() is an explicit interface implementation,
            // which is not public and has a different name, so try both
            var executeAsyncFunc =
                DynamicMethodBuilder<Func<object, object, CancellationToken, object>>
                   .GetOrCreateMethodCallDelegate(controllerType, "ExecuteAsync") ??
                DynamicMethodBuilder<Func<object, object, CancellationToken, object>>
                   .GetOrCreateMethodCallDelegate(controllerType, "System.Web.Http.Controllers.IHttpController.ExecuteAsync");

            using (Scope scope = CreateScope(controllerContext))
            {
                return scope.Span.Trace(
                    () =>
                    {
                        CancellationToken cancellationToken = ((CancellationTokenSource)cancellationTokenSource).Token;
                        return executeAsyncFunc(@this, controllerContext, cancellationToken);
                    },
                    onComplete: e =>
                    {
                        if (e != null)
                        {
                            scope.Span.SetException(e);
                        }

                        // some fields aren't set till after execution, so repopulate anything missing
                        UpdateSpan(controllerContext, scope.Span);
                        scope.Span.Finish();
                    });
            }
        }

        private static Scope CreateScope(object controllerContext)
        {
            var scope = Tracer.Instance.StartActive(OperationName, finishOnClose: false);
            UpdateSpan(controllerContext, scope.Span);
            return scope;
        }

        private static void UpdateSpan(object controllerContext, Span span)
        {
            var request = controllerContext.GetProperty("Request");
            var headers = request.GetProperty("Headers");
            var host = headers.GetProperty("Host") as string;
            var requestUri = request.GetProperty("RequestUri") as Uri;
            var rawUrl = requestUri?.ToString().ToLowerInvariant();
            var path = requestUri?.AbsolutePath.ToLowerInvariant();
            var method = (request.GetProperty("Method").GetProperty("Method") as string)?.ToUpperInvariant() ?? "GET";
            var routeData = controllerContext.GetProperty("RouteData");
            var route = routeData.GetProperty("Route");
            var routeTemplate = route.GetProperty("RouteTemplate") as string;
            var routeValues = routeData.GetProperty("Values") as IDictionary<string, object>;
            var controller = (routeValues?.GetValueOrDefault("controller") as string)?.ToLowerInvariant();
            var action = (routeValues?.GetValueOrDefault("action") as string)?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(routeTemplate))
            {
                span.ResourceName = $"{method} {path}";
            }
            else
            {
                routeTemplate = routeTemplate.Replace("{controller}", controller).Replace("{action}", action);
                span.ResourceName = $"{method} {routeTemplate}";
            }

            span.Type = SpanTypes.Web;
            span.SetTag(Tags.AspNetAction, action);
            span.SetTag(Tags.AspNetController, controller);
            span.SetTag(Tags.AspNetRoute, routeTemplate);
            span.SetTag(Tags.HttpMethod, method);
            span.SetTag(Tags.HttpRequestHeadersHost, host);
            span.SetTag(Tags.HttpUrl, rawUrl);
        }
    }
}

#endif
