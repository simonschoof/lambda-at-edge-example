import { CloudFrontResultResponse } from "aws-lambda";

export async function handler(event: {Records: {cf: {response: any; request: any;}}[];}): Promise<CloudFrontResultResponse>{
    console.log("hello lambda@edge");
    const {response, request} = event.Records[0].cf
    return response
}