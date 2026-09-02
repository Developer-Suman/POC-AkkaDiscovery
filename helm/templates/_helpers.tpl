{{/*
Expand the name of the chart.
*/}}
{{- define "helm.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "helm.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "helm.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "helm.labels" -}}
helm.sh/chart: {{ include "helm.chart" . }}
{{ include "helm.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "helm.selectorLabels" -}}
app.kubernetes.io/name: {{ include "helm.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "helm.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "helm.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Alias helpers for legacy templates that reference "cpe.*" helper names.
These delegate to the canonical "helm.*" helpers defined above.
*/}}
{{- define "cpe.name" -}}
{{- include "helm.name" . -}}
{{- end }}

{{- define "cpe.fullname" -}}
{{- include "helm.fullname" . -}}
{{- end }}

{{- define "cpe.chart" -}}
{{- include "helm.chart" . -}}
{{- end }}

{{- define "cpe.labels" -}}
{{- include "helm.labels" . -}}
{{- end }}

{{- define "cpe.selectorLabels" -}}
{{- include "helm.selectorLabels" . -}}
{{- end }}

{{- define "cpe.serviceAccountName" -}}
{{- include "helm.serviceAccountName" . -}}
{{- end }}


{{/*
Stub for akkaOptions seed nodes helper: return empty string or configured value
*/}}
{{- define "cpe.akkaOptionsSeedNodes" -}}
{{- $ao := index .Values "akkaOptions" -}}
{{- if and $ao (index $ao "seedNodes") -}}
AkkaOptions__SeedNodes: {{ printf "%q" (index $ao "seedNodes") }}
{{- end -}}
{{- end }}
