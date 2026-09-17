import json

transcript_path = r'C:\Users\josfa\.gemini\antigravity\brain\b1e6b557-cba6-4aa0-81a9-49d708ad8402\.system_generated\logs\transcript_full.jsonl'

found_full_file = False
with open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        try:
            data = json.loads(line)
            # we are looking for a system response containing the full file
            if data['type'] == 'TOOL_RESPONSE':
                content = data['content']
                if 'public partial class Form1 : Form' in content and 'private void ProcessCheckoutAsync' in content:
                    with open('original_form1_full.txt', 'w', encoding='utf-8') as out:
                        out.write(content)
                        found_full_file = True
        except:
            pass
