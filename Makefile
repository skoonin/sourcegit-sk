# Local development tasks for SourceGit (macOS-focused).
# GitHub Actions are disabled in this repository; `make check` is the local CI gate.

DOTNET ?= $(shell command -v dotnet 2>/dev/null || echo $(HOME)/.local/share/dotnet/dotnet)
# Avalonia's build-time stats task writes to ~/Library/Application Support/AvaloniaUI;
# that write fails in restricted/sandboxed environments and breaks the build. Opt out to
# keep builds hermetic. Only affects build telemetry, not the app.
export AVALONIA_TELEMETRY_OPTOUT := true
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
run: ## Run last built GUI app
	$(DOTNET) run --project $(APP_PROJECT)

.PHONY: run-build
run-build: ## Build and run the app (Debug)
	$(MAKE) build
	$(MAKE) run

.PHONY: build-dev
build-dev: ## Build a dev .app for local testing and print its exact sha-stamped version
	@test -z "$$(git status --porcelain)" || echo "WARNING: uncommitted changes -> version will show -dirty instead of a clean commit sha; commit first for an unambiguous build"
	$(MAKE) app
	@# Version composition is canonical in src/SourceGit.csproj (GenVersionInfo); mirrored here for the summary line.
	@tail=$$(git describe --abbrev=8 --dirty 2>/dev/null | sed -E 's/^v[0-9]{4}\.[0-9]+(-sk(\.[0-9]+)?)?//' | tr -d g); \
	 echo "Dev build ready: build/SourceGit.app  (version v$(VERSION)$$tail)"

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

.PHONY: loc-check
loc-check: ## Sync TRANSLATION.md and sort locale files (CI parity; run after locale changes)
	npm install --prefix build/scripts --no-save --silent fs-extra@11.2.0 xml2js@0.6.2
	node build/scripts/localization-check.js

##@ Release

.PHONY: publish
publish: ## Publish a Release build for RUNTIME (default osx-arm64) to build/SourceGit
	$(DOTNET) publish -c Release -r $(RUNTIME) -o build/SourceGit $(APP_PROJECT)

.PHONY: app
app: publish ## Build SourceGit.app bundle and zip (build/sourcegit_<version>.<runtime>.zip)
	rm -rf build/SourceGit.app build/*.zip
	VERSION=$(VERSION) RUNTIME=$(RUNTIME) bash build/scripts/package.osx-app.sh

.PHONY: release
release: ## Run checks, tag v<VERSION>, and build the release zip (tag stays local until pushed)
	@test -z "$$(git status --porcelain)" || { echo "ERROR: working tree not clean; commit or stash first"; exit 1; }
	@! git rev-parse -q --verify "refs/tags/v$(VERSION)" >/dev/null || { echo "ERROR: tag v$(VERSION) already exists; bump VERSION first"; exit 1; }
	$(MAKE) check
	git tag -a "v$(VERSION)" -m "Release $(VERSION)"
	$(MAKE) app
	@echo "Released v$(VERSION): build/sourcegit_$(VERSION).$(RUNTIME).zip"
	@echo "To publish: git push origin master v$(VERSION); gh release create v$(VERSION) build/sourcegit_$(VERSION).$(RUNTIME).zip --title v$(VERSION) --notes 'Fork release $(VERSION)'"

.PHONY: install
install: ## Install built SourceGit.app into /Applications (overwrites)
	rm -rf /Applications/SourceGit.app
	cp -R build/SourceGit.app /Applications/
	@echo "Installed /Applications/SourceGit.app ($(VERSION), $(RUNTIME)). Restart the app to pick it up."

##@ Housekeeping

.PHONY: clean
clean: ## Remove build outputs
	-$(DOTNET) clean $(APP_PROJECT) >/dev/null
	rm -rf build/SourceGit build/SourceGit.app build/*.zip
