using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PollyTest
{
    public static class TestePolly
    {
        public static void Retry_FallBack_CircuitBreaker()
        {
            var apiTwoPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), onRetry: (exception, retryAttempt, retryCount, r) =>
                {
                    Console.WriteLine($"Retrying API Two {retryCount} time(s)");
                });

            var apiOnefallBackPolicy = Policy
                .Handle<HttpRequestException>()
                .Fallback(() =>
                {
                    Console.WriteLine($"Fallback for API One");
                    apiTwoPolicy.Execute(() => CallApiTwo());
                });

            var apiOnePolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), onRetry: (exception, retryAttempt, retryCount, r) =>
                {
                    Console.WriteLine($"Retrying API One {retryCount} time(s)");
                })
                .Wrap(Policy
                    .Handle<HttpRequestException>()
                .AdvancedCircuitBreaker(
                failureThreshold: 1,
                samplingDuration: TimeSpan.FromSeconds(100),
                minimumThroughput: 6,
                durationOfBreak: TimeSpan.FromSeconds(90),
                onBreak: (ex, timespan) =>
                {
                    Console.WriteLine("Circuit is Open");
                },
                onHalfOpen: () => Console.WriteLine("Circuit is half-open."),
                onReset: () => Console.WriteLine("Circuit is reset.")
                ));

            var apiOneCombinedPolicy = Policy.Wrap(apiOnefallBackPolicy, apiOnePolicy);

            // Simulating calls to API One
            for (int i = 0; i < 50; i++)
            {
                try
                {
                    var circuitState = apiOneCombinedPolicy.GetPolicy<CircuitBreakerPolicy>().CircuitState;
                    if (circuitState != CircuitState.Open && circuitState != CircuitState.Isolated)
                    {
                        apiOneCombinedPolicy.Execute(() =>
                        {
                            CallApiOne(circuitState);
                        });
                    }
                    else
                    {
                        apiTwoPolicy.Execute(() =>
                        {
                            CallApiTwo();
                        });
                    }  
                }
                catch (BrokenCircuitException)
                {
                    apiTwoPolicy.Execute(() =>
                    {
                        CallApiTwo();
                    });
                }
            }
        }

        public static void CallApiOne(CircuitState state)
        {
            // Simulating API One call
            Console.WriteLine($"API One Called{Environment.NewLine}");

            if (state == CircuitState.HalfOpen) { return;  }
            throw new HttpRequestException("Simulating API One failure.");
        }

        public static void CallApiTwo()
        {
            // Simulating API Two call
            Console.WriteLine($"API Two Called{Environment.NewLine}");

            bool success = true;
            Random rand = new Random();

            if (rand.Next(0, 2) != 0)
            {
                //success = false;
            }

            if(success ) { return; }
            throw new HttpRequestException("Simulating API Two failure.");
        }

    }
}
