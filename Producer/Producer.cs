// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Producer.cs" company="Craig Nicholson">
//   MIT
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Producer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Framing;

    /// <summary>
    /// The program.
    /// </summary>
    public class Producer
    {
        /// <summary>
        /// The main entry point
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            var hostName = "localhost";
            var userName = "electsolve";
            var password = "electsolve";
            var exchange = "OutageEventChangedNotification";

            var outageEvents = CreateOutageEvents();

            try
            {
                // var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
                var factory = new ConnectionFactory { HostName = hostName };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // declare the exchange / queue and the type, fanout for Pub/Sub
                    channel.ExchangeDeclare(exchange, ExchangeType.Fanout);

                    // create some headers and alter other basic properties
                    IBasicProperties properties = new BasicProperties()
                    {
                        ContentType = "text/plain",
                        DeliveryMode = 2,
                        Headers = new Dictionary<string, object>
                         {
                            { "ObjectType", "outageEvent[]" },
                            { "MultiSpeakVersion", "4.1.6" },
                            { "CorrelationId", Guid.NewGuid().ToString() },
                            { "AnyKeyThatWillHelpYou", "SaveOurFuture" },
                            { "IsWorthy", true },
                            { "latitude", 51.5252949 },
                            { "logitude", -0.0905493 },
                            { "SByte.MinValue", sbyte.MinValue }
                         },
                        Expiration = "36000000",
                        Persistent = true,
                    };

                    // body is the message we are sending
                    var body = XmlSerializeIntoBytes(outageEvents);

                    // Since we declare an exchnageType of fanout, routingKey will be ignored.  All consumers will get the message
                    // there is no need to route to specific consumers.
                    channel.BasicPublish(exchange, string.Empty, properties, body);
                    Console.WriteLine(" [x] Sent {0} bytes", body.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// The serialize object into byte array.  Using generic object failed to serialize the XmlElement[].
        /// </summary>
        /// <param name="outageEvents">
        /// The outage events.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        private static byte[] SerializeIntoBytes(object outageEvents)
        {
            if (outageEvents == null)
            {
                return null;
            }

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, outageEvents);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// The xml serialize into bytes.
        /// </summary>
        /// <param name="outageEvents">
        /// The outage events.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        private static byte[] XmlSerializeIntoBytes(outageEvent[] outageEvents)
        {
            if (outageEvents == null)
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                var xs = new XmlSerializer(typeof(outageEvent[]));
                var xmlTextWriter = new XmlTextWriter(ms, Encoding.UTF8);
                xs.Serialize(ms, outageEvents);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// The create outage events.
        /// </summary>
        /// <returns>
        /// The <see cref="outageEvent"/>.
        /// </returns>
        private static outageEvent[] CreateOutageEvents()
        {
            // Data for GMLocation
            var points = new PointType();
            var coords = new CoordType() { X = 2191236.56044671M, Y = 13757840.7320126M, YSpecified = true };
            points.Item = coords;

            // Example
            // <outageReasonCodeList>
            //    <extensions>
            //        <q1:outageReasonContainer 
            //            xmlns:q1="http://www.multispeak.org/Version_4.1_Release">
            //            <q1:outageReasonList>
            //                <q1:outageReasonItem>
            //                    <q1:category>Cause</q1:category>
            //                    <q1:outageReason>
            //                        <q1:description>Animal</q1:description>
            //                        <q1:outageReportingCodeList>
            //                            <q1:outageReportingCode>020</q1:outageReportingCode>
            //                        </q1:outageReportingCodeList>
            //                    </q1:outageReason>
            //                </q1:outageReasonItem>
            //                <q1:outageReasonItem>
            //                    <q1:category>Weather Condition</q1:category>
            //                    <q1:outageReason>
            //                        <q1:description>Extreme Heat</q1:description>
            //                        <q1:outageReportingCodeList>
            //                            <q1:outageReportingCode>030</q1:outageReportingCode>
            //                        </q1:outageReportingCodeList>
            //                    </q1:outageReason>
            //                </q1:outageReasonItem>
            //            </q1:outageReasonList>
            //        </q1:outageReasonContainer>
            //    </extensions>
            // </outageReasonCodeList>
            var outagereasoncontainer = new outageReasonContainer
            {               
                // xmlns:q1="http://www.multispeak.org/Version_4.1_Release"
                outageReasonList = new[]
                {
                    new outageReasonItem
                    {
                        category = "Weather Condition",
                        outageReason = new outageReason()
                        {
                            description = "Extreme Heat",
                            outageReportingCodeList = new[]
                            {
                                new outageReportingCode
                                {
                                    Value = "030"
                                }
                            }
                        }
                    },
                    new outageReasonItem
                    {
                        category = "Cause",
                        outageReason = new outageReason()
                        {
                            description = "Animal",
                            outageReportingCodeList = new[]
                            {
                                new outageReportingCode
                                {
                                    Value = "020"
                                }
                            }
                        }
                    }
                },
            };

            // Convert the the  outagereasoncontainer to XmlElement
            var xmlElements = new XmlElement[1];
            var doc = new XmlDocument();
            using (var writer = doc.CreateNavigator().AppendChild())
            {
                new XmlSerializer(outagereasoncontainer.GetType()).Serialize(writer, outagereasoncontainer);
            }

            xmlElements[0] = doc.DocumentElement;

            var outageEvents = new[]
                                   {
                                       new outageEvent
                                       {
                                           objectID = "2017-10-27-0003",
                                           extensionsList = new[]
                                                                {
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "isClosed",
                                                                            extValue = new extValue { Value = "True" },
                                                                            extType  = extType.boolean,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "substationName",
                                                                            extValue = new extValue { Value = "PARKWAY_T2" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "feederCode",
                                                                            extValue = new extValue { Value = "3" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "phaseCode",
                                                                            extValue = new extValue { Value = "B" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "statusPhaseB",
                                                                            extValue = new extValue { Value = "NormalOrRestored" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                },
                                           objectName = "T61563680002",
                                           GMLLocation = points,
                                           GPSLocation =
                                               new GPSLocation()
                                                   {
                                                       GPSValidity = true,
                                                       GPSValiditySpecified = true,
                                                       latitude = 29.576805699162243,
                                                       longitude = -98.299250294859718
                                                   },
                                           gridLocation = "61563680",
                                           area = "West",
                                           problemLocation = "T61563680002",
                                           deviceID = new objectRef()
                                                          {
                                                              name = "T61563680002",
                                                              noun  = new XmlQualifiedName("transformerBank"),
                                                              objectID = "86101734-89a6-11e6-90e7-1866da2dc956"
                                                          },
                                           deviceType = "Transformer",
                                           outagedPhase = phaseCode.B,
                                           substationCode = "60",
                                           feeder = "P301",
                                           outageStatus = outageStatus.Restored, 
                                           startTime = DateTime.Now.AddHours(-3),
                                           startTimeSpecified = true,
                                           completed = DateTime.Now,
                                           completedSpecified = true,
                                           customersAffected = "6",
                                           priorityCustomersCount = "0",
                                           ODEventCount = "1",
                                           customersRestored = "1",

                                           outageReasonCodeList = new outageReasonCodeList()
                                                                      {
                                                                          // holds the outagereasoncontainer
                                                                          extensions = new extensions()
                                                                          {
                                                                              Any = xmlElements
                                                                          }
                                                                      },
                                       },

                                       new outageEvent
                                       {
                                           objectID = "2017-10-27-0002",
                                           extensionsList = new[]
                                                                {
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "substationName",
                                                                            extValue = new extValue { Value = "PARKWAY_T2" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "feederCode",
                                                                            extValue = new extValue { Value = "3" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "phaseCode",
                                                                            extValue = new extValue { Value = "B" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "statusPhaseB",
                                                                            extValue = new extValue { Value = "NormalOrRestored" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                },
                                           objectName = "T61563680002",
                                           GMLLocation = points,
                                           GPSLocation =
                                               new GPSLocation()
                                                   {
                                                       GPSValidity = true,
                                                       GPSValiditySpecified = true,
                                                       latitude = 29.576805699162243,
                                                       longitude = -98.299250294859718
                                                   },
                                           gridLocation = "61563680",
                                           area = "West",
                                           problemLocation = "T61563680002",
                                           deviceID = new objectRef()
                                                          {
                                                              name = "T61563680002",
                                                              noun  = new XmlQualifiedName("transformerBank"),
                                                              objectID = "86101734-89a6-11e6-90e7-1866da2dc956"
                                                          },
                                           deviceType = "Transformer",
                                           outagedPhase = phaseCode.B,
                                           substationCode = "60",
                                           feeder = "P301",
                                           outageStatus = outageStatus.Restored,
                                           startTime = DateTime.Now.AddHours(-3),
                                           startTimeSpecified = true,
                                           completed = DateTime.Now,
                                           completedSpecified = true,
                                           customersAffected = "6",
                                           priorityCustomersCount = "0",
                                           ODEventCount = "1",
                                           customersRestored = "1",
                                           outageReasonCodeList = new outageReasonCodeList()
                                           {
                                               extensions = new extensions()
                                               {
                                                   Any = xmlElements
                                               }
                                           },
                                       },
                                       new outageEvent
                                       {
                                           objectID = "2017-10-27-0002",
                                           verb = action.Delete,
                                           comments = @"1 associated calls deleted
                                                          -----------------------------------------
                                                      Oct 27 2017  1:46PM
                                           OutageRecID: 2017 - 10 - 27 - 0002 discarded from DisSpatch | Outage Events by Dispatcher: LUIS2016\Administrator </ comments >
                                           ",
                                           extensionsList = new[]
                                                                {
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "substationName",
                                                                            extValue = new extValue { Value = "PARKWAY_T2" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "feederCode",
                                                                            extValue = new extValue { Value = "3" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "phaseCode",
                                                                            extValue = new extValue { Value = "B" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                    new extensionsItem
                                                                        {
                                                                            extName = "statusPhaseB",
                                                                            extValue = new extValue { Value = "NormalOrRestored" },
                                                                            extType  = extType.@string,
                                                                            extTypeSpecified = true
                                                                        },
                                                                },
                                           objectName = "T61563680002",
                                           GMLLocation = points,
                                           GPSLocation =
                                               new GPSLocation()
                                                   {
                                                       GPSValidity = true,
                                                       GPSValiditySpecified = true,
                                                       latitude = 29.576805699162243,
                                                       longitude = -98.299250294859718
                                                   },
                                           gridLocation = "61563680",
                                           area = "West",
                                           problemLocation = "T61563680002",
                                           deviceID = new objectRef()
                                                          {
                                                              name = "T61563680002",
                                                              noun  = new XmlQualifiedName("transformer"),
                                                              objectID = "86101734-89a6-11e6-90e7-1866da2dc956"
                                                          },
                                           deviceType = "Transformer",
                                           outagedPhase = phaseCode.B,
                                           substationCode = "60",
                                           feeder = "P301",
                                           outageStatus = outageStatus.Restored,
                                           startTime = DateTime.Now.AddHours(-3),
                                           startTimeSpecified = true,
                                           completed = DateTime.Now,
                                           completedSpecified = true,
                                           customersAffected = "6",
                                           priorityCustomersCount = "0",
                                           ODEventCount = "1",
                                           customersRestored = "1",
                                           outageReasonCodeList = new outageReasonCodeList()
                                           {
                                               extensions = new extensions()
                                               {
                                                   Any = xmlElements
                                               }
                                           },
                                       }
                                   };
            return outageEvents;
        }
    }
}
