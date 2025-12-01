# NuGet Package Deployment Guide

This document explains how to deploy the MinimalEndpoints package to NuGet.org using GitHub Actions.

## Prerequisites

1. **NuGet.org Account**: Create an account at https://www.nuget.org if you don't have one
2. **API Key**: Generate an API key from your NuGet.org account
3. **Repository Admin Access**: You need admin access to the GitHub repository to add secrets

## Setup Instructions

### Step 1: Generate NuGet API Key

1. Go to https://www.nuget.org and sign in to your account
2. Navigate to **Account Settings** → **API Keys**
3. Click **Create** to generate a new API key
4. Set appropriate scopes:
   - Recommended: Limit to "Push new packages and package versions"
   - Optionally set an expiration date
5. Copy the API key (you'll only see it once)

### Step 2: Add GitHub Secret

1. Go to your GitHub repository: https://github.com/MarioGK/MinimalEndpoints
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `NUGET_API_KEY`
5. Value: Paste the API key you copied in Step 1
6. Click **Add secret**

### Step 3: Deploy the Package

#### Option A: Using Git Tags (Recommended)

The workflow automatically triggers when you push a tag matching the pattern `v*`:

```bash
# Create and push a version tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

This will:
1. Build the project
2. Run tests
3. Pack the NuGet package
4. Push to NuGet.org
5. Create a GitHub Release

#### Option B: Manual Workflow Dispatch

1. Go to **Actions** → **Deploy NuGet Package**
2. Click **Run workflow**
3. Optionally specify a custom version
4. The workflow will execute

## Workflow Steps

The GitHub Actions workflow (`deploy.yml`) performs the following steps:

1. **Checkout Code**: Clones the repository
2. **Setup .NET**: Installs .NET 6.0 and 10.0 SDKs
3. **Restore Dependencies**: Runs `dotnet restore`
4. **Build**: Builds the project in Release mode
5. **Test**: Runs all unit tests
6. **Pack**: Creates the NuGet package
7. **Push**: Publishes to NuGet.org
8. **Create Release**: Creates a GitHub Release (for tagged releases)

## Troubleshooting

### API Key Issues
- **Invalid API Key**: Verify you copied the key correctly
- **Unauthorized**: Check that the key hasn't expired
- **Invalid Scope**: Ensure the key has "Push" permissions

### Build Failures
- Check the workflow logs in the **Actions** tab
- Ensure all tests pass locally before pushing tags
- Verify .NET dependencies are correctly specified

### Package Already Exists
- NuGet.org has a built-in duplicate check
- The `--skip-duplicate` flag in the workflow prevents errors on re-runs
- Use different version numbers for each release

## Version Numbering

Follow Semantic Versioning:
- **Major.Minor.Patch** (e.g., `1.0.0`)
- Tag format: `v1.0.0`

When updating version in code:
- The version is read from the project file properties
- If not explicitly set, you can add `<Version>1.0.0</Version>` to the `.csproj` file

## Viewing the Published Package

After successful deployment:
1. Visit https://www.nuget.org/packages/MinimalEndpoints
2. Your package will appear within a few minutes
3. Users can install via: `dotnet add package MinimalEndpoints`

## Package Metadata

The following metadata is configured in the build files:

- **Author**: MarioGK
- **License**: MIT
- **Repository**: https://github.com/MarioGK/MinimalEndpoints
- **Tags**: aspnetcore, minimal-apis, source-generator, endpoints, openapi, swagger
- **Description**: A source code generator for ASP.NET Core that simplifies minimal API development

## Next Steps

After deploying for the first time:

1. Verify the package appears on NuGet.org
2. Test installation in a sample project: `dotnet add package MinimalEndpoints`
3. Consider setting up additional protections:
   - Branch protection rules
   - Required status checks
   - Approval workflows for releases
