#!/bin/bash
config=Release

if [ -n "$1" ]
then
  suffix="-$1"
else
  suffix=""
fi

cd *ConsoleApp
for runtime in 'linux-x64' 'win-x64'
do
  output=publish/$runtime
  filename=publish/github-ext$suffix.$runtime.zip

  dotnet publish -c $config -o $output -r $runtime
  zip -9DmrT $filename $output
done

