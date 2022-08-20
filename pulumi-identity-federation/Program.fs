module Program

open Pulumi.FSharp
open Pulumi.Aws.Iam
open Pulumi.Aws.Iam.Inputs

let infra () =

  let cloudFrontPolicy =

        let cloudFrontStatement =
            GetPolicyDocumentStatementInputArgs(
                Actions = inputList [ input "cloudfront:CreateCloudFrontOriginAccessIdentity";
                input "cloudfront:GetCloudFrontOriginAccessIdentity";
                input "cloudfront:UpdateCloudFrontOriginAccessIdentity";
                input "cloudfront:DeleteCloudFrontOriginAccessIdentity";
                input "s3:CreateAccessPoint";
                input "s3:PutAnalyticsConfiguration";
                input "s3:PutAccelerateConfiguration";
                input "s3:PutAccessPointConfigurationForObjectLambda";
                input "s3:DeleteObjectVersion";
                input "s3:PutStorageLensConfiguration";
                input "s3:RestoreObject";
                input "s3:DeleteAccessPoint";
                input "s3:CreateBucket";
                input "s3:DeleteAccessPointForObjectLambda";
                input "s3:ReplicateObject";
                input "s3:PutEncryptionConfiguration";
                input "s3:DeleteBucketWebsite";
                input "s3:AbortMultipartUpload";
                input "s3:PutLifecycleConfiguration";
                input "s3:UpdateJobPriority";
                input "s3:DeleteObject";
                input "s3:CreateMultiRegionAccessPoint";
                input "s3:DeleteBucket";
                input "s3:PutBucketVersioning";
                input "s3:PutIntelligentTieringConfiguration";
                input "s3:PutMetricsConfiguration";
                input "s3:PutBucketOwnershipControls";
                input "s3:DeleteMultiRegionAccessPoint";
                input "s3:PutObjectLegalHold";
                input "s3:InitiateReplication";
                input "s3:UpdateJobStatus";
                input "s3:PutBucketCORS";
                input "s3:PutInventoryConfiguration";
                input "s3:PutObject";
                input "s3:PutBucketNotification";
                input "s3:DeleteStorageLensConfiguration";
                input "s3:PutBucketWebsite";
                input "s3:PutBucketRequestPayment";
                input "s3:PutObjectRetention";
                input "s3:PutBucketLogging";
                input "s3:CreateAccessPointForObjectLambda";
                input "s3:PutBucketObjectLockConfiguration";
                input "s3:ReplicateDelete";
                input "s3:ListAccessPointsForObjectLambda";
                input "s3:GetObjectVersionTagging";
                input "s3:GetStorageLensConfigurationTagging";
                input "s3:GetObjectAcl";
                input "s3:GetBucketObjectLockConfiguration";
                input "s3:GetIntelligentTieringConfiguration";
                input "s3:GetObjectVersionAcl";
                input "s3:GetBucketPolicyStatus";
                input "s3:GetObjectRetention";
                input "s3:GetBucketWebsite";
                input "s3:GetJobTagging";
                input "s3:ListJobs";
                input "s3:GetMultiRegionAccessPoint";
                input "s3:GetObjectAttributes";
                input "s3:GetObjectLegalHold";
                input "s3:GetBucketNotification";
                input "s3:DescribeMultiRegionAccessPointOperation";
                input "s3:GetReplicationConfiguration";
                input "s3:ListMultipartUploadParts";
                input "s3:GetObject";
                input "s3:DescribeJob";
                input "s3:GetAnalyticsConfiguration";
                input "s3:GetObjectVersionForReplication";
                input "s3:GetAccessPointForObjectLambda";
                input "s3:GetStorageLensDashboard";
                input "s3:GetLifecycleConfiguration";
                input "s3:GetAccessPoint";
                input "s3:GetInventoryConfiguration";
                input "s3:GetBucketTagging";
                input "s3:GetAccessPointPolicyForObjectLambda";
                input "s3:GetBucketLogging";
                input "s3:ListBucketVersions";
                input "s3:ListBucket";
                input "s3:GetAccelerateConfiguration";
                input "s3:GetObjectVersionAttributes";
                input "s3:GetBucketPolicy";
                input "s3:GetEncryptionConfiguration";
                input "s3:GetObjectVersionTorrent";
                input "s3:GetBucketRequestPayment";
                input "s3:GetAccessPointPolicyStatus";
                input "s3:GetObjectTagging";
                input "s3:GetMetricsConfiguration";
                input "s3:GetBucketOwnershipControls";
                input "s3:GetBucketPublicAccessBlock";
                input "s3:GetMultiRegionAccessPointPolicyStatus";
                input "s3:ListBucketMultipartUploads";
                input "s3:GetMultiRegionAccessPointPolicy";
                input "s3:GetAccessPointPolicyStatusForObjectLambda";
                input "s3:ListAccessPoints";
                input "s3:GetBucketVersioning";
                input "s3:ListMultiRegionAccessPoints";
                input "s3:GetBucketAcl";
                input "s3:GetAccessPointConfigurationForObjectLambda";
                input "s3:ListStorageLensConfigurations";
                input "s3:GetObjectTorrent";
                input "s3:GetStorageLensConfiguration";
                input "s3:GetAccountPublicAccessBlock";
                input "s3:ListAllMyBuckets";
                input "s3:GetBucketCORS";
                input "s3:GetBucketLocation";
                input "s3:GetAccessPointPolicy";
                input "s3:GetObjectVersion";
                input "lambda:CreateFunction";
                input "lambda:UpdateFunctionCode";
                input "lambda:DeleteFunction";
                input "lambda:PublishVersion";
                input "iam:AttachRolePolicy";
                input "iam:CreateRole";
                input "iam:CreatePolicy";
                input "iam:DeletePolicy";
                input "iam:GetRole";
                input "iam:ListInstanceProfilesForRole";
                input "iam:ListAttachedRolePolicies";
                input "iam:ListRoles";
                input "iam:ListRolePolicies";
                input "iam:DetachRolePolicy";
                input "iam:DeleteRole" ],
                Resources =
                    inputList [ input "*" ]
            )


        let policyDocumentInvokeArgs =
            GetPolicyDocumentInvokeArgs(
                Statements =
                    inputList [ input cloudFrontStatement ]
            )

        let policyDocument =
            GetPolicyDocument.Invoke(policyDocumentInvokeArgs)

        let policyArgs =
            PolicyArgs(PolicyDocument = io (policyDocument.Apply(fun (pd) -> pd.Json)))

        Policy("cloudFrontPolicy", policyArgs)
  
  
  let openIdConnectProviderArgs = OpenIdConnectProviderArgs(
      Url = "https://token.actions.githubusercontent.com",
      ClientIdLists = inputList [input "sts.amazonaws.com"],
      ThumbprintLists = inputList [input "6938fd4d98bab03faadb97b34396831e3780aea1"])

  let openIdConnectProvider = OpenIdConnectProvider("GithubOidc", openIdConnectProviderArgs)

  let federatedPrincipal =
    GetPolicyDocumentStatementPrincipalInputArgs(Type = "Federated", Identifiers = inputList [ io openIdConnectProvider.Arn])
  
  let githubActionsRole =
    
    let assumeRoleWithWebIdentityStatement =
        GetPolicyDocumentStatementInputArgs(
            Principals =
                inputList [ input federatedPrincipal ],
            Actions = inputList [ input "sts:AssumeRoleWithWebIdentity" ],
            Conditions = inputList [
              input (GetPolicyDocumentStatementConditionInputArgs(
                Test = "StringLike",
                Variable = "token.actions.githubusercontent.com:sub",
                Values = inputList [ input "repo:simonschoof/lambda-at-edge-example:*"]
                ))
            ]
        )

    let policyDocumentInvokeArgs =
        GetPolicyDocumentInvokeArgs(
            Statements =
                inputList [ input assumeRoleWithWebIdentityStatement ]
        )
    let policyDocument =
        GetPolicyDocument.Invoke(policyDocumentInvokeArgs)

    Role("githubActionsRole",
      RoleArgs(
        Name= "githubActionsRole", 
        AssumeRolePolicy = io (policyDocument.Apply(fun (pd) -> pd.Json)),
        ManagedPolicyArns = inputList [ io cloudFrontPolicy.Arn])
        )

  dict [("openIdConnectProvider", openIdConnectProvider.Id :> obj);
        ("githubIamRole", githubActionsRole.Id :> obj)]

[<EntryPoint>]
let main _ =
  Deployment.run infra
