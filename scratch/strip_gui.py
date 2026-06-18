import os
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    new_content = re.sub(r'public override ResourceOptions GetOptions\s*\(\s*object\s+[a-zA-Z0-9_]+\s*\)\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', 'public override ResourceOptions GetOptions (object w) { return GetDefaultOptions(); }', content)
    
    # Also strip out any remaining GUI.Widget usage outside of GetOptions just in case
    new_content = re.sub(r'[a-zA-Z0-9_]+\s+as\s+GUI\.Widget[a-zA-Z0-9_]+', 'null', new_content)
    new_content = re.sub(r'\(\s*GUI\.Widget[a-zA-Z0-9_]+\s*\)', '(object)', new_content)

    if new_content != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {filepath}")

for root, _, files in os.walk('ArcFormats'):
    for file in files:
        if file.endswith('.cs'):
            process_file(os.path.join(root, file))
