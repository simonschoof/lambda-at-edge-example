
docker build --tag amazonlinux:nodejs .  

docker run --rm --volume ${PWD}:/build amazonlinux:nodejs /bin/bash -c "source ~/.bashrc; npm init -f -y; rm -rf node_modules; npm install --ignore-scripts; npm rebuild sharp; npm run build"