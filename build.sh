#!/bin/bash

for $arg in $@; do
  case $arg in
    --target )
      target=$2
      shift
      shift
      ;;
    --release )
      target="Release"
      shift
      ;;
    --debug )
      target="Debug"
      shift
      ;;
  esac
done

# Build project
if [[ -n target ]]; then
  # http://stackoverflow.com/questions/17628660/how-can-i-use-xbuild-to-build-release-binary
  xbuild /p:Configuration=$target KeePassFaviconDownloader.sln
else
  # Build defaults
  xbuild KeePassFaviconDownloader.sln
fi

# Create temporary directory
KPTempDir="./plugin"
rm -rf "$KPTempDir"
mkdir "$KPTempDir"
# Copy files
cp ./HtmlAgilityPack.dll "$KPTempDir/"
cp ./KeePassFaviconDownloader.csproj "$KPTempDir/"
cp -r ./Properties "$KPTempDir/"

# Build PLGX
mono /usr/lib/keepass2/KeePass.exe --plgx-create "$KPTempDir"

# Cleanup
rm -rf "$KPTempDir"
mv "$KPTempDir.plgx" "./KeePassFaviconDownloader.plgx"

# Create debian package
PKG_PATH="./package"
VPATH="/usr/lib/keepass2/plugins"
rm -rf $PKG_PATH
mkdir -p $PKG_PATH$VPATH
mv ./KeePassFaviconDownloader.plgx $PKG_PATH$VPATH/
# TODO actual packaging

# Cleanup
#rm -rf $PKG_PATH

