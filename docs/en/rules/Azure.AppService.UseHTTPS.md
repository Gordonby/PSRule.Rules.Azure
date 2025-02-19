---
severity: Important
pillar: Security
category: Data protection
resource: App Service
online version: https://azure.github.io/PSRule.Rules.Azure/en/rules/Azure.AppService.UseHTTPS/
ms-content-id: b26053bc-db4a-487a-8fb1-11c438c8d493
---

# Enforce encrypted App Service connections

## SYNOPSIS

Azure App Service apps should only accept encrypted connections.

## DESCRIPTION

Azure App Service apps are configured by default to accept encrypted and unencrypted connections.
HTTP connections can be automatically redirected to use HTTPS when the _HTTPS Only_ setting is enabled.

Unencrypted communication to App Service apps could allow disclosure of information to an untrusted party.

## RECOMMENDATION

When access using unencrypted HTTP connection is not required consider enabling _HTTPS Only_.
Also consider using Azure Policy to audit or enforce this configuration.

## LINKS

- [Data encryption in Azure](https://docs.microsoft.com/azure/architecture/framework/security/design-storage-encryption#data-in-transit)
- [Enforce HTTPS](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-https)
