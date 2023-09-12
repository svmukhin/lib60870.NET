using lib60870;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace cs104_client_session
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Using lib60870.NET version " + LibraryCommon.GetLibraryVersionString());

            var client = new Cs104Client();
            await client.ConnectAsync("127.0.0.1", 2404);

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (Exception) { }
        }
    }
}