---
severity: Awareness
pillar: Operational Excellence
category: Repeatable infrastructure
resource: Front Door
online version: https://azure.github.io/PSRule.Rules.Azure/en/rules/Azure.FrontDoor.Name/
---

# Use valid Front Door names

## SYNOPSIS

Front Door names should meet naming requirements.

## DESCRIPTION

When naming Azure resources, resource names must meet service requirements.
The requirements for Front Door names are:

- Between 5 and 64 characters long.
- Alphanumerics and hyphens.
- Start and end with alphanumeric.
- Front Door names must be globally unique.

## RECOMMENDATION

Consider using names that meet Front Door naming requirements.
Additionally consider naming resources with a standard naming convention.

## NOTES

This rule does not check if Front Door names are unique.

## LINKS

- [Repeatable infrastructure](https://docs.microsoft.com/azure/architecture/framework/devops/automation-infrastructure)
- [Naming rules and restrictions for Azure resources](https://docs.microsoft.com/azure/azure-resource-manager/management/resource-name-rules)
- [Azure template reference](https://docs.microsoft.com/azure/templates/microsoft.network/frontdoors)
- [Tagging and resource naming](https://docs.microsoft.com/azure/architecture/framework/devops/app-design#tagging-and-resource-naming)
