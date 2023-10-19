FROM gradle:jdk17-alpine as compile
COPY . /home/source/java
WORKDIR /home/source/java
# Default gradle user is `gradle`. We need to add permission on working directory for gradle to build.
USER root
RUN chown -R gradle /home/source/java
USER gradle
RUN gradle clean build

FROM eclipse-temurin:17-jre
WORKDIR /home/application/java
COPY --from=compile /home/source/java/build/libs/currency-rate-api.jar .
COPY --from=compile /home/source/java/applicationinsights.json .
ADD https://github.com/open-telemetry/opentelemetry-java-instrumentation/releases/download/v1.30.0/opentelemetry-javaagent.jar ./
ADD https://github.com/microsoft/ApplicationInsights-Java/releases/download/3.4.17/applicationinsights-agent-3.4.17.jar ./
ENV OTEL_SERVICE_VERSION=$DOCKER_TAG
EXPOSE 8080
ENTRYPOINT [ "java", "-jar", "/home/application/java/currency-rate-api.jar"]
