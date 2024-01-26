---
sidebar_position: 2
---

export const CenterImg = ({children, color}) => (
  <p align="center">
    {children}
  </p>
);

# BaGetter

BaGetter (pronounced "ba getter") is a lightweight NuGet and symbol server. It is [open source](https://github.com/bagetter/BaGetter), cross-platform, and cloud ready!


<CenterImg>
  <img width="100%" src="https://user-images.githubusercontent.com/737941/50140219-d8409700-0258-11e9-94c9-dad24d2b48bb.png"/>
</CenterImg>

## Run BaGetter

You can run BaGetter on your preferred platform:

* [On your computer](Installation/local.md)
* [Docker](Installation/docker.md)
* [Azure](Installation/azure.md)
* [AWS](Installation/aws.md)
* [Google Cloud](Installation/gcp.md)
* [Alibaba Cloud (Aliyun)](Installation/aliyun.md)

## BaGetter SDK

You can also use the [`BaGetter.Protocol`](https://www.nuget.org/packages/BaGetter.Protocol) package to interact with a NuGet server. For more information, please refer to the [BaGetter SDK](Advanced/sdk.md) guide.
