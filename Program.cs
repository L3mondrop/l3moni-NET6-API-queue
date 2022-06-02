using Microsoft.OpenApi.Models;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
     c.SwaggerDoc("v1", new OpenApiInfo { 
         Title = "l3moni queue API", 
         Description = "This API sends a item to a Storage queue", 
         Version = "v1",
         Contact = new OpenApiContact
         {
             Name = "Mikko Kasanen",
             Url = new Uri("https://lemoni.cloud"),
             Email = "mikko.kasanen@microsoft.com"
         },
         });
});
    
var app = builder.Build();
    
if (app.Environment.IsDevelopment())
{
     app.UseDeveloperExceptionPage();
}
    
app.UseSwagger();
app.UseSwaggerUI(c =>
{
   c.SwaggerEndpoint("/swagger/v1/swagger.json", "l3moni-queue API V1");
   c.RoutePrefix = string.Empty; // Adds a swagger route at the root of the application
});

var connectionString = "DefaultEndpointsProtocol=https;AccountName=l3moniapimstorage;AccountKey=hrDbXc/BC8+4JbR7x5zVYFSYKCvqrtO+RoKozPM2A+hdwEYdYLKun4u0T3WKPpG8jHZaUOaMyj4fJMA39/QlYg==;EndpointSuffix=core.windows.net";
var queuename = "l3moni-queue";

app.MapPost("/createaqueue/{newqueuename}",(string newqueuename) => {
    var queueClient = new QueueClient(connectionString, newqueuename);
    queueClient.CreateIfNotExists();
    return "Queue created";
}).WithName("Create a queue");

app.MapPost("/sendmessage/{queuename}/{message}",(string queuename, string message) => {
    var queueClient = new QueueClient(connectionString, queuename);
    queueClient.SendMessage(message);
    // Logic to insert the message into webmethods
    // IF webmethods replied 200 then -> "Message was handled"
    // ELSE webmethods replied 500 -> return "Error with ingestion, your message has been stored and will be retried"
    return "Message sent";
}).WithName("Send a message to Queue");

app.MapPost("/peekitem/{queuename}",(string queuename) => {
    var queueClient = new QueueClient(connectionString, queuename);
    PeekedMessage message = queueClient.PeekMessage();
    Console.WriteLine(message.MessageText + " " + message.MessageId);

    return message.MessageText;
}).WithName("Peek a message from Queue");

app.MapPost("/getitem/{queuename}",(string queuename) => {
    var queueClient = new QueueClient(connectionString, queuename);
    QueueMessage message = queueClient.ReceiveMessage();
    if (message.DequeueCount >= 5)
    {
        queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
    }
    Console.WriteLine(message.MessageText + " " + message.MessageId);

    return message.MessageText;
}).WithName("Get a message from Queue");

app.MapPost("/getmultipleitems/{queuname}/{amount}",(string queuename, int amount) => {
    var queueClient = new QueueClient(connectionString, queuename);
    QueueMessage[] messages = queueClient.ReceiveMessages(amount);
    foreach (QueueMessage msg in messages)
    {
        Console.WriteLine(msg.MessageText + " " + msg.MessageId);
    }
}).WithName("Get multiple messages from Queue");

app.MapPost("/spam/",() => {
    var queueClient = new QueueClient(connectionString, queuename);
    for (int i = 0; i < 100; i++)
    {
        var payload = new JsonObject {
            { "id", i },
            { "name", "l3moni" },
            { "message", "Hello world" }};
   
        queueClient.SendMessage(JsonSerializer.Serialize(payload));
        //queueClient.SendMessage("Message " + i);
    }
}).WithName("Spam the queue");

app.MapPost("/deletemessage/{queuename}",(string queuename) => {
    var queueClient = new QueueClient(connectionString, queuename);

    try {
    QueueMessage message = queueClient.ReceiveMessage();
    var messageID = message.MessageId;
    var dequeue = message.DequeueCount;
    queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
    return "Message deleted: " + messageID;
    }
    catch (RequestFailedException e) {
        var errormessage = e.Message;
        return "No message to delete: " + errormessage;
    }
    
}).WithName("Delete a message from Queue");
    
app.Run();