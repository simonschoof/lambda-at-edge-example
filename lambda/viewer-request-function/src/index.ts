import { CloudFrontRequest } from "aws-lambda";

export async function handler(event: { Records: { cf: { request: any; } }[]; }): Promise<CloudFrontRequest> {
    return event.Records[0].cf.request;
}