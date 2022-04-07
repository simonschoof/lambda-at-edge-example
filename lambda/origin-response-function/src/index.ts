import { CloudFrontResultResponse } from "aws-lambda";

export async function handler(event: {Records: {cf: {response: any; request: any;}}[];}): Promise<CloudFrontResultResponse>{
    console.log("hello lambda@edge");
    const {response, request} = event.Records[0].cf
    console.log("origin-response-function-request:", request);
    console.log("origin-response-function-response:", response);
    return response
}