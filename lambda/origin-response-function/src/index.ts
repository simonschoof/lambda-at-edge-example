import { CloudFrontResultResponse } from "aws-lambda";
import { GetObjectCommand, GetObjectCommandInput, GetObjectCommandOutput, S3Client } from "@aws-sdk/client-s3";
import sharp from "sharp"; 

const BUCKET_NAME = "images-76b39297-2c72-426d-8c2e-98dc34bfcbe9-eu-central-1";

export async function handler(event: { Records: { cf: { response: any; request: any; } }[]; }): Promise<CloudFrontResultResponse> {
    console.log("Entering origin response function");
    const { response, request } = event.Records[0].cf

    if (response.status !== '200') {
        console.log("Response status is not 200, returning");
        return response;
    }

    if (request.querystring === '') {
        console.log("No querystring, returning");
        return response;
    }

    const query = new URLSearchParams(request.querystring);
    const width = parseInt(query.get('width')!!, 10);
    const height = parseInt(query.get('height')!!, 10);

    console.log("Resizing image to", width, height);

    // 1. Get the image from S3
    // 2. Resize the image
    // 3. Return the image to CloudFront

    return response
}