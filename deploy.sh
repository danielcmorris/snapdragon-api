#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# Snapdragon API — Cloud Run Setup & Deploy Script
#
# Run once to set up infrastructure, then Cloud Build handles subsequent deploys.
# Usage: ./deploy.sh [--first-run]
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

PROJECT_ID="snapdragonerp"
REGION="us-central1"
SERVICE_NAME="snapdragon-api"
SERVICE_ACCOUNT="snapdragon@${PROJECT_ID}.iam.gserviceaccount.com"
REPO="snapdragon"
IMAGE="us-central1-docker.pkg.dev/${PROJECT_ID}/${REPO}/${SERVICE_NAME}"

echo "▶ Project: ${PROJECT_ID}  Region: ${REGION}"
gcloud config set project "${PROJECT_ID}"

# ── First-run: enable APIs and create infrastructure ─────────────────────────
if [[ "${1:-}" == "--first-run" ]]; then
  echo ""
  echo "── Enabling required GCP APIs ──────────────────────────────────────────"
  gcloud services enable \
    run.googleapis.com \
    artifactregistry.googleapis.com \
    cloudbuild.googleapis.com \
    secretmanager.googleapis.com \
    sqladmin.googleapis.com \
    iam.googleapis.com

  echo ""
  echo "── Creating Artifact Registry repository ───────────────────────────────"
  gcloud artifacts repositories create "${REPO}" \
    --repository-format=docker \
    --location="${REGION}" \
    --description="Snapdragon ERP Docker images" \
    || echo "  (already exists — skipping)"

  echo ""
  echo "── Granting service account permissions ────────────────────────────────"
  # BigQuery
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/bigquery.dataViewer"
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/bigquery.jobUser"
  # Cloud Storage
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/storage.objectAdmin"
  # Vertex AI / Discovery Engine
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/aiplatform.user"
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/discoveryengine.editor"
  # Secret Manager
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/secretmanager.secretAccessor"
  # Cloud SQL (if used)
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${SERVICE_ACCOUNT}" \
    --role="roles/cloudsql.client"

  echo ""
  echo "── Grant Cloud Build permission to deploy Cloud Run ────────────────────"
  CLOUDBUILD_SA="$(gcloud projects describe ${PROJECT_ID} --format='value(projectNumber)')@cloudbuild.gserviceaccount.com"
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${CLOUDBUILD_SA}" \
    --role="roles/run.admin"
  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${CLOUDBUILD_SA}" \
    --role="roles/iam.serviceAccountUser"

  echo ""
  echo "── Creating Secret Manager secrets ─────────────────────────────────────"
  echo "  You will be prompted to enter each secret value."
  echo ""

  create_secret() {
    local name="$1"
    local prompt="$2"
    echo -n "  ${prompt}: "
    read -rs value
    echo ""
    if gcloud secrets describe "${name}" --project="${PROJECT_ID}" &>/dev/null; then
      echo "${value}" | gcloud secrets versions add "${name}" --data-file=-
      echo "  ✓ Updated secret: ${name}"
    else
      echo "${value}" | gcloud secrets create "${name}" \
        --data-file=- --replication-policy=automatic
      echo "  ✓ Created secret: ${name}"
    fi
  }

  create_secret "snapdragon-db-connection" \
    "PostgreSQL connection string (e.g. Host=...;Port=5432;Database=snapdragon;Username=...;Password=...)"

  create_secret "snapdragon-jwt-key" \
    "JWT signing key (random string, 32+ chars)"

  create_secret "snapdragon-google-client-id" \
    "Google OAuth Client ID"

  create_secret "snapdragon-places-api-key" \
    "Google Places API key"

  echo ""
  echo "── Connect Cloud Build to GitHub ───────────────────────────────────────"
  echo "  Visit the Cloud Build console to connect your GitHub repo:"
  echo "  https://console.cloud.google.com/cloud-build/triggers;region=${REGION}"
  echo ""
  echo "  Create a trigger with:"
  echo "    Repository: danielcmorris/snapdragon-api"
  echo "    Branch:     ^main\$"
  echo "    Build config: cloudbuild.yaml"
  echo ""
  echo "First-run setup complete. Re-run without --first-run to deploy manually."
  exit 0
fi

# ── Manual deploy (no --first-run) ───────────────────────────────────────────
echo ""
echo "── Authenticating Docker with Artifact Registry ────────────────────────"
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

echo ""
echo "── Building Docker image ────────────────────────────────────────────────"
COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "manual")
docker build -t "${IMAGE}:${COMMIT}" -t "${IMAGE}:latest" .

echo ""
echo "── Pushing image ────────────────────────────────────────────────────────"
docker push "${IMAGE}:${COMMIT}"
docker push "${IMAGE}:latest"

echo ""
echo "── Deploying to Cloud Run ───────────────────────────────────────────────"
gcloud run deploy "${SERVICE_NAME}" \
  --image="${IMAGE}:${COMMIT}" \
  --region="${REGION}" \
  --platform=managed \
  --service-account="${SERVICE_ACCOUNT}" \
  --allow-unauthenticated \
  --port=8080 \
  --memory=512Mi \
  --cpu=1 \
  --min-instances=0 \
  --max-instances=10 \
  --set-secrets="ConnectionStrings__Postgres=snapdragon-db-connection:latest,Jwt__Key=snapdragon-jwt-key:latest,GoogleAuth__ClientId=snapdragon-google-client-id:latest,GoogleCloud__PlacesApiKey=snapdragon-places-api-key:latest" \
  --set-env-vars="GoogleCloud__ProjectId=${PROJECT_ID},VertexAi__ProjectId=${PROJECT_ID},VertexAi__Location=${REGION},VertexAi__DataStoreId=snapdragonerp-datastore,VertexAi__ModelId=gemini-2.0-flash,GoogleCloud__Buckets__Invoice=invoice-repository,GoogleCloud__Buckets__Object=object-repository,GoogleCloud__Buckets__Proposal=proposal-repository"

echo ""
echo "✓ Deployed ${IMAGE}:${COMMIT}"
gcloud run services describe "${SERVICE_NAME}" \
  --region="${REGION}" \
  --format="value(status.url)" | xargs echo "  URL:"
