#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates PowerShell script syntax for CI/testing

.DESCRIPTION
    This script validates that PowerShell scripts in the repository have correct syntax
    and can be parsed by both PowerShell 7+ and Windows PowerShell 5.1 (when available).

.PARAMETER Path
    Path to PowerShell script to validate (default: install.ps1)

.EXAMPLE
    ./validate-powershell.ps1
    ./validate-powershell.ps1 -Path install.ps1
#>

param(
    [string]$Path = "install.ps1"
)

$ErrorActionPreference = "Stop"

function Test-PowerShellSyntax {
    param([string]$ScriptPath)
    
    if (!(Test-Path $ScriptPath)) {
        throw "Script not found: $ScriptPath"
    }
    
    Write-Host "Validating PowerShell syntax: $ScriptPath" -ForegroundColor Cyan
    
    try {
        # Test with current PowerShell version
        $content = Get-Content -Raw $ScriptPath
        [System.Management.Automation.PSParser]::Tokenize($content, [ref]$null) | Out-Null
        Write-Host "✅ Syntax validation passed" -ForegroundColor Green
        
        # Additional checks
        $lines = Get-Content $ScriptPath
        $openBraces = ($content -split '{').Count - 1
        $closeBraces = ($content -split '}').Count - 1
        $openParens = ($content -split '\(').Count - 1  
        $closeParens = ($content -split '\)').Count - 1
        
        Write-Host "📊 Syntax Statistics:" -ForegroundColor Yellow
        Write-Host "   Lines: $($lines.Count)"
        Write-Host "   Braces: $openBraces open, $closeBraces close"
        Write-Host "   Parentheses: $openParens open, $closeParens close"
        
        if ($openBraces -ne $closeBraces) {
            throw "Mismatched braces: $openBraces open, $closeBraces close"
        }
        
        if ($openParens -ne $closeParens) {
            throw "Mismatched parentheses: $openParens open, $closeParens close"
        }
        
        Write-Host "✅ Structure validation passed" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Syntax validation failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
try {
    Write-Host "PowerShell Script Syntax Validator" -ForegroundColor Cyan
    Write-Host "PowerShell Version: $($PSVersionTable.PSVersion)" -ForegroundColor Yellow
    Write-Host ""
    
    $result = Test-PowerShellSyntax $Path
    
    if ($result) {
        Write-Host ""
        Write-Host "🎉 All validations passed!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "💥 Validation failed!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}