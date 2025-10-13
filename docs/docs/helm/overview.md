---
id: overview
slug: /helm/overview
title: BaGetter Helm Chart Overview
sidebar_label: Overview
sidebar_position: 1
---

The BaGetter chart packages an opinionated Kubernetes deployment for the open-source BaGetter NuGet server. Values are organized by concern to make customization predictable and compatible with schema validation.

## Configuration Structure

| Section | Keys | Purpose |
|---------|------|---------|
| `general` | `replicaCount`, `imagePullSecrets` | Global runtime basics (scaling baseline, registry credentials). |
| `metadata` | `nameOverride`, `fullnameOverride` | Override default resource naming. |
| `serviceAccount` | `create`, `name`, `annotations`, `automount` | Configure the service account bound to the pod. |
| `workload` | `podAnnotations`, `podLabels`, `podSecurityContext`, `containerSecurityContext` | Targeted pod/container level metadata and security settings. |
| `bagetter` | `apiKey`, `image.*`, `env[]`, `resources.*`, `pathBase`, `urls`, `maxPackageSizeGiB` | Application specific knobs including container image, core server toggles, environment variables, and resource limits. |
| `bagetter.storage` | `type`, provider-specific sub-sections (`fileSystem`, `azureBlobStorage`, `awsS3`, etc.) | Select and configure the backing package storage service. |
| `bagetter.database` | `type`, provider-specific connection settings (`sqlite`, `mysql`, `postgresql`, etc.) | Configure the metadata database backend and optionally source connection strings from secrets. |
| `bagetter.retention` | `maxMajorVersions`, `maxMinorVersions`, `maxPatchVersions`, `maxPrereleaseVersions` | Configure automatic package clean-up limits for SemVer segments. |
| `bagetter.mirror` | `enabled`, `packageSource`, `legacy`, `packageDownloadTimeoutSeconds`, `authentication.*` | Enable read-through caching and supply upstream credentials or headers. |
| `bagetter.authentication` | `apiKeys[]`, `credentials[]` | Configure additional push API keys and basic-auth credentials (with optional secret references). |
| `bagetter.statistics` | `enableStatisticsPage`, `listConfiguredServices` | Control the statistics dashboard exposure and contents. |
| `bagetter.healthCheck` | `path`, `statusPropertyName` | Customize the health probe endpoint path and JSON payload. |
| `networking.service` | `type`, `port`, `targetPort` | Cluster networking exposure for BaGetter. |
| `networking.ingress` | `enabled`, `className`, `annotations`, `hosts[]`, `tls[]` | Public ingress configuration for HTTP/S routing. |
| `health` | `livenessProbe`, `readinessProbe` | Fine-tune container probe behavior. |
| `scaling.autoscaling` | `enabled`, `minReplicas`, `maxReplicas`, `targetCPUUtilizationPercentage`, `targetMemoryUtilizationPercentage` | HorizontalPodAutoscaler settings. |
| `storage.persistence` | `enabled`, `size`, `storageClassName`, `mountPath`, `accessModes` | Persistent volume claim management for repository storage. |
| `storage.volumes`, `storage.volumeMounts` | Additional Kubernetes volumes/mounts appended alongside the PVC. |
| `scheduling` | `nodeSelector`, `tolerations`, `affinity` | Advanced scheduling preferences and constraints. |

The chart also maintains legacy top-level keys (`replicaCount`, `service`, `persistence`, etc.) for compatibility with earlier releases. New deployments should prefer the structured sections above. Validation is enforced through `values.schema.json` to catch typos early.

## Getting Started

1. Add the repository: `helm repo add bagetter https://helm.bagetter.com`
2. Install with defaults: `helm install my-bagetter bagetter/bagetter`
3. Copy `values.yaml` and tailor sections described above for your environment.

Continue with the [storage configuration](./storage.md), [database providers](./database.md), and [advanced configuration patterns](./configuration.md) guides for deep dives into specific areas.
