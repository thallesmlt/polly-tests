using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace PollyTest
{
    public static class TestePolly
    {
        public static void Retry_FallBack_CircuitBreaker()
        {
            var apiTwoPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), onRetry: (exception, retryAttempt, retryCount, r) =>
                {
                    Console.WriteLine($"Retrying API Two {retryCount} time(s)");
                });

            var apiOnefallBackPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<BrokenCircuitException>()
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
                samplingDuration: TimeSpan.FromSeconds(90),
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
        }

        public static void CallApiOne(CircuitState state)
        {
            // Simulating API One call
            Console.WriteLine($"API One Called{Environment.NewLine}");

            //Force CircuitBreaker returns to Closed State when circuit isHhalfOpen
            //Comment the if statement to keep the CircuitBreaker in the OpenState
            if (state == CircuitState.HalfOpen)
                return;

            throw new HttpRequestException("Simulating API One failure.");
        }

        public static void CallApiTwo()
        {
            // Simulating API Two call
            Console.WriteLine($"API Two Called{Environment.NewLine}");

            //50% of throw or not and exception. The ideia is to test the retry policy of CallApiTwo()
            Random rand = new Random();
            if (rand.Next(0, 2) != 0)
                return;

            throw new HttpRequestException("Simulating API Two failure.");
        }
    }
}
