using System;
using System.Collections.Concurrent;
using System.Text;
using DTO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Receiver.DeSerialization;

public class RpcClient
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly IBasicProperties props;
    private DateTime LastUpDate { get; set; }
    public RpcClient()
    {
        //var factory = new ConnectionFactory() { HostName = "localhost" };//192.168.88.36
        var factory = new ConnectionFactory() { UserName = "slavik", Password = "slavik", HostName = "localhost" };//192.168.88.36//193.254.196.48

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        replyQueueName = channel.QueueDeclare().QueueName;
        consumer = new EventingBasicConsumer(channel);

        props = channel.CreateBasicProperties();
        props.ReplyTo = replyQueueName;

        consumer.Received += (model, ea) =>
        {
            LastUpDate = DateTime.UtcNow;
            switch (ea.BasicProperties.CorrelationId)
            {
                case ObjectCategory.Message:
                    Message mess = ea.Body.Deserializer<Message>();
                    Console.WriteLine(mess.UserName + ": " + mess.UserMessage);
                    break;
                case ObjectCategory.StatusInfo:
                    PingAnswer();
                    return;
                    break;
                default:
                    break;
            }
        };
    }
    private void PingAnswer()
    {
        StatusInfo ping = new StatusInfo();
        IBasicProperties proper = channel.CreateBasicProperties();
        proper.ReplyTo = replyQueueName;
        proper.CorrelationId = ObjectCategory.StatusInfo;
        channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: proper, body: ping.Serializer());
    }
    public void Call(string name ,string message)
    {
        Message mess = new Message()
        {
            UserMessage = message,
            UserName = name
        };
        IBasicProperties proper = channel.CreateBasicProperties();
        proper.CorrelationId = ObjectCategory.Message;
        proper.ReplyTo = replyQueueName;
        channel.BasicPublish( exchange: "", routingKey: "rpc_queue", basicProperties: proper, body: mess.Serializer());
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
        while (true)
        {
            phrase = Console.ReadLine();
            rpcClient.Call(UserName, phrase);
            phrase = string.Empty;
        }
        rpcClient.Close();
    }
}