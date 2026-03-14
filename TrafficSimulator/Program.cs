using System.Diagnostics;

HttpClient httpClient = new() {BaseAddress = new Uri("http://localhost:8080")};
int rps = 10;
bool isRunning = true;

Console.WriteLine("Traffic Simulator is running");
Console.WriteLine($"Current RPS: {rps} (Normal)");
Console.WriteLine("Commands: 'u' (RPS 150), 'd' (RPS 10), 'q' (Quit)");

_ = Task.Run(() =>
{
	while (isRunning)
	{
		var input = Console.ReadLine()?.ToLower();

		switch (input)
		{
			case "u":
				rps = 150;
				Console.WriteLine("\n[!] SPARK: RPS set to 150");
				break;

			case "d":
				rps = 10;
				Console.WriteLine("\n[OK] NORMAL: RPS set to 10");
				break;

			case "q":
				isRunning = false;
				break;
		}
	}
});

while (isRunning)
{
	var stopwatch = Stopwatch.StartNew();

	var tasks = Enumerable
		.Range(0, rps)
		.Select(_ => SendRequest(httpClient));

	await Task.WhenAll(tasks);

	var elapsed = stopwatch.ElapsedMilliseconds;
	if (elapsed < 1000)
	{
		await Task.Delay(1000 - (int) elapsed);
	}

	Console.Write(".");
}

async Task SendRequest(HttpClient client)
{
	try
	{
		await client.PostAsync("/chaos/non-linear-latency", null);
	}
	catch
	{
		Console.Write("X");
	}
}