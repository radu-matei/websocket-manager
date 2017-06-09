# websocket-manager

Travis: [![Build Status](https://travis-ci.org/radu-matei/websocket-manager.svg?branch=master)](https://travis-ci.org/radu-matei/websocket-manager)

NuGet: [![NuGet](https://img.shields.io/nuget/v/WebSocketManager.svg)](https://www.nuget.org/packages/WebSocketManager)

Simple middlware for real-time .NET Core
----------------------------------------

This is an Asp .Net Core middleware that provides real-time functionality to .NET Core applications. 

To the core, it is a WebSocket middleware for Asp .Net Core with TypeScript / JavaScript client and .Net Core client that supports the client and the server invoking each others' methods.

Why wouldn't I use SignalR for this?
------------------------------------

First of all, SignalR for Asp .Net Core is still in its very incipient stages. A preview is expected mid-2017, while a release near the end of 2017, so most probably it will be available for Asp .Net Core 2.0. 

> The preview and release information were taken from [this talk by Damian Edwards and David Fowler, the guys in charge of Asp .Net Core](https://vimeo.com/204078084).

What is this library's connection to SignalR?
----------------------------------------------

This library **is not an official release by Microsoft** and in any way related to the original SignalR project, other by a lot of concepts inspired from it. 

Because the release of SignalR for Asp .Net Core was delayed for so long, I decided to write a very basic, stripped down (compared to the original SignalR) that only supports WebSockets (is based on `Microsoft.AspNetCore.WebSockets`) with a TypeScript client.

A lot of features, both on the server side and the client side were written looking at SignalR (both old and new) code, so if you wrote SignalR in the past, the approach is very similar.

Get Started with **websocket-manager**
--------------------------------------

While it is still in development, you can see some examples of usage in the [`samples`](/samples) folder.

Contribute to **websocket-manager**
-----------------------------------

Contributions in any form are welcome! Submit issues with bugs and recommendations! 
**Pull Requests** are highly appreciated!