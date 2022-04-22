export SHARP_IGNORE_GLOBAL_LIBVIPS=true
rm -rf node_modules/sharp 
npm install  --unsafe-perm --arch=x64 --platform=linux --target=14.19.0 sharp