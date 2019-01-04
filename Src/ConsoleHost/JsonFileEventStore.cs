﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using LiquidProjections.Abstractions;
using Newtonsoft.Json;

namespace LiquidProjections.ExampleHost
{
    public class JsonFileEventStore : IDisposable
    {
        private const int AverageEventsPerTransaction = 6;
        private readonly int pageSize;
        private ZipArchive zip;
        private readonly Queue<ZipArchiveEntry> entryQueue;
        private StreamReader currentReader = null;
        private static long lastCheckpoint = 0;

        public JsonFileEventStore(string filePath, int pageSize)
        {
            this.pageSize = pageSize;
            zip = ZipFile.Open(filePath, ZipArchiveMode.Read);
            entryQueue = new Queue<ZipArchiveEntry>(zip.Entries.Where(e => e.Name.EndsWith(".json")));
        }

        public IDisposable Subscribe(long? lastProcessedCheckpoint, LiquidProjections.Abstractions.Subscriber subscriber, string subscriptionId)
        {
            var innerSubscriber = new Subscriber(subscriptionId, lastProcessedCheckpoint ?? 0, subscriber.HandleTransactions);
            
            Task.Run(async () =>
            {
                Task<Transaction[]> loader = LoadNextPageAsync();
                Transaction[] transactions = await loader;

                while (transactions.Length > 0)
                {
                    // Start loading the next page on a separate thread while we have the subscriber handle the previous transactions.
                    loader = LoadNextPageAsync();

                    await innerSubscriber.Send(transactions);

                    transactions = await loader;
                }
            });

            return innerSubscriber;
        }

        private Task<Transaction[]> LoadNextPageAsync()
        {
            return Task.Run(() =>
            {
                var transactions = new List<Transaction>();

                var transaction = new Transaction
                {
                    Checkpoint = ++lastCheckpoint
                };

                string json;

                do
                {
                    json = CurrentReader.ReadLine();

                    if (json != null)
                    {
                        transaction.Events.Add(new EventEnvelope
                        {
                            Body = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All,
                                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
                            })
                        });
                    }

                    if ((transaction.Events.Count == AverageEventsPerTransaction) || (json == null))
                    {
                        if (transaction.Events.Count > 0)
                        {
                            transactions.Add(transaction);
                        }

                        transaction = new Transaction
                        {
                            Checkpoint = ++lastCheckpoint
                        };
                    }
                }
                while ((json != null) && (transactions.Count < pageSize));

                return transactions.ToArray();
            });
        }

        private StreamReader CurrentReader => 
            currentReader ?? (currentReader = new StreamReader(entryQueue.Dequeue().Open()));

        public void Dispose()
        {
            zip.Dispose();
            zip = null;
        }

        internal class Subscriber : IDisposable
        {
            private readonly string id;
            private readonly long fromCheckpoint;
            private readonly Func<IReadOnlyList<Transaction>, SubscriptionInfo, Task> handler;
            private bool disposed;

            public Subscriber(string id, long fromCheckpoint, Func<IReadOnlyList<Transaction>, SubscriptionInfo, Task> handler)
            {
                this.id = id;
                this.fromCheckpoint = fromCheckpoint;
                this.handler = handler;
            }

            public async Task Send(IEnumerable<Transaction> transactions)
            {
                if (!disposed)
                {
                    Transaction[] readOnlyList = transactions.Where(t => t.Checkpoint >= fromCheckpoint).ToArray();
                    if (readOnlyList.Length > 0)
                    {
                        await handler(readOnlyList, new SubscriptionInfo
                        {
                            Subscription = this,
                            Id = id
                        });
                    }
                }
                else
                {
                    throw new ObjectDisposedException("");
                }
            }

            public void Dispose()
            {
                disposed = true;
            }
        }
    }
}