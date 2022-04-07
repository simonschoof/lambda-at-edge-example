module Program

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws
open Pulumi.Aws.S3
open Pulumi.Aws.Iam
open Pulumi.Aws.Iam.Inputs
open System.Text.Json
open Pulumi.Aws.CloudFront
open Pulumi.Aws.CloudFront.Inputs

let infra () =

    (*
--------------------
S3
--------------------
*)
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


    (*
--------------------
IAM
--------------------
*)
    let originAccessIdentity =

        let originAccessIdentityArgs =
            OriginAccessIdentityArgs(Comment = "Access identy to access the origin bucket")

        OriginAccessIdentity("Cloudfront Origin Access Identity", originAccessIdentityArgs)  

    let lambdaRole =

        let assumeRolePolicyJson =
            JsonSerializer.Serialize(
                Map<string, obj>
                    [ ("Version", "2012-10-17")
                      ("Statement",
                       Map<string, obj>
                           [ ("Action", "sts:AssumeRole")
                             ("Effect", "Allow")
                             ("Sid", "")
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

    let lambdaPrincipal =
        GetPolicyDocumentStatementPrincipalInputArgs(Type = "AWS", Identifiers = inputList [ io lambdaRole.Arn ])

    let cloudFrontPrincipal =
        GetPolicyDocumentStatementPrincipalInputArgs(
            Type = "AWS",
            Identifiers = inputList [ io originAccessIdentity.IamArn ]
        )

    let imageBucketPolicy =

        let getObjectStatement =
            GetPolicyDocumentStatementInputArgs(
                Principals = inputList [ input lambdaPrincipal; input cloudFrontPrincipal ],
                Actions = inputList [ input "s3:GetObject" ],
                Resources =
                    inputList [ io bucket.Arn
                                io (Output.Format($"{bucket.Arn}/*")) ]
            )

        let putObjectAndListBucketStatement =
            GetPolicyDocumentStatementInputArgs(
                Principals = inputList [ input lambdaPrincipal ],
                Actions = inputList [ input "s3:PutObject"; input "s3:ListBucket" ],
                Resources =
                    inputList [ io bucket.Arn
                                io (Output.Format($"{bucket.Arn}/*")) ]
            )
        

        let policyDocumentInvokeArgs =
            GetPolicyDocumentInvokeArgs(
                Statements =
                    inputList [ input getObjectStatement
                                input putObjectAndListBucketStatement ]
            )

        let policyDocument =
            GetPolicyDocument.Invoke(policyDocumentInvokeArgs)

        let bucketPolicyArgs =
            BucketPolicyArgs(Bucket = bucket.Id, Policy = io (policyDocument.Apply(fun (pd) -> pd.Json)))

        BucketPolicy("imageBucketpolicy", bucketPolicyArgs)

    (*
--------------------
Lambda
--------------------
*)
    let originResponseLambda =

        let lambdaFunctionArgs =
            Lambda.FunctionArgs(
                Handler = "index.handler",
                Runtime = "nodejs14.x",
                MemorySize = 512,
                Timeout = 5,
                Role = lambdaRole.Arn,
                Publish = true,
                Code =
                    input (
                        AssetArchive(
                            Map<string, AssetOrArchive>
                                [ (".",
                                   FileArchive(
                                       "../lambda/origin-response-function/dist"
                                   )) ]
                        )
                    )
            )
        
        let lambdaOptions = 
            let customResourceOptions = CustomResourceOptions()
            customResourceOptions.Provider <- Provider("useast1", ProviderArgs (Region = "us-east-1" ));
            customResourceOptions

        Lambda.Function("imageResizerLambda", lambdaFunctionArgs, lambdaOptions )


    (*
--------------------
CloudFront
--------------------
*)
    let cloudFrontDistribution =



        let s3OriginConfigArgs =
            DistributionOriginS3OriginConfigArgs(
                OriginAccessIdentity = originAccessIdentity.CloudfrontAccessIdentityPath
            )


        let originArgs =
            DistributionOriginArgs(
                DomainName = bucket.BucketRegionalDomainName,
                OriginId = "myS3Origin",
                S3OriginConfig = s3OriginConfigArgs
            )

        let viewerCertificate =
            DistributionViewerCertificateArgs(CloudfrontDefaultCertificate = true)

        let forwardeValueCookies =
            DistributionDefaultCacheBehaviorForwardedValuesCookiesArgs(Forward = "none")

        let forwardedValuesArgs =
            DistributionDefaultCacheBehaviorForwardedValuesArgs(
                QueryString = true,
                QueryStringCacheKeys = inputList [ input "d" ],
                Cookies = forwardeValueCookies
            )

        let lambdaOriginResponseAssociation = DistributionDefaultCacheBehaviorLambdaFunctionAssociationArgs(
            EventType = "origin-response",
            LambdaArn = Output.Format($"{originResponseLambda.Arn}:{originResponseLambda.Version}"),
            IncludeBody = false
        )

        let defaultCacheBehaviorArgs =
            DistributionDefaultCacheBehaviorArgs(
                AllowedMethods =
                    inputList [ input "GET"
                                input "HEAD"
                                input "OPTIONS" ],
                CachedMethods = inputList [ input "GET"; input "HEAD" ],
                TargetOriginId = "myS3Origin",
                ForwardedValues = forwardedValuesArgs,
                ViewerProtocolPolicy = "redirect-to-https",
                MinTtl = 100,
                DefaultTtl = 3600,
                MaxTtl = 86400,
                SmoothStreaming = false,
                Compress = true,
                LambdaFunctionAssociations = inputList [input lambdaOriginResponseAssociation]
            )

        let geoRestrictions =
            DistributionRestrictionsGeoRestrictionArgs(RestrictionType = "none")

        let restrictionArgs =
            DistributionRestrictionsArgs(GeoRestriction = geoRestrictions)

        let cloudFrontDistributionArgs =
            DistributionArgs(
                Origins = originArgs,
                Enabled = true,
                Comment = "istribution for content delivery",
                DefaultRootObject = "index.html",
                PriceClass = "PriceClass_100",
                ViewerCertificate = viewerCertificate,
                DefaultCacheBehavior = defaultCacheBehaviorArgs,
                Restrictions = restrictionArgs
            )

        Distribution("imageResizerDistribution", cloudFrontDistributionArgs)

    (*
--------------------
Exports
--------------------
*)
    // dict [ ("bucketName", bucket.Id :> obj) ]
    dict [ ("bucketName", bucket.Id :> obj)
           ("distribution", cloudFrontDistribution.Id :> obj) ]

[<EntryPoint>]
let main _ = Deployment.run infra
