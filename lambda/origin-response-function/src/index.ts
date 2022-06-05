import { CloudFrontResultResponse } from "aws-lambda";
import { GetObjectCommand, GetObjectCommandInput, GetObjectCommandOutput, S3Client } from "@aws-sdk/client-s3";
import { Readable } from "stream";
import sharp from "sharp"; 

const BUCKET_NAME = "images-76b39297-2c72-426d-8c2e-98dc34bfcbe9-eu-central-1";

export async function handler(event: { Records: { cf: { response: any; request: any; } }[]; }): Promise<CloudFrontResultResponse> {
    console.log("Entering origin response function");
    const { response, request } = event.Records[0].cf

    if (response.status !== '200') {
        console.log("Response status is not 200, returning");
        return response;
    }

    console.log("Response status", response.status);

    if (request.querystring === '') {
        console.log("No querystring, returning");
        return response;
    }

    const query = new URLSearchParams(request.querystring);
    const width = parseInt(query.get('width')!!, 10);
    const height = parseInt(query.get('height')!!, 10);

    console.log("Resizing image to", width, height);

    // 1. Get the image from S3
    const s3Key = request.uri.substring(1);
    console.log("S3 key:", s3Key);
    const cmd = new GetObjectCommand({ Key: s3Key, Bucket: BUCKET_NAME });
    const s3 = new S3Client({region: 'eu-central-1'});
    
    const s3Response = await s3.send<GetObjectCommandInput, GetObjectCommandOutput>(cmd);

    if (!s3Response.Body) {
        throw new Error(`No body in response. Bucket: ${BUCKET_NAME}, Key: ${s3Key}`);
    }

   const imageBuffer = Buffer.from(await new Promise<Buffer>((resolve, reject) => {
        const chunks:any = [];
        s3Response.Body.on('data', (chunk: any) =>  chunks.push(chunk));
        s3Response.Body.on('error', reject);
        s3Response.Body.on('end', () => resolve(Buffer.concat(chunks)));
    }));      

  
    // 2. Resize the image
    const resizedImage = await sharp(imageBuffer).resize({ width, height }).toBuffer()
    const resizedImageResponse = resizedImage.toString('base64');

    // 3. Return the image to CloudFront
    return {
        status : '200',
        body : resizedImageResponse,
        bodyEncoding : 'base64'
    }
}