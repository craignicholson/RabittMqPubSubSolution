# RabbitMQ C# Pub/Sub Example

## Goals

- Send message to RabbitMQ durable queue
- Have many durable Consumers all receive the message
- Message will be a serialized object (soap/xml) using multispeak
- Use BasicProperties Headers to help with processing the message

## Dependencies

1 - Erlang 64 bit version 20.2 (otp_win64_20.2.exe)
2 - RabbitMQ Server 3.7.0 (rabbitmq-server-3.7.0)
3 - Follow the directions from RabbitMQ to install the software
4 - If you create your own solution or project add the NuGet Rabbit.MQ.Client

## MultiSpeak 4.1.6

MultiSpeak is a standard in Utility Industry for various systems to communicate.

- AMI/AMR
- OMS
- MDM
- CIS

## RabbitMQ Usage

MultiSpeak has several types of notifications. One type is the OutageEventChangedNotification produced by Outage Management Systems.
Typically the OutageEventChangedNotfiiation is sent to a web service endpoint and this endpoint will then send the message to RabbitMQ.

The Producer in this example mocks up an OutageEventChangedNotification to test the Producre and Consumer.

## Running this example

- Start RabbitMQ
- Run the Consumer
- Run the Producer

## TODO

Add Web Service Endpoint which captures the OutageEventChangedNotification and sends this message to RabbitMQ.
