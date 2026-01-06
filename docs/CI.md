# Continuous Integration (CI) Pipeline Documentation

## Overview

This repository uses GitHub Actions to automatically build and test all components of the FamilyFitness application on every commit and pull request. The CI pipeline ensures code quality and helps prevent regressions before code is merged.

## Pipeline Triggers

The CI workflow (`.github/workflows/ci.yml`) runs automatically on:

1. **Every push to any branch**
   - Ensures all commits are buildable and pass tests
   - Provides immediate feedback to developers

2. **Every pull request (PR) targeting `main` or `development` branches**
   - Acts as a quality gate before merging
   - Prevents broken code from entering main branches
   - PR cannot be merged if CI fails

## Pipeline Steps

### 1. Environment Setup
- **Runner**: Ubuntu latest (Linux)
- **.NET SDK**: .NET 10.x (latest patch version)
- **PostgreSQL**: Version 17 running as a service container
  - Database: `family_fitness`
  - Username: `postgres`
  - Password: `postgres`
  - Port: `5432`
  - Health checks ensure database is ready before tests run

### 2. Build Process

The pipeline performs the following build steps:

```bash
# 1. Restore all NuGet dependencies
dotnet restore FamilyFitness.sln

# 2. Build all projects in Release configuration
dotnet build FamilyFitness.sln --configuration Release --no-restore
```

**Projects built:**
- `FamilyFitness.Domain` - Core domain entities and business logic
- `FamilyFitness.Application` - Use cases and application services
- `FamilyFitness.Infrastructure` - PostgreSQL implementation with EF Core
- `FamilyFitness.Api` - RESTful API endpoints
- `FamilyFitness.Blazor` - Blazor web UI
- `FamilyFitness.AppHost` - Aspire orchestration project

### 3. Test Execution

All test projects are executed:

```bash
dotnet test FamilyFitness.sln --configuration Release --no-build --verbosity normal
```

**Test projects:**
- `FamilyFitness.UnitTests` - Fast, isolated unit tests
- `FamilyFitness.IntegrationTests` - Tests with database integration
- `FamilyFitness.EndToEndTests` - Full application flow tests

**Environment variables for tests:**
- `ConnectionStrings__postgres` - PostgreSQL connection string for integration tests

### 4. Test Results

- Test results are uploaded as artifacts in TRX format
- Available for download from the GitHub Actions run page
- Retained for 30 days
- Uploaded even if tests fail (`if: always()` condition)

## Quality Gates

The pipeline enforces the following quality gates:

✅ **All projects must compile successfully**
- Any build error fails the pipeline
- Compilation warnings are displayed but don't fail the build

✅ **All tests must pass**
- Any failing test fails the pipeline
- Test output is displayed in the workflow logs

✅ **Dependencies must restore**
- Missing or conflicting packages fail the pipeline

## Viewing CI Results

### For a specific commit:
1. Go to the repository on GitHub
2. Click on "Actions" tab
3. Find your commit or PR in the workflow runs list
4. Click to view detailed logs for each step

### For a pull request:
1. GitHub automatically displays CI status on the PR page
2. Green check ✅ = All checks passed
3. Red X ❌ = Build or tests failed
4. Yellow dot 🟡 = Workflow is running
5. Click "Details" to see the full workflow log

## Local Testing

Before pushing code, you can run the same checks locally:

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release
```

For integration tests that require PostgreSQL:

```bash
# Start PostgreSQL with Docker
docker run -d \
  --name family-fitness-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=family_fitness \
  -p 5432:5432 \
  postgres:17

# Run tests
dotnet test --configuration Release

# Stop PostgreSQL container
docker stop family-fitness-postgres
docker rm family-fitness-postgres
```

## Troubleshooting CI Failures

### Build Failures
- **Cause**: Compilation errors, missing dependencies, syntax errors
- **Solution**: Review the build log, fix compilation errors locally
- **Tip**: Run `dotnet build` locally before pushing

### Test Failures
- **Cause**: Failing test cases, database connection issues
- **Solution**: Review test output, run tests locally
- **Tip**: Run `dotnet test` locally with the same configuration

### Dependency Restore Failures
- **Cause**: Missing packages, version conflicts, network issues
- **Solution**: Check NuGet package references, clear local cache
- **Tip**: Run `dotnet restore` locally

### PostgreSQL Connection Issues
- **Cause**: Service container not ready, incorrect connection string
- **Solution**: Check service configuration in workflow file
- **Note**: The workflow includes health checks to ensure PostgreSQL is ready

## Extending the CI Pipeline

The current CI pipeline is intentionally simple and focused on building and testing. Future enhancements could include:

### Potential Extensions

1. **Code Coverage**
   ```yaml
   - name: Generate code coverage
     run: dotnet test --collect:"XPlat Code Coverage"
   
   - name: Upload coverage to Codecov
     uses: codecov/codecov-action@v4
   ```

2. **Code Quality Analysis**
   ```yaml
   - name: Run code analysis
     run: dotnet build --no-restore /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest
   ```

3. **Security Scanning**
   ```yaml
   - name: Run security scan
     uses: github/codeql-action/init@v3
   ```

4. **Deployment Steps** (after CI validation)
   - Build Docker images
   - Publish to container registry
   - Deploy to staging/production environments

5. **Performance Testing**
   - Benchmark tests
   - Load testing
   - Memory profiling

### Adding Steps to the Workflow

To extend the workflow:

1. Edit `.github/workflows/ci.yml`
2. Add new steps under the `steps:` section
3. Follow the existing pattern for step naming
4. Test locally if possible
5. Commit and push to see it run

### Creating Additional Workflows

For separate concerns (e.g., deployment, scheduled tasks), create new workflow files:

1. Create `.github/workflows/new-workflow.yml`
2. Define appropriate triggers
3. Add jobs and steps
4. Document the new workflow

## CI Pipeline Architecture Decisions

### Why GitHub Actions?
- Native integration with GitHub
- Free for public repositories
- Large ecosystem of actions
- Easy to configure with YAML

### Why PostgreSQL 17?
- Matches the production database
- Required for integration tests
- Available as a service container
- Latest stable version

### Why .NET 10?
- Application is built with .NET 10
- Latest LTS or stable version support
- Best performance and features

### Why Release Configuration?
- Optimized builds
- Closer to production environment
- Better performance for tests
- Catches release-specific issues

## Best Practices

1. **Keep CI Fast**
   - Current pipeline runs in ~1-2 minutes
   - Avoid slow operations in CI
   - Use parallel jobs if pipeline grows

2. **Fail Fast**
   - Build before testing
   - Stop on first failure
   - Provide clear error messages

3. **Reproducible Builds**
   - Pin dependency versions when needed
   - Use same commands locally and in CI
   - Document environment requirements

4. **Test Isolation**
   - Each test run uses fresh PostgreSQL
   - Tests should not depend on execution order
   - Clean up test data properly

5. **Clear Feedback**
   - Descriptive step names
   - Appropriate verbosity levels
   - Upload artifacts for debugging

## Support and Maintenance

### Updating Dependencies

When updating .NET version or packages:

1. Update project files (`.csproj`)
2. Update workflow `.NET version`
3. Test locally
4. Update this documentation

### Workflow Maintenance

The workflow should be reviewed when:
- .NET version changes
- PostgreSQL version changes
- New test projects are added
- New build requirements emerge
- Performance degrades

### Getting Help

If CI is failing and you need help:
1. Review the workflow logs
2. Check this documentation
3. Try reproducing locally
4. Check GitHub Actions status page
5. Review recent commits for breaking changes

## Metrics and Monitoring

Track these metrics to ensure CI health:

- **Success Rate**: Aim for 100%
- **Build Time**: Keep under 5 minutes
- **Test Coverage**: Monitor trends
- **Flaky Tests**: Identify and fix

Access these metrics via:
- GitHub Actions dashboard
- Workflow run history
- PR status checks

## Conclusion

This CI pipeline provides automated quality assurance for every code change. It's designed to be:
- **Simple**: Easy to understand and maintain
- **Fast**: Quick feedback to developers  
- **Reliable**: Consistent results
- **Extensible**: Room for future enhancements

For questions or improvements, please open an issue or submit a pull request.
