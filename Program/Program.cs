using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using System.Runtime;


class Program
{
    static async Task Main(string[] args)
    {
      
        Task.Delay(5000);
        Task task_1 = Task.Run(() => task1());
        Task task_4 = Task.Run(() => SendDataAsync());
        await Task.WhenAll(task_4, task_1);
    }

    public static async Task task1()
    {

        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("test.mosquitto.org", 1883)
                                                              .WithCleanSession()
                                                              .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        Console.WriteLine("Connect successful");

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                                            .WithTopicFilter(f =>
                                            {
                                                f.WithTopic("/ABCD/data");
                                                f.WithAtLeastOnceQoS();
                                            }).Build();
        await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        Console.WriteLine("Subscribe topic: " + "/ABCD/data");

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            Console.WriteLine("Topic /ABCD/data: " + Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
        };

        await Task.Delay(Timeout.Infinite);
    }
    public static async Task SendDataAsync()
{
    var mqttFactory = new MqttFactory();
    var mqttClient = mqttFactory.CreateMqttClient();

    var mqttClientOptions = new MqttClientOptionsBuilder()
        .WithTcpServer("test.mosquitto.org", 1883)
        .WithCleanSession()
        .Build();

    await mqttClient.ConnectAsync(mqttClientOptions);
    Console.WriteLine("Connect successful");

    int packetCount = 1;
    DateTime start = DateTime.Now;
    while (true)
    {
        var dataPacket = new DataPacket
        {
            name = "STREAMING_DATA",
            packetNumber = packetCount,
            data = await GenerateDataEntries(start)
        };

        string json = JsonConvert.SerializeObject(dataPacket);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("/ABCD/data")
            .WithPayload(json)
            .Build();

        await mqttClient.PublishAsync(message);
        Console.WriteLine($"Sent packet {packetCount}");

        packetCount++;
        //await Task.Delay(100); // Delay to control sending rate

    }
}

   
private static async Task<List<DataEntry>> GenerateDataEntries(DateTime start)
{
    var dataEntries = new List<DataEntry>();

    for (int i = 0; i < 50; i++)
    {
        TimeSpan time = DateTime.Now - start;
        dataEntries.Add(new DataEntry
        {
            timestamp = (int)time.TotalMilliseconds,
            accX = Random.NextDouble() * 0.01,
            accY = Random.NextDouble() * 0.01,
            accZ = 1.0 + Random.NextDouble() * 0.02 - 0.01,
            force = Random.NextDouble() * 1000 - 500
        });
        await Task.Delay(20);
    }

    return dataEntries;
}

private static readonly Random Random = new Random();

    public class DataPacket
    {
        public string name { get; set; }
        public int packetNumber { get; set; }
        public List<DataEntry> data { get; set; }
    }

    public class DataEntry
    {
        public int timestamp { get; set; }
        public double accX { get; set; }
        public double accY { get; set; }
        public double accZ { get; set; }
        public double force { get; set; }
    }
}
