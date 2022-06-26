import { CloudFrontRequest } from "aws-lambda";

interface ResizeParameters {
    width: number;
    height: number;
}

const AllowedDimensions = {
    maxWidth: 10000,
    maxHeight: 10000,
}

export async function handler(event: { Records: { cf: { request: any; } }[]; }): Promise<CloudFrontRequest> {
    console.log("Entering viewer request");
    const request = event.Records[0].cf.request;
    const urlsSearchParams = new URLSearchParams(request.querystring);

    console.log("Fetching image url", request.uri);

    const params = parseParams(urlsSearchParams);

    if (paramsValid(params)) {
        console.log("Provided dimensions: width: " + params.width + " height: " + params.height);
        console.log("Request querystring: ", request.querystring);
    } else {
        console.log("No dimension or invalid dimension params found, returning original image");
        request.querystring = "";
        console.log("New request querystring: ", request.querystring);
    }

    return request;
}

function parseParams(params: URLSearchParams): ResizeParameters {
    const widthString = params.get('width');
    const heightString = params.get('height');

    const width: number = widthString ? parseInt(widthString, 10) : NaN;
    const height: number = heightString ? parseInt(heightString, 10): NaN; 

    const resizerParams: ResizeParameters = {
        width: width,
        height: height,
    }
    return resizerParams

}

function paramsValid(params: ResizeParameters) {
    return !isNaN(params.width) 
    && !isNaN(params.height)
    && params.width > 0 
    && params.height > 0
    && params.width <= AllowedDimensions.maxWidth
    && params.height <= AllowedDimensions.maxHeight;
}