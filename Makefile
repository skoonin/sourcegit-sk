# Local development tasks for SourceGit (macOS-focused).
# GitHub Actions are disabled in this repository; `make check` is the local CI gate.

DOTNET ?= $(shell command -v dotnet 2>/dev/null || echo $(HOME)/.local/share/dotnet/dotnet)
# Lazy so only the app target shells out for the version.
VERSION = $(shell cat VERSION)
RUNTIME ?= osx-arm64

APP_PROJECT := src/SourceGit.csproj
TEST_PROJECT := tests/SourceGit.Tests/SourceGit.Tests.csproj

.DEFAULT_GOAL := help

##@ General

.PHONY: help
help: ## Show this help
	@awk 'BEGIN {FS = ":.*##"} /^[a-zA-Z_-]+:.*?##/ { printf "  %-14s %s\n", $$1, $$2 } /^##@/ { printf "\n%s\n", substr($$0, 5) }' $(MAKEFILE_LIST)

##@ Development

.PHONY: build
build: ## Build the app (Debug)
	$(DOTNET) build $(APP_PROJECT)

.PHONY: run
run: ## Build and run the GUI app
	$(DOTNET) run --project $(APP_PROJECT)

.PHONY: test
test: ## Run the test suite
	$(DOTNET) test $(TEST_PROJECT)

.PHONY: format
format: ## Fix code style in app and tests
	$(DOTNET) format $(APP_PROJECT)
	$(DOTNET) format $(TEST_PROJECT)

.PHONY: format-check
format-check: ## Verify code style without changing files (CI parity)
	$(DOTNET) format --verify-no-changes $(APP_PROJECT)
	$(DOTNET) format --verify-no-changes $(TEST_PROJECT)

.PHONY: check
check: format-check build test ## Run all local gates: format, build, test

##@ Release

.PHONY: publish
publish: ## Publish a Release build for RUNTIME (default osx-arm64) to build/SourceGit
	$(DOTNET) publish -c Release -r $(RUNTIME) -o build/SourceGit $(APP_PROJECT)

.PHONY: app
app: publish ## Build SourceGit.app bundle and zip (build/sourcegit_<version>.<runtime>.zip)
	VERSION=$(VERSION) RUNTIME=$(RUNTIME) bash build/scripts/package.osx-app.sh

##@ Housekeeping

.PHONY: clean
clean: ## Remove build outputs
	-$(DOTNET) clean $(APP_PROJECT) >/dev/null
	rm -rf build/SourceGit build/SourceGit.app build/*.zip
