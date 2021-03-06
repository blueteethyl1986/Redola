﻿using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logrila.Logging;
using Logrila.Logging.NLogIntegration;
using Redola.Rpc.TestContracts;

namespace Redola.Rpc.DynamicProxy.TestRpcClient
{
    class Program
    {
        static void Main(string[] args)
        {
            NLogLogger.Use();

            ILog log = Logger.Get<Program>();

            var localActor = new RpcActor();

            var helloClient = RpcServiceProxyGenerator.CreateServiceProxy<IHelloService>(localActor, "server");
            var calcClient = RpcServiceProxyGenerator.CreateServiceProxy<ICalcService>(localActor, "server");
            var orderClient = RpcServiceProxyGenerator.CreateServiceProxy<IOrderService>(localActor, "server");

            localActor.RegisterRpcService(helloClient as RpcService);
            localActor.RegisterRpcService(calcClient as RpcService);
            localActor.RegisterRpcService(orderClient as RpcService);

            localActor.Bootup();

            while (true)
            {
                try
                {
                    string text = Console.ReadLine().ToLowerInvariant().Trim();
                    if (text == "quit" || text == "exit")
                    {
                        break;
                    }
                    else if (text == "reconnect")
                    {
                        localActor.Shutdown();
                        localActor.Bootup();
                    }
                    else if (Regex.Match(text, @"^hello(\d*)$").Success)
                    {
                        var match = Regex.Match(text, @"^hello(\d*)$");
                        int totalCalls = 0;
                        if (!int.TryParse(match.Groups[1].Value, out totalCalls))
                        {
                            totalCalls = 1;
                        }
                        for (int i = 0; i < totalCalls; i++)
                        {
                            Hello(log, helloClient);
                        }
                    }
                    else if (Regex.Match(text, @"^add(\d*)$").Success)
                    {
                        var match = Regex.Match(text, @"^add(\d*)$");
                        int totalCalls = 0;
                        if (!int.TryParse(match.Groups[1].Value, out totalCalls))
                        {
                            totalCalls = 1;
                        }
                        for (int i = 0; i < totalCalls; i++)
                        {
                            Add(log, calcClient);
                        }
                    }
                    else if (Regex.Match(text, @"^order(\d*)$").Success)
                    {
                        var match = Regex.Match(text, @"order(\d*)$");
                        int totalCalls = 0;
                        if (!int.TryParse(match.Groups[1].Value, out totalCalls))
                        {
                            totalCalls = 1;
                        }
                        for (int i = 0; i < totalCalls; i++)
                        {
                            PlaceOrder(log, orderClient);
                        }
                    }
                    else if (Regex.Match(text, @"^hello(\d+)x(\d+)$").Success)
                    {
                        var match = Regex.Match(text, @"^hello(\d+)x(\d+)$");
                        int totalCalls = int.Parse(match.Groups[1].Value);
                        int threadCount = int.Parse(match.Groups[2].Value);
                        Hello10000MultiThreading(log, helloClient, totalCalls, threadCount);
                    }
                    else if (Regex.Match(text, @"^add(\d+)x(\d+)$").Success)
                    {
                        var match = Regex.Match(text, @"^add(\d+)x(\d+)$");
                        int totalCalls = int.Parse(match.Groups[1].Value);
                        int threadCount = int.Parse(match.Groups[2].Value);
                        Add10000MultiThreading(log, calcClient, totalCalls, threadCount);
                    }
                    else
                    {
                        log.WarnFormat("Cannot parse the operation for input [{0}].", text);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }

            localActor.Shutdown();
        }

        private static void Hello(ILog log, IHelloService helloClient)
        {
            var response = helloClient.Hello(new HelloRequest() { Text = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.fffffff") });

            log.DebugFormat("Hello, receive hello response from server with [{0}].", response.Text);
        }

        private static void Hello10000(ILog log, IHelloService helloClient)
        {
            log.DebugFormat("Hello10000, start ...");
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
            {
                helloClient.Hello10000(new Hello10000Request() { Text = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.fffffff") });
            }
            watch.Stop();
            log.DebugFormat("Hello10000, end with cost {0} ms.", watch.ElapsedMilliseconds);
        }

        private static void Hello10000MultiThreading(ILog log, IHelloService helloClient, int totalCalls, int threadCount)
        {
            log.DebugFormat("Hello10000MultiThreading, TotalCalls[{0}], ThreadCount[{1}], start ...", totalCalls, threadCount);

            var taskList = new Task[threadCount];
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < threadCount; i++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    for (var j = 0; j < totalCalls / threadCount; j++)
                    {
                        helloClient.Hello10000(new Hello10000Request() { Text = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.fffffff") });
                    }
                },
                TaskCreationOptions.PreferFairness);
                taskList[i] = task;
            }
            Task.WaitAll(taskList);
            watch.Stop();

            log.DebugFormat("Hello10000MultiThreading, TotalCalls[{0}], ThreadCount[{1}], end with cost [{2}] ms."
                + "{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}",
                totalCalls, threadCount, watch.ElapsedMilliseconds,
                Environment.NewLine, string.Format("   Concurrency level: {0} threads", threadCount),
                Environment.NewLine, string.Format("   Complete requests: {0}", totalCalls),
                Environment.NewLine, string.Format("Time taken for tests: {0} seconds", (decimal)watch.ElapsedMilliseconds / 1000m),
                Environment.NewLine, string.Format("    Time per request: {0:#####0.000} ms (avg)", (decimal)watch.ElapsedMilliseconds / (decimal)totalCalls),
                Environment.NewLine, string.Format(" Requests per second: {0} [#/sec] (avg)", (int)((decimal)totalCalls / ((decimal)watch.ElapsedMilliseconds / 1000m)))
                );
        }

        private static void Add(ILog log, ICalcService calcClient)
        {
            var response = calcClient.Add(new AddRequest() { X = 3, Y = 4 });

            log.DebugFormat("Add, receive add response from server with [{0}].", response.Result);
        }

        private static void Add10000MultiThreading(ILog log, ICalcService calcClient, int totalCalls, int threadCount)
        {
            log.DebugFormat("Add10000MultiThreading, TotalCalls[{0}], ThreadCount[{1}], start ...", totalCalls, threadCount);

            var taskList = new Task[threadCount];
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < threadCount; i++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    for (var j = 0; j < totalCalls / threadCount; j++)
                    {
                        calcClient.Add(new AddRequest() { X = 1, Y = 2 });
                    }
                },
                TaskCreationOptions.PreferFairness);
                taskList[i] = task;
            }
            Task.WaitAll(taskList);
            watch.Stop();

            log.DebugFormat("Add10000MultiThreading, TotalCalls[{0}], ThreadCount[{1}], end with cost [{2}] ms."
                + "{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}",
                totalCalls, threadCount, watch.ElapsedMilliseconds,
                Environment.NewLine, string.Format("   Concurrency level: {0} threads", threadCount),
                Environment.NewLine, string.Format("   Complete requests: {0}", totalCalls),
                Environment.NewLine, string.Format("Time taken for tests: {0} seconds", (decimal)watch.ElapsedMilliseconds / 1000m),
                Environment.NewLine, string.Format("    Time per request: {0:#####0.000} ms (avg)", (decimal)watch.ElapsedMilliseconds / (decimal)totalCalls),
                Environment.NewLine, string.Format(" Requests per second: {0} [#/sec] (avg)", (int)((decimal)totalCalls / ((decimal)watch.ElapsedMilliseconds / 1000m)))
                );
        }

        private static void PlaceOrder(ILog log, IOrderService orderClient)
        {
            var request = new PlaceOrderRequest()
            {
                Contract = new Order()
                {
                    OrderID = Guid.NewGuid().ToString(),
                    ItemID = "Apple",
                    BuyCount = 100,
                },
            };

            var response = orderClient.PlaceOrder(request);

            log.DebugFormat("PlaceOrder, receive place order response from server with [{0}].", response.ErrorCode);
        }
    }
}
