# Project Overview

This is project is a Demo app showcasing Dapr in both Go lang, .NET and Java. The app showcase an app that service transactions on a bank credit account where the order is important. The purpose is to show how to use Dapr to build a portable app that can run on any cloud provider. It also shows how to monitor the application with OpenTelemetry and the OpenTelemetry Collector.

## Folder Structure

- `/docs`: Contains documentation for the project, including API specifications and user guides.
- `/dapr`: Contains Dapr related files, such as configuration files for Dapr components and pub/sub.
- `/src`: Contains the source code the applications
- `/src/go`: Contains the Go application source code.
- `/src/dotnet`: Contains the .NET application source code.
- `/src/java`: Contains the Java application source code.
- `/infrastructure`: Contains the infrastructure as code (IaC) files for deploying the applications. Contains both container apps with bicep and kubernetes manifests.

## Libraries and Frameworks

- Dapr: A portable, event-driven runtime that makes it easy to build resilient, microservices-based applications.
- DotNet: A free, cross-platform, open-source developer platform for building many different types of applications.
- OpenTelemetry: A set of APIs, libraries, agents, and instrumentation to provide observability for applications.
- Java: A high-level, class-based, object-oriented programming language that is designed to have as few implementation dependencies as possible.

## Architecture

The application is built using Dapr to act as an abstraction for the infrastructure dependencies, allowing developers to focus on building business logic without worrying about the underlying infrastructure. The main architecture is event driven. The credit api serves transactions and sends booking events to the pub/sub component, which then forwards them to the appropriate services for processing.
