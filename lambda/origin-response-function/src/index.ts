import { CloudFrontResultResponse } from "aws-lambda";

export async function handler(event: { Records: { cf: { response: any; request: any; } }[]; }): Promise<CloudFrontResultResponse> {
    return event.Records[0].cf.response;
}