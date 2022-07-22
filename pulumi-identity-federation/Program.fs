module Program

open Pulumi.FSharp
open Pulumi.Aws.S3
open Pulumi.Aws.Iam
open Pulumi.Aws.Iam.Inputs

let infra () =

  let openIdConnectProviderArgs = OpenIdConnectProviderArgs(
      Url = "https://token.actions.githubusercontent.com",
      ClientIdLists = inputList [input "sts.amazonaws.com"],
      ThumbprintLists = inputList [input "6938fd4d98bab03faadb97b34396831e3780aea1"])

  let openIdConnectProvider = OpenIdConnectProvider("GithubOidc", openIdConnectProviderArgs)

  let githubIamRole =
    
    let federatedPrincipal =
        GetPolicyDocumentStatementPrincipalInputArgs(Type = "Federated", Identifiers = inputList [ io openIdConnectProvider.Arn])

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
      RoleArgs(AssumeRolePolicy = io (policyDocument.Apply(fun (pd) -> pd.Json)))
        )

  dict [("openIdConnectProvider", openIdConnectProvider.Id :> obj);
        ("githubIamRole", githubIamRole.Id :> obj)]

[<EntryPoint>]
let main _ =
  Deployment.run infra
