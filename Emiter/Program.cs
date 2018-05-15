using System;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RpcClient
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly IBasicProperties props;

    public RpcClient()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        replyQueueName = channel.QueueDeclare().QueueName;
        consumer = new EventingBasicConsumer(channel);

        props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

        consumer.Received += (model, ea) =>
        {
            Console.WriteLine(Encoding.UTF8.GetString(ea.Body));
        };
    }

    public void Call(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish( exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);
        channel.BasicConsume( consumer: consumer, queue: replyQueueName, autoAck: true);
    }

    public void Close()
    {
        connection.Close();
    }
}

public class Rpc
{
    public static void Main()
    {
        var rpcClient = new RpcClient();

        string UserName = "";
        Console.WriteLine("Please enter your name:");
        UserName = Console.ReadLine();
        Console.WriteLine($"Hi, {UserName}!");
        string phrase;
        rpcClient.Call("ping");
        while (true)
        {
            phrase = Console.ReadLine();
            rpcClient.Call($"{UserName} say: " + phrase);
            phrase = string.Empty;
        }
        rpcClient.Close();
    }
}