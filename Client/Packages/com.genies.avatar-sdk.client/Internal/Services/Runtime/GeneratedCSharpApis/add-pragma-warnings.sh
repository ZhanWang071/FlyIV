#!/bin/zsh

# Adds #pragma warning disable CS0472 to the top of all generated C# files
# Run this in ~/genies-unity-packages/com.genies.services/Runtime/GeneratedCSharpApis

set -e

echo "Adding #pragma warning disable CS0472 to generated C# files..."

add_pragma_to_file() {
    local cs_file="$1"
    local pragma_line="#pragma warning disable CS0472"
    
    # Check if the pragma is already present
    if ! grep -q "^$pragma_line" "$cs_file"; then
        echo "Adding pragma to: $cs_file"
        # Create a temp file with pragma at top, then the original content
        echo "$pragma_line" | cat - "$cs_file" > "${cs_file}.tmp" && mv "${cs_file}.tmp" "$cs_file"
    fi
}

# Process services directories
for dir in Api Model Client Properties; do
    if [ -d "$dir" ]; then
        for file in "$dir"/*.cs; do
            if [ -f "$file" ]; then
                add_pragma_to_file "$file"
            fi
        done
    fi
done

# Process Genies.Persona directories
for dir in Genies.Persona/Api Genies.Persona/Model Genies.Persona/Client; do
    if [ -d "$dir" ]; then
        for file in "$dir"/*.cs; do
            if [ -f "$file" ]; then
                add_pragma_to_file "$file"
            fi
        done
    fi
done

# Process Genies.SDKServices directories
for dir in Genies.SDKServices/Api Genies.SDKServices/Model Genies.SDKServices/Client; do
    if [ -d "$dir" ]; then
        for file in "$dir"/*.cs; do
            if [ -f "$file" ]; then
                add_pragma_to_file "$file"
            fi
        done
    fi
done

# Process Auth directory
if [ -d "Auth" ]; then
    for file in Auth/*.cs; do
        if [ -f "$file" ]; then
            add_pragma_to_file "$file"
        fi
    done
fi

echo "Done!"
