# gensam

[![NuGet](https://img.shields.io/nuget/v/gensam.svg?style=flat-square)](https://www.nuget.org/packages/gensam/)
[![Build status](https://ci.appveyor.com/api/projects/status/twe76tp4qo5newex/branch/master?svg=true)](https://ci.appveyor.com/project/muazweb/gensam/history/branch/master)

`gensam` is a command line tool to help build serverless apps on AWS by generating an [AWS SAM](https://aws.amazon.com/serverless/sam/) template file from draw.io (diagrams.net) diagrams using the AWS19 icons.

## Installing

`gensam` is a [.NET Core Global Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) and can be installed using command:

```sh
dotnet tool install --global gensam
```