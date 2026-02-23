# =======================================
# Generate code for skill function via large models
# =======================================
import re
from flask import request, jsonify

def generate_functions(data):
    response_data = []

    for item in data:
        # 提取函数名和参数，例如 "UPDATE(string element_id, float value)"
        func_sig = item.get("function", "")
        func_description = item.get("description", "")
        print("正在生成函数：" + func_sig + " 功能描述：" + func_description)
        match = re.match(r"(\w+)\((.*)\)", func_sig)
        
        if match:
            func_name = match.group(1) # UPDATE
            params = match.group(2)    # string element_id, float value
            
            # 生成skill function代码字符串
            # TODO: 使用大模型生成结果替换mock code
            generated_code = generate_mock_code(func_name, params)
            
            response_data.append({
                "function": func_name,
                "code": generated_code
            })

    return jsonify(response_data)

def generate_mock_code(func_name, params):
    class_name = transfer_skill_to_class_name(func_name)
    
    code_template = f"""using UnityEngine;

public class {class_name}
{{
    public static void Execute({params})
    {{
        Debug.Log("正在执行动态生成的 {class_name} 逻辑");
    }}
}}"""
    return code_template

# 转换名称格式用于类名 (例如 UPDATE -> Update)
def transfer_skill_to_class_name(func_name):
    class_name = "".join(word.capitalize() for word in func_name.split("_"))
    return class_name