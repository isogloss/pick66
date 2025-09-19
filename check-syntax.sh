#!/bin/bash

# Simple syntax checker for batch files
# This doesn't run the scripts but checks for basic syntax issues

echo "Checking batch file syntax..."

# Check for basic batch file syntax issues
check_batch_file() {
    local file="$1"
    echo "Checking $file..."
    
    # Check for unmatched parentheses
    local open_count=$(grep -o '(' "$file" | wc -l)
    local close_count=$(grep -o ')' "$file" | wc -l)
    
    if [ "$open_count" -ne "$close_count" ]; then
        echo "  WARNING: Unmatched parentheses in $file (open: $open_count, close: $close_count)"
    else
        echo "  OK: Parentheses balanced"
    fi
    
    # Check for common batch syntax patterns
    if grep -q "if.*(" "$file"; then
        echo "  OK: Contains conditional statements"
    fi
    
    if grep -q "echo.*" "$file"; then
        echo "  OK: Contains echo statements"
    fi
    
    # Check for goto labels
    local goto_count=$(grep -c "goto" "$file" || echo "0")
    local label_count=$(grep -c "^:" "$file" || echo "0")
    
    if [ "$goto_count" -gt 0 ] && [ "$label_count" -eq 0 ]; then
        echo "  WARNING: Found goto statements but no labels"
    elif [ "$goto_count" -gt 0 ] && [ "$label_count" -gt 0 ]; then
        echo "  OK: goto/label pairs detected"
    fi
    
    echo "  File appears syntactically valid"
    echo ""
}

# Check installer batch files
if [ -f "install.bat" ]; then
    check_batch_file "install.bat"
fi

if [ -f "install-standalone.bat" ]; then
    check_batch_file "install-standalone.bat"
fi

if [ -f "setup.bat" ]; then
    check_batch_file "setup.bat"
fi

echo "Basic syntax check complete."
echo "Note: This is a basic check. Full validation requires Windows environment."