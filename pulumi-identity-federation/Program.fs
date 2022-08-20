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
                input "s3:CreateBucket";
                input "s3:DeleteBucket";
                input "s3:ListBucket";
                input "s3:ListAllMyBuckets";
                input "s3:GetBucketAcl";
                input "s3:GetBucketCORS";
                input "s3:GetBucketWebsite";
                input "s3:GetBucketVersioning";
                input "s3:GetAccelerateConfiguration";
                input "s3:GetBucketRequestPayment";
                input "s3:GetBucketLogging";
                input "s3:GetBucketPolicy";
                input "s3:PutBucketPolicy";
                input "lambda:CreateFunction";
                input "lambda:UpdateFunctionCode";
                input "lambda:DeleteFunction";
                input "lambda:PublishVersion";
                input "iam:AttachRolePolicy";
                input "iam:CreateRole";
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
