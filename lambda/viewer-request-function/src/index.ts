import { CloudFrontRequest } from "aws-lambda";

interface ResizeParameters {
    width?: number;
    height?: number;
}

const AllowedDimensions = {
    maxWidth: 1000,
    maxHeight: 1000,
}

export async function handler(event: { Records: { cf: { request: any; } }[]; }): Promise<CloudFrontRequest> {
    console.log("Entering viewer request");
    const request = event.Records[0].cf.request;
    const urlsSearchParams = new URLSearchParams(request.querystring);

    console.log("Fetching image url", request.uri);

    const params = parseParams(urlsSearchParams);

    if (!validateParams(params)) {
        console.log("Provided dimensions: width: " + params.width + " height: " + params.height);
        console.log("Request querystring: ", request.querystring);

        request.querystring = `width=${params.width}&height=${params.height}`;
        console.log("New request querystring: ", request.querystring);

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

    if (widthString === null || heightString === null) {
        const resizerParams: ResizeParameters = {
            width: undefined,
            height: undefined,
        }
        return resizerParams
    }

    const width: number = (parseInt(widthString, 10) || AllowedDimensions.maxWidth) > AllowedDimensions.maxWidth ?
        AllowedDimensions.maxWidth : parseInt(widthString, 10);
    const height: number = (parseInt(heightString, 10) || AllowedDimensions.maxHeight) > AllowedDimensions.maxHeight ?
        AllowedDimensions.maxHeight : parseInt(heightString, 10);

    const resizerParams: ResizeParameters = {
        width: width,
        height: height,
    }
    return resizerParams

}

function validateParams(params: ResizeParameters) {
    return !params.width || !params.height || params.width <= 0 || params.height <= 0;
}