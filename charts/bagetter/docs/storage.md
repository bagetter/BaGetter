# Storage Configuration

BaGetter can persist packages to several backends. Set `bagetter.storage.type` to select the implementation and populate the corresponding nested block (`bagetter.storage.<provider>`).

## FileSystem (Default)

Stores packages on the cluster’s persistent volume. A PVC is created automatically when `storage.persistence.enabled` is true.

```yaml
bagetter:
  storage:
    type: FileSystem
    fileSystem:
      path: /srv/bagetter/packages
storage:
  persistence:
    enabled: true
    size: 50Gi
    storageClassName: fast-ssd
    mountPath: /srv/bagetter
```

## AWS S3

Reference AWS credentials from a Kubernetes secret (recommended). `assumeRoleArn` and `useInstanceProfile` cover IRSA or node role scenarios.

```bash
kubectl create secret generic bagetter-aws-storage \
  --from-literal=access-key=AKIA... \
  --from-literal=secret-key=abc123...
```

```yaml
bagetter:
  storage:
    type: AwsS3
    awsS3:
      bucket: my-bagetter-packages
      region: us-east-1
      prefix: releases/
      endpoint: https://s3.us-east-1.amazonaws.com # optional for AWS, required for S3-compatible endpoints
      existingSecret:
        name: bagetter-aws-storage
        keys:
          accessKey: access-key
          secretKey: secret-key
storage:
  persistence:
    enabled: false
```

## Azure Blob Storage

Provide either a full connection string or account credentials. The example below sources the connection string from a secret.

```bash
kubectl create secret generic bagetter-azure-storage \
  --from-literal=connection-string="DefaultEndpointsProtocol=..."
```

```yaml
bagetter:
  storage:
    type: AzureBlobStorage
    azureBlobStorage:
      container: bagetter
      existingSecret:
        name: bagetter-azure-storage
        keys:
          connectionString: connection-string
storage:
  persistence:
    enabled: false
```

## Google Cloud Storage

Mount a service-account JSON key into the pod and point BaGetter at the GCS bucket.

```bash
kubectl create secret generic bagetter-gcp-key \
  --from-file=credentials.json=./gcp-service-account.json
```

```yaml
bagetter:
  storage:
    type: GoogleCloud
    googleCloud:
      bucketName: bagetter-artifacts
      projectId: my-gcp-project
      serviceAccountSecret:
        name: bagetter-gcp-key
        key: credentials.json
        mountPath: /var/secrets/gcp
        fileName: credentials.json
storage:
  persistence:
    enabled: false
```

## Aliyun OSS & Tencent COS

Both Chinese cloud providers follow the same pattern: define credentials inline or via secrets and set provider-specific options (bucket, prefix, regions). Review `values.yaml` for complete key names.

## Null Storage

Use `type: Null` to disable package persistence entirely—useful for API-only or read-through cache setups where storage is handled externally.
