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

        let bucketName =
            "images-76b39297-2c72-426d-8c2e-98dc34bfcbe9-eu-central-1"

        let bucketArgs =
            BucketArgs(Acl = "private", BucketName = bucketName)

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
                Principals =
                    inputList [ input lambdaPrincipal
                                input cloudFrontPrincipal ],
                Actions = inputList [ input "s3:GetObject" ],
                Resources =
                    inputList [ io bucket.Arn
                                io (Output.Format($"{bucket.Arn}/*")) ]
            )

        let listBucketStatement =
            GetPolicyDocumentStatementInputArgs(
                Principals = inputList [ input lambdaPrincipal ],
                Actions =
                    inputList [ input "s3:ListBucket" ],
                Resources =
                    inputList [ io bucket.Arn
                                io (Output.Format($"{bucket.Arn}/*")) ]
            )


        let policyDocumentInvokeArgs =
            GetPolicyDocumentInvokeArgs(
                Statements =
                    inputList [ input getObjectStatement
                                input listBucketStatement ]
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
    let lambdaOptions =
        let customResourceOptions = CustomResourceOptions()
        customResourceOptions.Provider <- Provider("useast1", ProviderArgs(Region = "us-east-1"))
        customResourceOptions

    let viewerRequestLambda =

        let lambdaFunctionArgs =
            Lambda.FunctionArgs(
                Handler = "index.handler",
                Runtime = "nodejs14.x",
                MemorySize = 128,
                Timeout = 1,
                Role = lambdaRole.Arn,
                Publish = true,
                Code =
                    input (
                        AssetArchive(
                            Map<string, AssetOrArchive> [ (".", FileArchive("../lambda/viewer-request-function/dist")) ]
                        )
                    )
            )

        Lambda.Function("viewerRequestLambda", lambdaFunctionArgs, lambdaOptions)

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
                                [ (".", FileArchive("../lambda/origin-response-function/dist"))
                                  ("node_modules", FileArchive("../lambda/origin-response-function/node_modules")) ]
                        )
                    )
            )

        Lambda.Function("originResponseLambda", lambdaFunctionArgs, lambdaOptions)

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
                QueryStringCacheKeys =
                    inputList [ input "width"
                                input "height" ],
                Cookies = forwardeValueCookies
            )

        let lambdaViewerRequestAssociation =
            DistributionDefaultCacheBehaviorLambdaFunctionAssociationArgs(
                EventType = "viewer-request",
                LambdaArn = Output.Format($"{viewerRequestLambda.Arn}:{viewerRequestLambda.Version}"),
                IncludeBody = false
            )

        let lambdaOriginResponseAssociation =
            DistributionDefaultCacheBehaviorLambdaFunctionAssociationArgs(
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
                LambdaFunctionAssociations =
                    inputList [ input lambdaViewerRequestAssociation
                                input lambdaOriginResponseAssociation ]
            )

        let geoRestrictions =
            DistributionRestrictionsGeoRestrictionArgs(RestrictionType = "none")

        let restrictionArgs =
            DistributionRestrictionsArgs(GeoRestriction = geoRestrictions)

        let cloudFrontDistributionArgs =
            DistributionArgs(
                Origins = originArgs,
                Enabled = true,
                Comment = "Distribution for content delivery",
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
    dict [ ("BucketName", bucket.Id :> obj)
           ("Distribution", cloudFrontDistribution.Id :> obj)
           ("LambdaRole", lambdaRole.Arn :> obj)
           ("OriginAccessIdentity", originAccessIdentity.IamArn :> obj)
           ("ViewerRequestLambda", viewerRequestLambda.Arn :> obj)
           ("OriginResponseLambda", originResponseLambda.Arn :> obj)
           ("ImageBucketPolicy", imageBucketPolicy.Id :> obj) ]

[<EntryPoint>]
let main _ = Deployment.run infra
