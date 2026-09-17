import json
import re

transcript_path = r'C:\Users\josfa\.gemini\antigravity\brain\b1e6b557-cba6-4aa0-81a9-49d708ad8402\.system_generated\logs\transcript_full.jsonl'

with open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        try:
            data = json.loads(line)
            if 'tool_calls' in data:
                # responses might be in the next steps?
                pass
            if 'content' in data and data['content']:
                if 'ProcessDepartmentSale' in data['content']:
                    with open('extracted_code.txt', 'a', encoding='utf-8') as out:
                        out.write(data['content'] + '\n' + '='*80 + '\n')
            
            # Check for tool_responses which might be nested or in a specific format
            if 'tool_responses' in data: # if we have such field
                pass
        except:
            pass
