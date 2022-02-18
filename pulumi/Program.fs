module Program

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws
open Pulumi.Aws.S3
open Pulumi.Aws.Iam
open Pulumi.Aws.Iam.Inputs

let infra () =


    let asyncCallerIndentity =
        async {
            let! result = GetCallerIdentity.InvokeAsync() |> Async.AwaitTask
            return result
        }

    let asyncRegion =
        async {
            let! result = GetRegion.InvokeAsync() |> Async.AwaitTask
            return result
        }

    let callerIdentity =
        Async.RunSynchronously(asyncCallerIndentity)

    let region = Async.RunSynchronously(asyncRegion)

    let regionName = region.Name
    let accountId = callerIdentity.AccountId
    let bucketName = $"image-resize-{accountId}-{regionName}"
    let bucketArgs = BucketArgs(Acl = "private")
    let bucket = Bucket(bucketName, bucketArgs)

    let principal =
        GetPolicyDocumentStatementPrincipalInputArgs(Type = "AWS", Identifiers = inputList [ input "*" ])


    let statements =
        GetPolicyDocumentStatementInputArgs(
            Principals = inputList [ input principal ],
            Actions = inputList [ input "s3:GetObject" ],
            Resources =
                inputList [ io bucket.Arn
                            io (Output.Format($"{bucket.Arn}/*")) ]
        )


    let policyDocumentInvokeArgs =
        GetPolicyDocumentInvokeArgs(Statements = statements)

    let policyDocument = GetPolicyDocument.Invoke(policyDocumentInvokeArgs)

    let bucketPolicyArgs =
        BucketPolicyArgs(Bucket = bucket.Id, Policy = io (policyDocument.Apply(fun (pd) -> pd.Json)))

    let bucketPolicy =
        BucketPolicy("allowAllGetObject", bucketPolicyArgs)


    // Export the name of the bucket
    dict [ ("bucketName", bucket.Id :> obj) ]

[<EntryPoint>]
let main _ = Deployment.run infra
