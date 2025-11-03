---
id: upgrading
slug: /helm/upgrading
title: Upgrading the Chart
sidebar_label: Upgrading
sidebar_position: 6
---

When moving from chart versions `<1.6.0` to `>=1.6.0`, migrate custom values to the structured sections (`general`, `bagetter.storage`, etc.). Legacy top-level keys remain temporarily for backward compatibility but will be removed in a future major release.

Recommended workflow:

1. Pull the latest `values.yaml` and merge your overrides into the new structure.
2. Run `helm lint` and `helm unittest charts/bagetter` with your values file.
3. Use `helm diff upgrade` (via [helm-diff](https://github.com/databus23/helm-diff)) to preview changes before applying them to production clusters.

Document chart upgrade notes in your repository's release process so downstream consumers stay aligned with chart behavior.
