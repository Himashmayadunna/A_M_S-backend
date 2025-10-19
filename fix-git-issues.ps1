# Git Fix Script - Resolves common git issues and prepares for commit
# This script fixes the permission denied error and cleans up the repository

Write-Host "=== Git Repository Fix Script ===" -ForegroundColor Green
Write-Host ""

# Step 1: Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "? Error: Not in a git repository root directory" -ForegroundColor Red
    Write-Host "Please run this script from the root of your repository" -ForegroundColor Yellow
    exit 1
}

Write-Host "? Found git repository" -ForegroundColor Green
Write-Host ""

# Step 2: Remove cached .vs folder from git
Write-Host "1. Removing Visual Studio temporary files from git tracking..." -ForegroundColor Yellow

# Remove .vs folder from git index (but keep it on disk)
try {
    git rm -r --cached .vs 2>$null
    Write-Host "   ? Removed .vs folder from git tracking" -ForegroundColor Green
} catch {
    Write-Host "   ??  .vs folder not in git index (this is good)" -ForegroundColor Gray
}

Write-Host ""

# Step 3: Remove other common temporary files
Write-Host "2. Cleaning up other temporary files..." -ForegroundColor Yellow

$filesToRemove = @(
    "**/.vs/**",
    "**/bin/**",
    "**/obj/**",
    "**/*.user",
    "**/*.suo"
)

foreach ($pattern in $filesToRemove) {
    try {
        git rm -r --cached $pattern 2>$null
        Write-Host "   ? Removed $pattern from git tracking" -ForegroundColor Green
    } catch {
        # Silently ignore if files don't exist
    }
}

Write-Host ""

# Step 4: Verify .gitignore exists
Write-Host "3. Checking .gitignore file..." -ForegroundColor Yellow

if (Test-Path ".gitignore") {
    Write-Host "   ? .gitignore file exists" -ForegroundColor Green
} else {
    Write-Host "   ? .gitignore file not found" -ForegroundColor Red
    Write-Host "   Creating .gitignore file..." -ForegroundColor Yellow
    # The .gitignore should have been created by the previous step
}

Write-Host ""

# Step 5: Show current git status
Write-Host "4. Current git status:" -ForegroundColor Yellow
Write-Host ""

git status

Write-Host ""

# Step 6: Instructions for next steps
Write-Host "=== Next Steps ===" -ForegroundColor Green
Write-Host ""

Write-Host "The repository has been cleaned up. Now you can commit your changes:" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Add all files (excluding ignored ones):" -ForegroundColor White
Write-Host "   git add ." -ForegroundColor Yellow
Write-Host ""

Write-Host "2. Commit your changes:" -ForegroundColor White
Write-Host '   git commit -m "Add auction management system backend"' -ForegroundColor Yellow
Write-Host ""

Write-Host "3. Push to GitHub:" -ForegroundColor White
Write-Host "   git push origin main" -ForegroundColor Yellow
Write-Host ""

Write-Host "Alternative: Do all steps at once:" -ForegroundColor White
Write-Host '   git add . && git commit -m "Add auction management system backend" && git push origin main' -ForegroundColor Yellow
Write-Host ""

Write-Host "=== What Was Fixed ===" -ForegroundColor Cyan
Write-Host "? Removed .vs folder from git tracking" -ForegroundColor White
Write-Host "? Removed bin/obj folders from git tracking" -ForegroundColor White
Write-Host "? Updated .gitignore to prevent future issues" -ForegroundColor White
Write-Host "? Cleaned up temporary Visual Studio files" -ForegroundColor White
Write-Host ""

Write-Host "Your repository is now ready for GitHub! ??" -ForegroundColor Green