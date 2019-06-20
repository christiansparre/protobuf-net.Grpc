﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.Grpc
{
    public readonly partial struct CallContext
    {
        /// <summary>
        /// Performs a full-duplex operation that will await both the producer and consumer streams
        /// </summary>
        public IAsyncEnumerable<TResponse> FullDuplexAsync<TRequest, TResponse>(
            IAsyncEnumerable<TRequest> source,
            Func<CallContext, CancellationToken, IAsyncEnumerable<TResponse>> producer,
            Func<IAsyncEnumerable<TRequest>, CallContext, Task> consumer)
        {
            return FullDuplexImpl(ConsumeAsWorkerAsync(source, consumer), producer, CancellationToken);
        }

        /// <summary>
        /// Performs a full-duplex operation that will await both the producer and consumer streams
        /// </summary>
        public IAsyncEnumerable<TResponse> FullDuplexAsync<TRequest, TResponse>(
            IAsyncEnumerable<TRequest> source,
            Func<CallContext, IAsyncEnumerable<TResponse>> producer,
            Func<IAsyncEnumerable<TRequest>, CallContext, Task> consumer)
        {
            return FullDuplexImpl(ConsumeAsWorkerAsync(source, consumer), producer, CancellationToken);
        }

        /// <summary>
        /// Performs a full-duplex operation that will await both the producer and consumer streams,
        /// performing an operation against each element in the inbound stream
        /// </summary>
        public IAsyncEnumerable<TResponse> FullDuplexAsync<TRequest, TResponse>(
            IAsyncEnumerable<TRequest> source,
            Func<CallContext, CancellationToken, IAsyncEnumerable<TResponse>> producer,
            Func<TRequest, CallContext, ValueTask> consumer)
        {
            return  FullDuplexImpl(ConsumeAsWorkerAsync(source, consumer), producer, CancellationToken);
        }

        /// <summary>
        /// Performs a full-duplex operation that will await both the producer and consumer streams,
        /// performing an operation against each element in the inbound stream
        /// </summary>
        public IAsyncEnumerable<TResponse> FullDuplexAsync<TRequest, TResponse>(
            IAsyncEnumerable<TRequest> source,
            Func<CallContext, IAsyncEnumerable<TResponse>> producer,
            Func<TRequest, CallContext, ValueTask> consumer)
        {
            return FullDuplexImpl(ConsumeAsWorkerAsync(source, consumer), producer, CancellationToken);
        }

        /// <summary>
        /// Performs an operation against each element in the inbound stream
        /// </summary>
        public async Task ConsumeAsync<TRequest>(IAsyncEnumerable<TRequest> source, Func<TRequest, CallContext, ValueTask> consumer)
        {
            await using (var iter = source.GetAsyncEnumerator(CancellationToken))
            {
                while (await iter.MoveNextAsync())
                {
                    await consumer(iter.Current, this);
                }
            }
        }

        /// <summary>
        /// Performs an aggregate operation against each element in the inbound stream
        /// </summary>
        public async Task<TValue> AggregateAsync<TRequest, TValue>(IAsyncEnumerable<TRequest> source,
            Func<TValue, TRequest, CallContext, ValueTask<TValue>> aggregate, TValue seed)
        {
            await using (var iter = source.GetAsyncEnumerator(CancellationToken))
            {
                while (await iter.MoveNextAsync())
                {
                    seed = await aggregate(seed, iter.Current, this);
                }
            }
            return seed;
        }

        private async IAsyncEnumerable<TResponse> FullDuplexImpl<TResponse>(
            Task consumed,
            Func<CallContext, IAsyncEnumerable<TResponse>> producer,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using (var iter = producer(this).GetAsyncEnumerator(cancellationToken))
            {
                while (await iter.MoveNextAsync())
                {
                    yield return iter.Current;
                }
            }
            await consumed;
        }

        private async IAsyncEnumerable<TResponse> FullDuplexImpl<TResponse>(
            Task consumed,
            Func<CallContext, CancellationToken, IAsyncEnumerable<TResponse>> producer,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using (var iter = producer(this, this.CancellationToken).GetAsyncEnumerator(cancellationToken))
            {
                while (await iter.MoveNextAsync())
                {
                    yield return iter.Current;
                }
            }
            await consumed;
        }

        private Task ConsumeAsWorkerAsync<TRequest>(IAsyncEnumerable<TRequest> source, Func<IAsyncEnumerable<TRequest>, CallContext, Task> consumer)
        {
            var ctx = this;
            return Task.Run(() => consumer(source, ctx), CancellationToken);
        }


        private Task ConsumeAsWorkerAsync<TRequest>(IAsyncEnumerable<TRequest> source, Func<TRequest, CallContext, ValueTask> consumer)
        {
            var ctx = this;
            return Task.Run(() => ctx.ConsumeAsync<TRequest>(source, consumer), CancellationToken);
        }
    }
}
