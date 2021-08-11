# TCP-IP Server

Simple C# and .NET Core implementation of a TCP server.  
Useful if you are building a TCP client, and would like a way to test it quickly.

## Building the server

To build the project, use your tool of preference, such as Visual Studio.  
It will generate the library `TcpServer.dll`, which is a Console Application.

## Pre-requisites

.NET Core must be installed on your machine.

## Running the server

To run the TCP server, follow the steps below:

1. Open a Command Prompt
2. CD (change dir) into your build folder, such as `bin/Debug`
3. Run the following command:

> `> dotnet TcpServer.dll -ip <ip> -port <port>`

For `ip` it's preferable to use the string `any`, and for `port` use any available number, such as `10000`. For example:

> `> dotnet TcpServer.dll -ip any -port 10000`

If the TCP server starts successfully, it displays the message `Waiting for client connection`

If clients connect to your server and send messages, the server will display those messages into the CMD window, next to their session IDs.


## Sending one-liner messages to connected clients

At the CMD prompt, run a command such as:

> `> 3: Hello there, client number three!`

The first token is the session ID (in this case `3`), followed by colon + space, followed by the actual one-liner message you want to send.


## Sending multi-line messages

In case you need to test messages composed of multiple lines, you can use files.  
Place the message inside a file, then run a command such as:

> `> 15: C:\temp\tcp\test-payload.msg`

The syntax is very similar, but instead of the actual message you pass the file path + name, after the session ID + colon + space.

If such a file exists, the TCP server will read its content, and send it over the corresponding connection.

## Stopping the server

To stop the server, runt the following command:

> `> stop!`

The sever will stop listening, release the port, and exit.

