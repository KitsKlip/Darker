using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Decorators;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker.QueryLogging
{
    public class QueryLoggingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(QueryLoggingDecorator<,>));

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.InfoFormat("Executing query {QueryName}: {Query}", queryName, GetSerializer().Serialize(query));

            var result = next(query);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.InfoFormat("Execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.InfoFormat("Executing async query {QueryName}: {Query}", queryName, GetSerializer().Serialize(query));

            var result = await next(query, cancellationToken).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.InfoFormat("Async execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        private NewtonsftJsonSerializer GetSerializer()
        {
            if (!Context.Bag.ContainsKey(Constants.ContextBagKey))
                throw new ConfigurationException($"Serializer does not exist in context bag with key {Constants.ContextBagKey}.");

            var serializer = Context.Bag[Constants.ContextBagKey] as NewtonsftJsonSerializer;
            if (serializer == null)
                throw new ConfigurationException($"The serializer in the context bag (with key {Constants.ContextBagKey}) must be of type {nameof(NewtonsftJsonSerializer)}, but is {Context.Bag[Constants.ContextBagKey].GetType()}.");

            return serializer;
        }
    }
}