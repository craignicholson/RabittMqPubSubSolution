// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Consumer.cs" company="Craig Nicholson">
//   MIT
// </copyright>
// <summary>
//   Defines the ReceiveLogs type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Consumer
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// The program.
    /// </summary>
    public class Consumer
    {
        /// <summary>
        /// The main.
        /// </summary>
        public static void Main()
        {
            try
            {
                var hostName = "localhost";
                var userName = "electsolve";
                var password = "electsolve";
                var exchange = "OutageEventChangedNotification";

                var factory = new ConnectionFactory() { HostName = hostName };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);

                    // Random Queue Name, this way each consumer gets it's own queue, what about durability for all cosumers?
                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName, exchange: exchange, routingKey: string.Empty);

                    Console.WriteLine(" [*] Waiting for outageEvents.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                        {
                            Console.WriteLine($"[*] Received outageEvents bytes : {ea.Body.Length}.");

                            // Read out the headers and use the key value pairs for business or processing logic
                            if (ea.BasicProperties.IsHeadersPresent())
                            {
                                var headers = ea.BasicProperties.Headers;
                                foreach (var header in headers)
                                {
                                    // strings are encoded at byte[] so we have to parse these out
                                    // differnent than the other object values, more tests are needed
                                    if (header.Value.GetType().Name == "Byte[]")
                                    {
                                        var headerBytes = (byte[])header.Value;
                                        var value = Encoding.UTF8.GetString(headerBytes);
                                        Console.WriteLine($"key : {header.Key} ; value : {value}");
                                    }
                                    else
                                    {
                                        var value = Convert.ToString(header.Value);
                                        Console.WriteLine($"key : {header.Key}; value : {value}");
                                    }
                                }
                            }

                            var body = ea.Body;
                            ProcessMessage(body);
                        };
                    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// The process message and write the contents to the console in xml.
        /// </summary>
        /// <param name="body">
        /// The body.
        /// </param>
        private static void ProcessMessage(byte[] body)
        {
            var message = ByteArrayToXmlEventsObject(body);

            // Serialize to xml so we can read this in the ouput
            var serializer = new XmlSerializer(typeof(outageEvent[]));
            string xml;
            using (var sww = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sww))
                {
                    serializer.Serialize(writer, message);
                    xml = sww.ToString();
                }
            }
            
            Console.WriteLine(" [x] {0}", xml);
        }

        /// <summary>
        /// Convert the byte array to an object, for this example we want the outageEvent[] object.
        /// </summary>
        /// <param name="arrBytes">
        /// The array of bytes.
        /// </param>
        /// <returns>
        /// The <see cref="outageEvent"/>.
        /// </returns>
        private static outageEvent[] ByteArrayToXmlEventsObject(byte[] arrBytes)
        {
            if (arrBytes == null)
            {
                return null;
            }

            var xs = new XmlSerializer(typeof(outageEvent[]));
            using (var ms = new MemoryStream())
            {
                ms.Write(arrBytes, 0, arrBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                return (outageEvent[])xs.Deserialize(ms);
            }
        }
    }
}