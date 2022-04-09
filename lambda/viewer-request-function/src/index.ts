import { CloudFrontRequest } from "aws-lambda";

type ImageFormat = 'webp' | 'png' | 'jpg';

interface ResizeParameters {
    width?: number;
    height?: number;
    format: ImageFormat;
}

const AllowedDimensions = {
    maxWidth: 1000,
    maxHeight: 1000,
}

export async function handler(event: { Records: { cf: { request: any; } }[]; }): Promise<CloudFrontRequest> {
    console.log("Entering viewer request");

    const request = event.Records[0].cf.request;
    const urlsSearchParams = new URLSearchParams(request.querystring);
    const fwdUri = request.uri;

    const params = parseParams(urlsSearchParams, fwdUri);

    if (!params.width || !params.height) {
        console.log("No dimension params found, returning original image");
        return request;
    }
    console.log("Provided dimensions: width: " + params.width + " height: " + params.height);
    console.log("Request querystring: ", request.querystring);

    request.querystring = `width=${params.width}&height=${params.height}&format=${params.format}`;
    console.log("New request querystring: ", request.querystring);

    return request;
}

function parseParams(params: URLSearchParams, uri: string): ResizeParameters {
    const widthString = params.get('width');
    const heightString = params.get('height');

    const format = (params.get('format') || uri.split('.')[1]) as ImageFormat;

    if (widthString === null || heightString === null) {
        const resizerParams: ResizeParameters = {
            width: undefined,
            height: undefined,
            format
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
        format
    }
    return resizerParams

}