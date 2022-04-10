import { CloudFrontResultResponse } from "aws-lambda";

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

    return response
}