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
Aliases and CPE-specific helpers expected by templates.
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

{{- define "cpe.lighthouseSeedNodes" -}}
{{- if .Values.lighthouse.enabled -}}
{{- $clusterName := .Values.akkaOptions.clusterName -}}
{{- $clusterPort := .Values.services.lighthouse.ports.akka -}}
{{- $seedHost := printf "%s-lighthouse" (include "cpe.fullname" .) -}}
{{- printf "akka.tcp://%s@%s:%v" $clusterName $seedHost $clusterPort | quote -}}
{{- else -}}
""
{{- end -}}
{{- end }}

{{- define "cpe.akkaOptionsSeedNodes" -}}
{{- if .Values.lighthouse.enabled }}
AkkaOptions__ClusterOptions__SeedNodes__0: {{ printf "akka.tcp://%s@%s-lighthouse:%v" .Values.akkaOptions.clusterName (include "cpe.fullname" .) .Values.services.lighthouse.ports.akka | quote }}
{{- end }}
{{- end }}
