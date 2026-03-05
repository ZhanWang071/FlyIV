#!/bin/zsh

# for post processing the auto generated swaggerhub files
# because the swaggerhub output file generation is inconsistent
# this script will not work on windows (needs to be bin/bash)
#
# Modified version that generates meta files without Unity dependency

# run this in ~/genies-unity-packages/com.genies.services/Runtime/GeneratedCSharpApis

set -e  # Exit immediately if a command exits with a non-zero status.
set -u  # Treat unset variables as an error and exit immediately.

# Define source directory
src_dir="tmp/src/Genies.Persona"

# Check if source directories exist
if [ ! -d "$src_dir" ]; then
    echo "Source directory $src_dir does not exist."
    exit 1
fi

# Move Api/ files
if [ -d "$src_dir/Api" ]; then
    mv -v "$src_dir/Api/"* Genies.Persona/Api/
else
    echo "Directory $src_dir/Api does not exist."
fi

# Move Client/ files
if [ -d "$src_dir/Client" ]; then
    mv -v "$src_dir/Client/"* Genies.Persona/Client/
else
    echo "Directory $src_dir/Client does not exist."
fi

# Move Model/ files
if [ -d "$src_dir/Model" ]; then
    mv -v "$src_dir/Model/"* Genies.Persona/Model/
else
    echo "Directory $src_dir/Model does not exist."
fi

# Remove the tmp directory if it exists
if [ -d "tmp" ]; then
    rm -rf tmp
    echo "Removed tmp directory."
else
    echo "tmp directory does not exist."
fi

# Add #pragma warning disable CS0472 to the top of all generated C# files
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

# Process all C# files in the Genies.Persona directories
for dir in Genies.Persona/Api Genies.Persona/Model Genies.Persona/Client; do
    if [ -d "$dir" ]; then
        for file in "$dir"/*.cs; do
            if [ -f "$file" ]; then
                add_pragma_to_file "$file"
            fi
        done
    fi
done

echo "Pragma addition completed!"

# Generate Unity meta files manually (without Unity dependency)
echo "Generating Unity meta files manually..."

# Function to generate a Unity GUID
generate_unity_guid() {
    python3 -c "import uuid; print(str(uuid.uuid4()).replace('-', ''))"
}

# Function to create a meta file for a C# script
create_cs_meta_file() {
    local cs_file="$1"
    local meta_file="${cs_file}.meta"

    if [ ! -f "$meta_file" ]; then
        local guid=$(generate_unity_guid)
        echo "Creating meta file for: $cs_file"

        cat > "$meta_file" <<EOL
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
EOL
    fi
}

# Function to create a meta file for a directory
create_directory_meta_file() {
    local dir_path="$1"
    local meta_file="${dir_path}.meta"

    if [ ! -f "$meta_file" ]; then
        local guid=$(generate_unity_guid)
        echo "Creating meta file for directory: $dir_path"

        cat > "$meta_file" <<EOL
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOL
    fi
}

# Generate meta files for Genies.Persona directories that don't have them
for dir in Genies.Persona Genies.Persona/Api Genies.Persona/Model Genies.Persona/Client; do
    if [ -d "$dir" ]; then
        create_directory_meta_file "$dir"
    fi
done

# Generate meta files for all C# files in Genies.Persona that don't have them
find Genies.Persona -name "*.cs" -type f | while read -r cs_file; do
    create_cs_meta_file "$cs_file"
done

echo "Meta file generation completed!"
echo "Script execution completed."
