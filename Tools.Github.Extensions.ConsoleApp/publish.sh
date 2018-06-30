#!/bin/bash
output=publish
config=Release

dotnet publish -c $config -o $output/linux-x64 -r linux-x64 --self-contained
dotnet publish -c $config -o $output/win-x64 -r win-x64 --self-contained
