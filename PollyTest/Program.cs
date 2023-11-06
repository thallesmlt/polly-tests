using System;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using static System.Net.Mime.MediaTypeNames;
using PollyTest;

class Program
{
    static void Main()
    {
        TestePolly.Retry_FallBack_CircuitBreaker();
    }   
}