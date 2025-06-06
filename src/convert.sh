#!/bin/bash

# Create input and output directories if they don't exist
mkdir -p input
mkdir -p output

# Change to the input directory
cd input

# Loop through all .fur files recursively
find . -type f -name "*.fur" | while read -r f; do
    # Get full path and filename without extension
    input_file="$f"
    filename=$(basename "$f" .fur)
    
    # Call the converter using relative path
    "../fur2Uge" --i "$input_file" --o "../output/${filename}.uge"
done
