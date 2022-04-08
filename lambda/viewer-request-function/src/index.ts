import { CloudFrontRequest } from "aws-lambda";
import querystring from 'querystring';

const variables = {
    allowedDimension : [ {w:100,h:100}, {w:200,h:200}, {w:300,h:300}, {w:400,h:400} ],
    defaultDimension : {w:200,h:200},
    variance: 20,
    webpExtension: 'webp'
};

export async function handler(event: {Records: {cf: {request: any;}}[];}): Promise<CloudFrontRequest>{
    console.log("hello cf request");
    const request = event.Records[0].cf.request;
    const headers = request.headers;

    // parse the querystrings key-value pairs. In our case it would be d=100x100
    const params = new URLSearchParams(request.querystring);

    // fetch the uri of original image
    let fwdUri = request.uri;

    // get the dimension of the image
    let dimension = params.get('d');   
    // if there is no dimension attribute, just pass the request
    if(dimension === null){
        console.log("no dimension params");
        return request;
    }
    // read the dimension parameter value = width x height and split it by 'x'
    const dimensionMatch = dimension.split("x");

    // set the width and height parameters
    let width = parseInt(dimensionMatch[0],10);
    let height = parseInt(dimensionMatch[1],10);

    console.log("width: " + width + " height: " + height);

    // parse the prefix, image name and extension from the uri.
    // In our case /images/image.jpg

    const match = fwdUri.match(/(.*)\/(.*)\.(.*)/);

    let prefix = match[1];
    let imageName = match[2];
    let extension = match[3];

    // define variable to be set to true if requested dimension is allowed.
    let matchFound = false;

    // calculate the acceptable variance. If image dimension is 105 and is within acceptable
    // range, then in our case, the dimension would be corrected to 100.
    let variancePercent = (variables.variance/100);

    for (let dimension of variables.allowedDimension) {
        let minWidth = dimension.w - (dimension.w * variancePercent);
        let maxWidth = dimension.w + (dimension.w * variancePercent);
        if(width >= minWidth && width <= maxWidth){
            width = dimension.w;
            height = dimension.h;
            matchFound = true;
            break;
        }
    }
    // if no match is found from allowed dimension with variance then set to default
    //dimensions.
    if(!matchFound){
        width = variables.defaultDimension.w;
        height = variables.defaultDimension.h;
    }

    // read the accept header to determine if webP is supported.
    let accept = headers['accept']?headers['accept'][0].value:"";

    let url = [];
    // build the new uri to be forwarded upstream
    url.push(prefix);
    url.push(width+"x"+height);
  
    // check support for webp
    if (accept.includes(variables.webpExtension)) {
        url.push(variables.webpExtension);
    }
    else{
        url.push(extension);
    }
    url.push(imageName+"."+extension);

    fwdUri = url.join("/");

    // final modified url is of format /images/200x200/webp/image.jpg
    request.uri = fwdUri;
    return request;
}