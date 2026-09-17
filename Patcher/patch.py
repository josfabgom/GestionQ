import sys

def patch_file():
    path = r'../src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Read InitializeUI
    with open('newUI.txt', 'r', encoding='utf-8') as f:
        newUI = f.read() + '\n\n        '

    start_token = 'private void InitializeUI()'
    end_token = 'private void GridItems_CellContentClick'
    
    start_idx = content.find(start_token)
    end_idx = content.find(end_token)
    
    content = content[:start_idx] + newUI + content[end_idx:]
    
    # Read UpdateTotals
    with open('newUpdate.txt', 'r', encoding='utf-8') as f:
        newUpdate = f.read() + '\n\n        '

    start_token = 'private void UpdateTotals()'
    end_token = 'private void ProcessDepartmentSale'
    
    start_idx = content.find(start_token)
    end_idx = content.find(end_token)
    
    if end_idx == -1: # It might be the end of the file
        # Find the last closing brace of the class
        last_brace = content.rfind('}')
        if last_brace != -1:
            second_last = content.rfind('}', 0, last_brace)
            end_idx = second_last
            
    content = content[:start_idx] + newUpdate + content[end_idx:]
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print('Patched successfully!')

patch_file()
