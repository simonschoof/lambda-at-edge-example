FROM amazonlinux:2

WORKDIR /tmp
#install the dependencies
RUN yum -y install gcc-c++ && yum -y install findutils tar gzip

RUN touch ~/.bashrc && chmod +x ~/.bashrc

RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.1/install.sh | bash| bash

RUN source ~/.bashrc && nvm install 14

WORKDIR /build