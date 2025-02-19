# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#
# Unit tests for API Management
#

[CmdletBinding()]
param ()

BeforeAll {
    # Setup error handling
    $ErrorActionPreference = 'Stop';
    Set-StrictMode -Version latest;

    if ($Env:SYSTEM_DEBUG -eq 'true') {
        $VerbosePreference = 'Continue';
    }

    # Setup tests paths
    $rootPath = $PWD;
    Import-Module (Join-Path -Path $rootPath -ChildPath out/modules/PSRule.Rules.Azure) -Force;
    $here = (Resolve-Path $PSScriptRoot).Path;
}

Describe 'Azure.APIM' -Tag 'APIM' {
    Context 'Conditions' {
        BeforeAll {
            $invokeParams = @{
                Baseline = 'Azure.All'
                Module = 'PSRule.Rules.Azure'
                WarningAction = 'Ignore'
                ErrorAction = 'Stop'
            }
            $dataPath = Join-Path -Path $here -ChildPath 'Resources.APIM.json';
            $result = Invoke-PSRule @invokeParams -InputPath $dataPath -Outcome All;
        }

        It 'Azure.APIM.Protocols' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.Protocols' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.HTTPEndpoint' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.HTTPEndpoint' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.APIDescriptors' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.APIDescriptors' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 2;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C';
            $ruleResult[0].Reason | Should -BeLike "The API '*' does not have a description set.";

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The API 'api-B' does not have a description set.";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -BeExactly "The API 'api-B' does not have a description set.";


            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 14;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.HTTPBackend' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.HTTPBackend' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -HaveCount 2;
            $ruleResult[0].Reason | Should -Be @(
                "The backend URL for 'endpoint-B' is not a HTTPS endpoint.", 
                "The service URL for 'api-B' is not a HTTPS endpoint."
            );

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.EncryptValues' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.EncryptValues' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 2;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-C';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The named value 'property-B' is not secret.";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -BeExactly "The named value 'property-B' is not secret.";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 14;
            $ruleResult.TargetName | Should -Be 'apim-B', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.ProductSubscription' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.ProductSubscription' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 2;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The product 'product-B' does not require a subscription to use.";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -BeExactly "The product 'product-B' does not require a subscription to use.";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 14;
            $ruleResult.TargetName | Should -Be 'apim-A', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.ProductApproval' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.ProductApproval' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The product 'starter' does not require approval.";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.SampleProducts' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.SampleProducts' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.ProductDescriptors' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.ProductDescriptors' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-A';
            $ruleResult[0].Reason | Should -BeLike "The product 'product-*' does not have a description set.";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.ProductTerms' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.ProductTerms' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 2;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-C';
            
            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -HaveCount 4;
            $ruleResult[0].Reason | Should -Be @(
                "The product 'product-A' does not have legal terms set.", 
                "The product 'product-B' does not have legal terms set.", 
                "The product 'starter' does not have legal terms set.", 
                "The product 'unlimited' does not have legal terms set."
            );

            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -HaveCount 2;
            $ruleResult[1].Reason | Should -Be @(
                "The product 'product-A' does not have legal terms set.",
                "The product 'product-B' does not have legal terms set."
            );

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 14;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.ManagedIdentity' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.ManagedIdentity' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 1;
            $ruleResult.TargetName | Should -Be 'apim-B';

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 15;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.CertificateExpiry' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.CertificateExpiry' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 14;
            $ruleResult.TargetName | Should -Be 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The certificate for host name 'api.contoso.com' expires or expired on '2020/01/01'.";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 2;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-A';
        }

        It 'Azure.APIM.Name' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.Name' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -BeNullOrEmpty;

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 16;
            $ruleResult.TargetName | Should -BeIn 'apim-B', 'apim-A', 'apim-C', 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-M', 'apim-N', 'apim-O', 'apim-P';
        }

        It 'Azure.APIM.AvailabilityZone' {
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.AvailabilityZone' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 8;
            $ruleResult.TargetName | Should -BeIn 'apim-D', 'apim-F', 'apim-G', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-P';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The API management service (apim-D) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -Be @(
                "The API management service (apim-F) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-F) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[2].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[2].Reason | Should -BeExactly "The API management service (apim-G) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[3].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[3].Reason | Should -BeExactly "The API management service (apim-I) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[4].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[4].Reason | Should -BeExactly "The API management service (apim-J) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[5].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[5].Reason | Should -Be @(
                "The API management service (apim-K) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-K) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[6].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[6].Reason | Should -BeExactly "The API management service (apim-L) deployed to region (Australia East) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[7].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[7].Reason | Should -BeExactly "The API management service (apim-P) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1].";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 5;
            $ruleResult.TargetName | Should -BeIn 'apim-E', 'apim-H', 'apim-M', 'apim-N', 'apim-O';

            # None
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'None' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 3;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-B', 'apim-C';
        }
    }

    Context 'Resource name' {
        BeforeAll {
            $invokeParams = @{
                Baseline = 'Azure.All'
                Module = 'PSRule.Rules.Azure'
                WarningAction = 'Ignore'
                ErrorAction = 'Stop'
            }
            $testObject = [PSCustomObject]@{
                Name = ''
                ResourceType = 'Microsoft.ApiManagement/service'
            }
        }

        BeforeDiscovery {
            $validNames = @(
                'apim-1'
                'APIM001'
                'a'
            )
            $invalidNames = @(
                '_apim1'
                '-apim1'
                'apim1_'
                'apim1-'
                '1apim'
            )
        }

        # Pass
        It '<_>' -ForEach $validNames {
            $testObject.Name = $_;
            $ruleResult = $testObject | Invoke-PSRule @invokeParams -Name 'Azure.APIM.Name';
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Outcome | Should -Be 'Pass';
        }

        # Fail
        It '<_>' -ForEach $invalidNames {
            $testObject.Name = $_;
            $ruleResult = $testObject | Invoke-PSRule @invokeParams -Name 'Azure.APIM.Name';
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Outcome | Should -Be 'Fail';
        }
    }

    Context 'With Configuration Option' -Tag 'Configuration' {
        BeforeAll {
            $invokeParams = @{
                Baseline = 'Azure.All'
                Module = 'PSRule.Rules.Azure'
                WarningAction = 'Ignore'
                ErrorAction = 'Stop'
            }
            $dataPath = Join-Path -Path $here -ChildPath 'Resources.APIM.json';
            $configPath = Join-Path -Path $here -ChildPath 'ps-rule-options.yaml';
        }

        It 'Azure.APIM.AvailabilityZone - HashTable option' {
            $option = @{
                'Configuration.AZURE_APIM_ADDITIONAL_REGION_AVAILABILITY_ZONE_LIST' = @(
                    [PSCustomObject]@{
                        Location = 'Australia Southeast'
                        Zones = @("1", "2", "3")
                    }
                    [PSCustomObject]@{
                        Location = 'Norway East'
                        Zones = @("1", "2", "3")
                    }
                )
            }
            $result = Invoke-PSRule @invokeParams -InputPath $dataPath -Option $option -Outcome All;
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.AvailabilityZone' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 10;
            $ruleResult.TargetName | Should -BeIn 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-P';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The API management service (apim-D) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -BeExactly "The API management service (apim-E) deployed to region (australiasoutheast) should use a minimum of two availability zones from the following [1, 2, 3].";
            $ruleResult[2].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[2].Reason | Should -Be @(
                "The API management service (apim-F) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-F) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[3].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[3].Reason | Should -Be @(
                "The API management service (apim-G) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-G) deployed to region (Norway East) should use a minimum of two availability zones from the following [1, 2, 3]."
            )
            $ruleResult[4].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[4].Reason | Should -BeExactly "The API management service (apim-H) deployed to region (norwayeast) should use a minimum of two availability zones from the following [1, 2, 3].";
            $ruleResult[5].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[5].Reason | Should -BeExactly "The API management service (apim-I) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[6].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[6].Reason | Should -BeExactly "The API management service (apim-J) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[7].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[7].Reason | Should -Be @(
                "The API management service (apim-K) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-K) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[8].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[8].Reason | Should -Be @(
                "The API management service (apim-L) deployed to region (australiasoutheast) should use a minimum of two availability zones from the following [1, 2, 3]."
                "The API management service (apim-L) deployed to region (Australia East) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[9].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[9].Reason | Should -BeExactly "The API management service (apim-P) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1].";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 3;
            $ruleResult.TargetName | Should -BeIn 'apim-M', 'apim-N', 'apim-O';
            
            # None
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'None' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 3;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-B', 'apim-C';
        }

        It 'Azure.APIM.AvailabilityZone - YAML file option' {
            $result = Invoke-PSRule @invokeParams -InputPath $dataPath -Option $configPath -Outcome All;
            $filteredResult = $result | Where-Object { $_.RuleName -eq 'Azure.APIM.AvailabilityZone' };

            # Fail
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Fail' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 10;
            $ruleResult.TargetName | Should -BeIn 'apim-D', 'apim-E', 'apim-F', 'apim-G', 'apim-H', 'apim-I', 'apim-J', 'apim-K', 'apim-L', 'apim-P';

            $ruleResult[0].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[0].Reason | Should -BeExactly "The API management service (apim-D) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[1].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[1].Reason | Should -BeExactly "The API management service (apim-E) deployed to region (australiasoutheast) should use a minimum of two availability zones from the following [1, 2, 3].";
            $ruleResult[2].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[2].Reason | Should -Be @(
                "The API management service (apim-F) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-F) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[3].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[3].Reason | Should -Be @(
                "The API management service (apim-G) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-G) deployed to region (Norway East) should use a minimum of two availability zones from the following [1, 2, 3]."
            )
            $ruleResult[4].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[4].Reason | Should -BeExactly "The API management service (apim-H) deployed to region (norwayeast) should use a minimum of two availability zones from the following [1, 2, 3].";
            $ruleResult[5].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[5].Reason | Should -BeExactly "The API management service (apim-I) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[6].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[6].Reason | Should -BeExactly "The API management service (apim-J) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1].";
            $ruleResult[7].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[7].Reason | Should -Be @(
                "The API management service (apim-K) deployed to region (australiaeast) should use a minimum of two availability zones from the following [3, 2, 1]."
                "The API management service (apim-K) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[8].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[8].Reason | Should -Be @(
                "The API management service (apim-L) deployed to region (australiasoutheast) should use a minimum of two availability zones from the following [1, 2, 3]."
                "The API management service (apim-L) deployed to region (Australia East) should use a minimum of two availability zones from the following [3, 2, 1]."
            )
            $ruleResult[9].Reason | Should -Not -BeNullOrEmpty;
            $ruleResult[9].Reason | Should -BeExactly "The API management service (apim-P) deployed to region (East US) should use a minimum of two availability zones from the following [3, 2, 1].";

            # Pass
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'Pass' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 3;
            $ruleResult.TargetName | Should -BeIn 'apim-M', 'apim-N', 'apim-O';
            
            # None
            $ruleResult = @($filteredResult | Where-Object { $_.Outcome -eq 'None' });
            $ruleResult | Should -Not -BeNullOrEmpty;
            $ruleResult.Length | Should -Be 3;
            $ruleResult.TargetName | Should -BeIn 'apim-A', 'apim-B', 'apim-C';
        }
    }
}
