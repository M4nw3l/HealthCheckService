# HealthCheckService Docker image bake file.
variable "TAG" {
  default = "latest"
}

variable "BUILD_CONFIGURATION" {
  default = "Release"
}

target "HealthCheckService" {
    context = "."
    dockerfile = "HealthCheckService/Dockerfile"
    args = {
      BUILD_CONFIGURATION="${BUILD_CONFIGURATION}"
    }
    tags = [ "healthcheckservice:${TAG}" ]
}

group "default" {
  targets = [ "HealthCheckService" ]
}