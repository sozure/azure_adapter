# VGManager Backend: A Backend API for Library and KeyVault Interaction

This .NET API project is designed to provide a seamless interface for interacting with Azure DevOps and Azure KeyVault. It is stateless and allows users to perform various operations related to these services through a set of well-defined endpoints. This README will guide you through the setup, endpoints, and usage of this API.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Endpoints](#endpoints)
  - [GET Projects](#get-projects)
  - [GET Secrets from KeyVault](#get-secrets)
  - [GET Deleted Secrets from KeyVault](#get-deleted-secrets-from-keyvault)
  - [Delete Secrets](#delete-secrets)
  - [Delete Secret Inline](#delete-secret-inline)
  - [Recover Secrets](#recover-secrets)
  - [Recover Secret Inline](#recover-secret-inline)
  - [Copy-Paste Entire KeyVaults](#copy-paste-entire-keyvaults)
  - [Get Variables from Variable Groups](#get-variables-from-variable-groups)
  - [Update variables of Variable Groups](#update-variables-of-variable-groups)
  - [Update variable of Variable Group Inline](#update-variable-of-variable-group-inline)
  - [Add variables of Variable Groups](#add-variables-of-variable-groups)
  - [Delete variables of Variable Groups](#delete-variables-of-variable-groups)
  - [Delete variable of Variable Group Inline](#delete-variable-of-variable-group-inline)

- [Usage](#usage)

## Prerequisites
Before you can use this API, make sure you have the following prerequisites installed and configured:

- .NET SDK (version 6.0 or higher)
- Azure DevOps SDK NuGet packages
- Azure KeyVault access and authentication credentials
- Visual Studio or a compatible IDE for development
- An Azure DevOps organization and project with the necessary permissions

## Getting Started
1. Clone this repository to your local machine.
2. Open the project in your preferred IDE.
3. Configure your Azure DevOps and KeyVault authentication credentials.
4. Build and run the project.

## Endpoints

### GET Projects

**Endpoint:** `/api/Project/Get`
**Description:** Retrieve a list of projects from your Azure DevOps organization.

### GET Secrets

**Endpoint:** `/api/Secret/Get`
**Description:** Retrieve secrets from Azure KeyVault.

### GET Deleted Secrets from KeyVault

**Endpoint:** `/api/Secret/GetDeleted`
**Description:** Retrieve deleted secrets from Azure KeyVault.

### Delete Secrets

**Endpoint:** `/api/Secret/Delete`
**Description:** Delete secrets from Azure KeyVault.

### Delete Secret Inline

**Endpoint:** `/api/Secret/DeleteInline`
**Description:** Delete secret from Azure KeyVault.

### Recover Secrets

**Endpoint:** `/api/Secret/Recover`
**Description:** Recover deleted secrets in Azure KeyVault.

### Recover Secret Inline

**Endpoint:** `/api/Secret/RecoverInline`
**Description:** Recover deleted secret in Azure KeyVault.

### Copy-Paste Entire KeyVaults

**Endpoint:** `/api/Secret/Copy`
**Description:** Copy and paste entire KeyVaults, including secrets, from one location to another.

### Get variables from Variable Groups

**Endpoint:** `/api/VariableGroup/Get`
**Description:** Retrieve variables from variable groups inside Azure DevOps Libraries.

### Update variables of Variable Groups

**Endpoint:** `/api/VariableGroup/Update`
**Description:** Perform update operation on variables within variable groups inside Azure DevOps Libraries.

### Update variable of Variable Group Inline

**Endpoint:** `/api/VariableGroup/UpdateInline`
**Description:** Perform update operation on variable within variable group inside Azure DevOps Libraries.

### Add variables of Variable Groups

**Endpoint:** `/api/VariableGroup/Add`
**Description:** Perform add operation on variables within variable groups inside Azure DevOps Libraries.

### Delete variables of Variable Groups

**Endpoint:** `/api/VariableGroup/Delete`
**Description:** Perform delete operation on variables within variable groups inside Azure DevOps Libraries.

### Delete variable of Variable Group Inline

**Endpoint:** `/api/VariableGroup/DeleteInline`
**Description:** Perform delete operation on variable within variable group inside Azure DevOps Libraries.

## Usage
Once the API is up and running, you can interact with it using your preferred API client or by making HTTP requests directly. Make sure to provide the required parameters and authentication tokens as needed for each endpoint.

Enjoy using the Azure DevOps and KeyVault interaction API! If you encounter any issues or have suggestions for improvements, please feel free to open an issue or submit a pull request.