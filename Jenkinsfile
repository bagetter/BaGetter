// CD pipeline for the BaGetter NuGet mirror (the devop fork, vhafdal/BaGetter).
//
// Brings BaGetter into the same Jenkins + Harbor + Helm flow as DevOp.Nexus
// (it previously shipped via GitHub Actions -> GHCR and was deployed to k8s by
// hand). One run: build the image -> push to Harbor (registry.devop.is) -> helm
// upgrade the `bagetter` release in the cluster.
//
// Agent: provisions its OWN pod (the shared Jenkins cloud has no Docker daemon):
//   - dind  : docker:27-dind daemon (privileged) on tcp://localhost:2375, MTU 1450
//   - build : docker:27-cli -> talks to dind, builds + pushes the image
//   - helm  : alpine/k8s (helm + kubectl) for the deploy stage
//
// Credentials used (already exist in Jenkins for the platform pipeline):
//   devop-registry-credentials (user/pass) — docker login to registry.devop.is (robot$jenkins-push)
//   vcube-kubeconfig           (secret file, optional) — kube access if the agent SA can't deploy
//
// PREREQUISITES (one-time, see deployment templates/chart/bagetter/README-cd.md):
//   - Harbor pull secret in the `bagetter` namespace so the cluster can pull the
//     private registry.devop.is/nexus/bagetter image (robot$k8s-pull).
//   - The live deployment is NOT currently Helm-managed; the first `helm upgrade
//     --install` must adopt or replace the existing resources. Validate on a
//     scratch namespace (ENVIRONMENT=dev) before pointing at prod `bagetter`.

pipeline {
    agent {
        kubernetes {
            defaultContainer 'jnlp'
            yaml '''
apiVersion: v1
kind: Pod
spec:
  serviceAccountName: nexus-deployer
  containers:
    - name: dind
      image: docker:27-dind
      securityContext:
        privileged: true
      args: ["--mtu=1450"]
      env:
        - name: DOCKER_TLS_CERTDIR
          value: ""
      resources:
        requests:
          ephemeral-storage: 4Gi
          memory: 1Gi
      volumeMounts:
        - name: docker-storage
          mountPath: /var/lib/docker
    - name: build
      image: docker:27-cli
      command: ["cat"]
      tty: true
      env:
        - name: DOCKER_HOST
          value: tcp://localhost:2375
        - name: DOCKER_BUILDKIT
          value: "1"
    - name: helm
      image: alpine/k8s:1.30.7
      command: ["cat"]
      tty: true
  volumes:
    - name: docker-storage
      emptyDir: {}
'''
        }
    }

    parameters {
        choice(name: 'ENVIRONMENT', choices: ['prod', 'dev'],
               description: 'Target: prod -> namespace bagetter; dev -> namespace bagetter-dev (scratch, for validating the chart)')
        string(name: 'VERSION_OVERRIDE', defaultValue: '',
               description: 'Image tag to build/deploy. Blank = sha-<short git sha>, matching the existing tag style.')
        booleanParam(name: 'PUSH_IMAGE', defaultValue: true, description: 'Build and push the image to Harbor')
        booleanParam(name: 'DEPLOY', defaultValue: true, description: 'helm upgrade --install against the cluster')
    }

    environment {
        REGISTRY_HOST = 'registry.devop.is'
        IMAGE         = 'registry.devop.is/nexus/bagetter'
        CHART_DIR     = 'deployment templates/chart/bagetter'
    }

    stages {
        stage('Resolve version') {
            steps {
                script {
                    git_safe()
                    def v = params.VERSION_OVERRIDE?.trim()
                    if (!v) {
                        def sha = sh(returnStdout: true, script: "git rev-parse --short HEAD").trim()
                        v = "sha-${sha}"
                    }
                    env.VERSION = v
                    env.NAMESPACE = (params.ENVIRONMENT == 'dev') ? 'bagetter-dev' : 'bagetter'
                    echo "BaGetter version=${env.VERSION}  env=${params.ENVIRONMENT}  ns=${env.NAMESPACE}"
                }
            }
        }

        stage('Build & push image') {
            when { expression { params.PUSH_IMAGE } }
            steps {
                container('build') {
                    sh '''
                        apk add --no-cache git >/dev/null
                        echo "Waiting for the dind daemon..."
                        timeout 90 sh -c 'until docker info >/dev/null 2>&1; do sleep 2; done'
                    '''
                    withCredentials([usernamePassword(credentialsId: 'devop-registry-credentials',
                            usernameVariable: 'REG_USER', passwordVariable: 'REG_PASS')]) {
                        sh '''
                            echo "$REG_PASS" | docker login "$REGISTRY_HOST" -u "$REG_USER" --password-stdin
                            # The fork Dockerfile takes the build-time version via --build-arg Version.
                            # BuildKit auto-populates TARGETARCH (the Dockerfile builds for it).
                            # Image tag is VERSION (sha-* or override); the assembly Version
                            # build-arg must be valid semver, so fall back to 1.0.0 for sha builds.
                            docker build \
                              --build-arg Version="${VERSION_OVERRIDE:-1.0.0}" \
                              -t "$IMAGE:$VERSION" \
                              -t "$IMAGE:latest" \
                              .
                            docker push "$IMAGE:$VERSION"
                            docker push "$IMAGE:latest"
                        '''
                    }
                }
            }
        }

        stage('Helm deploy') {
            when { expression { params.DEPLOY } }
            steps {
                container('helm') {
                    withKube { deployHelm() }
                }
            }
        }
    }

    post {
        success { echo "BaGetter ${env.VERSION} deployed to ${params.ENVIRONMENT} (ns ${env.NAMESPACE})." }
        always  { container('build') { sh 'docker logout "$REGISTRY_HOST" || true' } }
    }
}

def git_safe() { sh "git config --global --add safe.directory '*' 2>/dev/null || true" }

def withKube(Closure body) {
    try {
        withCredentials([file(credentialsId: 'vcube-kubeconfig', variable: 'KUBECONFIG')]) { body() }
    } catch (ignored) {
        echo 'vcube-kubeconfig not set; using the agent in-cluster ServiceAccount.'
        body()
    }
}

def deployHelm() {
    sh '''
        helm dependency build "$CHART_DIR"
        helm upgrade --install bagetter "$CHART_DIR" \
          --namespace "$NAMESPACE" --create-namespace \
          -f "$CHART_DIR/values-prod.yaml" \
          --set controllers.bagetter.containers.bagetter.image.repository="$IMAGE" \
          --set controllers.bagetter.containers.bagetter.image.tag="$VERSION" \
          --wait --timeout 5m
        kubectl -n "$NAMESPACE" rollout status deploy/bagetter --timeout=3m
    '''
}
