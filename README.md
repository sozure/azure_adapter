# VGManager Adapter: An adapter which retrieves data from Azure DevOps using Azure DevOps SDK.

VGManager Adapter is a .NET 8.0 project designed to fetch data from Azure DevOps using the Azure DevOps SDK. It serves as an intermediary between other VGManager microservices and Azure DevOps services. This README provides instructions on setting up, building, and running the microservice.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Usage](#usage)

## Prerequisites
Before you can use this adapter, make sure you have the following prerequisites installed and configured:

- .NET SDK (version 8.0)
- Azure DevOps SDK NuGet packages
- Azure KeyVault access and authentication credentials
- Visual Studio or a compatible IDE for development
- An Azure DevOps organization and project with the necessary permissions

## Getting Started
1. Clone this repository to your local machine.
2. Open the project in your preferred IDE.
3. Configure your Azure DevOps and KeyVault authentication credentials.
4. Build and run the project.

## Usage
Once the adapter is up and running, you can interact with it using Apache Kafka. Make sure to provide the required parameters and authentication tokens as needed for each endpoint.

Enjoy using VGManager Adapter! If you encounter any issues or have suggestions for improvements, please feel free to open an issue or submit a pull request.