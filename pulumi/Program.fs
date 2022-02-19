module Program

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws
open Pulumi.Aws.S3
open Pulumi.Aws.Iam
open Pulumi.Aws.Iam.Inputs
open System.Text.Json
open System.Collections.Generic

let infra () =

    let bucket =

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

        let accountId = callerIdentity.AccountId

        let bucketName =
            $"image-resize-{accountId}-{region.Name}"

        let bucketArgs = BucketArgs(Acl = "private")

        Bucket(bucketName, bucketArgs)
    

    let lambdaRole =

        let assumeRolePolicyJson =
            JsonSerializer.Serialize(
                Map<string, obj>
                    [ ("Version", "2012-10-17")
                      ("Statement",
                       Map<string, obj>
                           [ ("Action", "sts:AssumeRole");
                                 ("Effect", "Allow");
                                 ("Sid", "");
                                 ("Principal",
                                  Map [ ("Service",
                                         [ "lambda.amazonaws.com"
                                           "edgelambda.amazonaws.com" ]) ]) ]) ]
            )


        Role(
            "lambdaRole",
            RoleArgs(
                AssumeRolePolicy = assumeRolePolicyJson,
                Path = "/service-role/",
                ManagedPolicyArns = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
            )
        )
    
    let imageBucketPolicy =

        let lambdaPrincipal =
            GetPolicyDocumentStatementPrincipalInputArgs(Type = "AWS", Identifiers = inputList [ io lambdaRole.Arn ])

        //let globalPrincipal =  GetPolicyDocumentStatementPrincipalInputArgs(Type = "AWS", Identifiers = inputList [ input "*" ])

        let getObjectStatement =
            GetPolicyDocumentStatementInputArgs(
                Principals = inputList [ input lambdaPrincipal ],
                Actions = inputList [ input "s3:GetObject" ],
                Resources =
                    inputList [ io (Output.Format($"{bucket.Arn}/*")) ]
            )
        
        
        let putObjectStatement =
            GetPolicyDocumentStatementInputArgs(
                Principals = inputList [ input lambdaPrincipal ],
                Actions = inputList [ input "s3:PutObject" ],
                Resources =
                    inputList [ io (Output.Format($"{bucket.Arn}/*")) ]
            )

        let policyDocumentInvokeArgs =
            GetPolicyDocumentInvokeArgs(Statements = inputList [input getObjectStatement; input putObjectStatement])

        let policyDocument =
            GetPolicyDocument.Invoke(policyDocumentInvokeArgs)

        let bucketPolicyArgs =
            BucketPolicyArgs(Bucket = bucket.Id, Policy = io (policyDocument.Apply(fun (pd) -> pd.Json)))

        BucketPolicy("imageBucketpolicy", bucketPolicyArgs)


    // Export the name of the bucket
    dict [ ("bucketName", bucket.Id :> obj) ]

[<EntryPoint>]
let main _ = Deployment.run infra
